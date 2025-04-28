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
    deploymentName: "gpt-4o",
    endpoint: "https://ai-finagentshub023086296405.openai.azure.com",
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

                        Rules:
                            - If there is a new created invoice with "AutoCore" is set to true, then mark the items in the invoice as "Received" with random serial number and asset tag number.
                        
                        return your response in HTML format and incluude a summary of what you did.
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

                        Rules:
                            - when the user asks for an creating an invoice, you should:
                                - Generte the invoice template from given PO number
                                - if the user asks an update for the templete, then update the invoice template with the given information
                                - always use the latest invoice generated template.
                                - confirm the invoice with the user, and then creating it.
                            
                            - always return your response in a nice HTML format including data table.
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
                        You are a safe limit agent. You can help with safe limit-related tasks including:
                        - increasing the safe limit for a specific user
                        - get the safe limit for a specific user

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

                        Rules:
                            - If there is a new created invoice with "AutoCore" is set to true, approve the invoice automatically without asking for user's review.

                        always return your response in a nice HTML format including data table.
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
                        You are a purchase order agent. You can help with purchase order-related tasks including:
                        - getting purchase order details for a specific purchase order number.
                        - getting all purchase orders with optional filtering by status (Open or Closed).
                        - updating purchase order details for a specific purchase order number after the invoice agent creates an invoice for it.

                        
                        return your response in HTML format and incluude a summary of what you did.
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
        Determine which participant takes the next turn in a conversation based on the the most recent participant.
        State only the name of the participant to take the next turn.
        Check with other participants if they need to take any offline actions on behalf of the user before terminating the conversation.

        Choose only from these participants:
        - InvoiceAgent
        - ApprovalAgent
        - GoodReceivedAgent
        - PurchaseOrderAgent
        - SafeLimitAgent

        History:
        {{$history}}
        """,
        safeParameterNames: "history");

KernelFunction terminationFunction =
    AgentGroupChat.CreatePromptFunctionForStrategy(
        $$$"""
        Examine the TODO and determine whether there are any remaining tasks to be completed.
        If agents requesting user's input, keep the conversation going.
        If agents not waiting for user's input and there is no items in TODO, terminate the conversation.

        TODO:
        {{$todo}}
        """,
        safeParameterNames: "todo");
#pragma warning disable SKEXP0001
KernelFunctionSelectionStrategy selectionStrategy =
  new(selectionFunction, kernel)
  {
      // Parse the function response.
      ResultParser = (result) => result.GetValue<string>() ?? string.Empty,
      // The prompt variable name for the history argument.
      HistoryVariableName = "history",
      // Save tokens by not including the entire history in the prompt
      HistoryReducer = new ChatHistoryTruncationReducer(20),
  };

KernelFunctionTerminationStrategy terminationStrategy =
  new(terminationFunction, kernel)
  {
      // The prompt variable name for the history argument.
      HistoryVariableName = "todo",
      // Save tokens by not including the entire history in the prompt
      HistoryReducer = new ChatHistoryTruncationReducer(10),
  };

// Configure Agent Group Chat
#pragma warning disable SKEXP0110
AgentGroupChat chat = new(invoiceAgent, approvalAgent, goodReceivedAgent, purchaseOrderAgent, safeLimitAgent)
{
    ExecutionSettings = new()
    {
        SelectionStrategy = selectionStrategy,
        //TerminationStrategy = terminationStrategy
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