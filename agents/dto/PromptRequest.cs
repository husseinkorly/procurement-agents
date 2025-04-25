using agents.Services;

namespace agents.dto;

public class PromptRequest
{
    public string Prompt { get; set; } = string.Empty;
}

public class PromptResponse
{
    public List<MessageResponse> Messages { get; set; } = new();
}