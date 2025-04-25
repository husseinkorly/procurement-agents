using agents.dto;
using agents.Services;
using Microsoft.AspNetCore.Mvc;

namespace agents.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ChatService _chatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(ChatService chatService, ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    [HttpPost("prompt")]
    public async Task<IActionResult> ProcessPrompt([FromBody] PromptRequest request)
    {
        try
        {
            _logger.LogInformation("Received prompt: {Prompt}", request.Prompt);

            var responses = await _chatService.ProcessUserMessageAsync(request.Prompt);

            return Ok(new PromptResponse
            {
                Messages = responses,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing prompt");
            return StatusCode(500, "An error occurred while processing your request");
        }
    }
}
