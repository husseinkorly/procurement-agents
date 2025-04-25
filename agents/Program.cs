using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Azure.Identity;
using Microsoft.SemanticKernel.ChatCompletion;
using agents.plugins;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Microsoft.SemanticKernel.Agents.Chat;
using OpenTelemetry.Resources;
using agents.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        policy =>
        {
            policy.WithOrigins(
                    "http://localhost:5173",
                    "http://127.0.0.1:5173"
                )
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

// Configure OpenTelemetry
var resourceBuilder = ResourceBuilder
    .CreateDefault()
    .AddService("Agents-WebApp");
AppContext.SetSwitch("Microsoft.SemanticKernel.Experimental.GenAI.EnableOTelDiagnosticsSensitive", true);
builder.Logging.AddConsole();

// Configure Semantic Kernel
var kernelBuilder = Kernel.CreateBuilder();
kernelBuilder.AddAzureOpenAIChatCompletion(
    deploymentName: "gpt-4o-agent",
    endpoint: "https://oai-financecopilot-procurement-ppe.openai.azure.com",
    credentials: new AzureCliCredential()
);
// Add logging to kernel
var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .AddConsole()
        .AddDebug();
});
kernelBuilder.Services.AddSingleton(loggerFactory);
var kernel = kernelBuilder.Build();

// Configure Agents
ChatCompletionAgent goodReceivedAgent = new()
{
    Description = "Good received agent",
    Name = "GoodReceivedAgent",
    Instructions = """
                        You are a good received agent. You can help with good received-related tasks.
                        you're responsible for managing the goods received process, 
                        including updating the status of goods received, and providing information about goods received.
                        return your response in HTML format.
                        """,
    Kernel = kernel,
    Arguments = new KernelArguments(new PromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() })
};
goodReceivedAgent.Kernel.Plugins.Add(KernelPluginFactory.CreateFromType<GoodReceivedPlugin>());

// Invoice Agent
ChatCompletionAgent invoiceAgent = new()
{
    Description = "Invoice agent",
    Name = "InvoiceAgent",
    Instructions = """
                        You are an invoice agent. You can help with invoice-related tasks.
                        you can can get invoice(s) details, and create invoices.
                        return your response in HTML format.
                        """,
    Kernel = kernel,
    Arguments = new KernelArguments(new PromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() })
};
invoiceAgent.Kernel.Plugins.Add(KernelPluginFactory.CreateFromType<InvoicePlugin>());

// Safe Limit Agent
ChatCompletionAgent safeLimitAgent = new()
{
    Description = "Safe limit agent",
    Name = "SafeLimitAgent",
    Instructions = """
                        You are a safe limit agent. You can help with safe limit-related tasks.
                        you can get user's safe limit and request a limit increase.
                        return your response in HTML format.
                        """,
    Kernel = kernel,
    Arguments = new KernelArguments(new PromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() })
};
safeLimitAgent.Kernel.Plugins.Add(KernelPluginFactory.CreateFromType<SafeLimitPlugin>());

// Approval Agent
ChatCompletionAgent approvalAgent = new()
{
    Description = "Invoice approval agent",
    Name = "ApprovalAgent",
    Instructions = """
                        You are an invoice approval agent. You can help with invoice approval-related tasks.
                        you can approve invoices and get pending invoices for approval.
                        return your response in HTML format.
                        """,
    Kernel = kernel,
    Arguments = new KernelArguments(new PromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() })
};
approvalAgent.Kernel.Plugins.Add(KernelPluginFactory.CreateFromType<ApprovalPlugin>());

// Purchase Order Agent
ChatCompletionAgent purchaseOrderAgent = new()
{
    Description = "Purchase order agent",
    Name = "PurchaseOrderAgent",
    Instructions = """
                        You are a purchase order agent. You can help with purchase order-related tasks.
                        you can get purchase order(s) details, and approve purchase orders.
                        return your response in HTML format.
                        """,
    Kernel = kernel,
    Arguments = new KernelArguments(new PromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() })
};
purchaseOrderAgent.Kernel.Plugins.Add(KernelPluginFactory.CreateFromType<PurchaseOrderPlugin>());

// Configure Agent Selection Strategy
#pragma warning disable SKEXP0110
KernelFunction selectionFunction =
    AgentGroupChat.CreatePromptFunctionForStrategy(
        $$$"""
                Determine which participant takes the turn in a conversation based on the the most recent participant and user's request.
                State only the name of the participant to take the next turn.
                Chooese from the following participants:
                GoodReceivedAgent
                InvoiceAgent
                PurchaseOrderAgent
                SafeLimitAgent
                ApprovalAgent

                History:
                {{$history}}
                """,
        safeParameterNames: "history");
#pragma warning disable SKEXP0001
KernelFunctionSelectionStrategy selectionStrategy =
  new(selectionFunction, kernel)
  {
      // Parse the function response.
      ResultParser = (result) => result.GetValue<string>() ?? string.Empty,
      // The prompt variable name for the history argument.
      HistoryVariableName = "history",
      // Save tokens by not including the entire history in the prompt
      HistoryReducer = new ChatHistoryTruncationReducer(10),
  };

// Configure Agent Group Chat
#pragma warning disable SKEXP0110
AgentGroupChat chat = new(invoiceAgent, purchaseOrderAgent, goodReceivedAgent, safeLimitAgent, approvalAgent)
{
    ExecutionSettings = new()
    {
        SelectionStrategy = selectionStrategy
    }
};

// Register AgentGroupChat and ChatService
builder.Services.AddSingleton(chat);
builder.Services.AddSingleton<ChatService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    // Development-specific configuration can be added here
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();
app.MapControllers();

// Determine if we should run in Console mode or Web mode
bool runConsoleMode = args.Length > 0 && args[0].Equals("--console", StringComparison.OrdinalIgnoreCase);
if (runConsoleMode)
{
    RunConsoleMode(chat).Wait();
}
else
{
    app.Run();
}

static async Task RunConsoleMode(AgentGroupChat chat)
{
    ChatHistoryAgentThread thread = new ChatHistoryAgentThread();
    Console.WriteLine("Running in console mode. Type 'exit' to quit.");

    while (true)
    {
        Console.Write("User>>> ");
        string userInput = Console.ReadLine() ?? string.Empty;
        if (userInput.ToLower() == "exit")
        {
            break;
        }

        // Create a new message and add it to the chat history
        ChatMessageContent message = new(
            role: AuthorRole.User,
            content: userInput
        );
        thread.ChatHistory.Add(message);
        // Invoke the agent with the message and thread
        chat.AddChatMessage(message);
        // Add a cancellation token source to control the spinner task
        using var cts = new CancellationTokenSource();
        Console.Write("Processing your request... ");
        var spinner = new[] { '|', '/', '-', '\\' };
        var spinnerTask = Task.Run(async () =>
        {
            int i = 0;
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    Console.Write("\b" + spinner[i++ % spinner.Length]);
                    await Task.Delay(100, cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation occurs, no need to handle
            }
        }, cts.Token);

        // Get the response from the agent
        var messages = await chat.InvokeAsync().ToListAsync();
        // Properly cancel and wait for the spinner task to complete before disposing
        cts.Cancel();
        try
        {
            await spinnerTask;  // Wait for task to acknowledge cancellation
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation exceptions
        }

        Console.Write("\b \n");

        foreach (var messageContent in messages)
        {
            // Print the response from each agent
            Console.WriteLine($"{messageContent.Role}>>> {messageContent.Content}");
        }
    }
}