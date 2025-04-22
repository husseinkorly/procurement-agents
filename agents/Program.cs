using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Azure.Identity;
using Microsoft.SemanticKernel.ChatCompletion;
using agents.plugins;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Microsoft.SemanticKernel.Agents.Chat;


var builder = Kernel.CreateBuilder();
builder.AddAzureOpenAIChatCompletion(
    deploymentName: "gpt-4o",
    endpoint: "<your-endpoint>",
    credentials: new AzureCliCredential()
);
//builder.Services.AddSingleton(loggerFactory);
var kernel = builder.Build();

// crating an agent
ChatCompletionAgent goodReceivedAgent = new()
{
    Description = "Good received agent",
    Name = "GoodReceivedAgent",
    Instructions = """
                        You are a good received agent. You can help with good received-related tasks.
                        you're responsible for managing the goods received process, 
                        including updating the status of goods received, and providing information about goods received.
                        """,
    Kernel = kernel,
    Arguments = new KernelArguments(new PromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() })
};
// adding a plugin to the agent
goodReceivedAgent.Kernel.Plugins.Add(KernelPluginFactory.CreateFromType<GoodReceivedPlugin>());

ChatCompletionAgent invoiceAgent = new()
{
    Description = "Invoice agent",
    Name = "InvoiceAgent",
    Instructions = """
                        You are an invoice agent. You can help with invoice-related tasks.
                        you can can get invoice(s) details, and approve invoices
                        """,
    Kernel = kernel,
    Arguments = new KernelArguments(new PromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() })
};
invoiceAgent.Kernel.Plugins.Add(KernelPluginFactory.CreateFromType<InvoicePlugin>());
invoiceAgent.Kernel.Plugins.Add(KernelPluginFactory.CreateFromType<SafeLimitPlugin>());

ChatCompletionAgent purchaseOrderAgent = new()
{
    Description = "Purchase order agent",
    Name = "PurchaseOrderAgent",
    Instructions = """
                        You are a purchase order agent. You can help with purchase order-related tasks.
                        you can get purchase order(s) details, and approve purchase orders
                        """,
    Kernel = kernel,
    Arguments = new KernelArguments(new PromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() })
};
purchaseOrderAgent.Kernel.Plugins.Add(KernelPluginFactory.CreateFromType<PurchaseOrderPlugin>());

// creating a thread and pass it to the agent
ChatHistoryAgentThread thread = new ChatHistoryAgentThread();

#pragma warning disable SKEXP0110
KernelFunction selectionFunction =
    AgentGroupChat.CreatePromptFunctionForStrategy(
        $$$"""
                Determine which participant takes the next turn in a conversation based on the the most recent participant.
                State only the name of the participant to take the next turn.
                No participant should take more than one turn in a row.
                Chooese from the following participants:
                - GoodReceivedAgent
                - InvoiceAgent
                - PurchaseOrderAgent

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

#pragma warning disable SKEXP0110
AgentGroupChat chat = new(invoiceAgent, purchaseOrderAgent, goodReceivedAgent)
{
    ExecutionSettings = new()
    {
        SelectionStrategy = selectionStrategy
    }
};


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
    //await goodReceivedAgent.InvokeAsync(thread).ToListAsync();
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