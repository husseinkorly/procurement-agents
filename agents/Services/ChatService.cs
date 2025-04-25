using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;

namespace agents.Services;

public class ChatService
{
#pragma warning disable SKEXP0110
    private readonly AgentGroupChat _chat;
    private readonly ChatHistoryAgentThread _thread;

    public ChatService(AgentGroupChat chat)
    {
        _chat = chat;
        _thread = new ChatHistoryAgentThread();
    }

    public async Task<List<MessageResponse>> ProcessUserMessageAsync(string userMessage)
    {
        // Create a new message and add it to the chat
        var message = new ChatMessageContent(
            role: AuthorRole.User,
            content: userMessage
        );

        _chat.AddChatMessage(message);

        // Get the responses from the agents
        var responses = await _chat.InvokeAsync().ToListAsync();

        // Format the responses
        var formattedResponses = responses.Select(msg => new MessageResponse
        {
            Role = msg.Role.ToString(),
#pragma warning disable SKEXP0001
            AgentName = msg.AuthorName ?? string.Empty,
            Content = msg.Content ?? string.Empty,
            IsHtml = true
        }).ToList();

        return formattedResponses;
    }
}

public class MessageResponse
{
    public string Role { get; set; } = string.Empty;
    public string AgentName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsHtml { get; set; }
}