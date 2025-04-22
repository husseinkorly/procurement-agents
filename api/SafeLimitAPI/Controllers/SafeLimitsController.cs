using Microsoft.AspNetCore.Mvc;
using SafeLimitAPI.Models;
using SafeLimitAPI.Services;

namespace SafeLimitAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SafeLimitsController : ControllerBase
{
    private readonly SafeLimitService _safeLimitService;
    private readonly ILogger<SafeLimitsController> _logger;

    public SafeLimitsController(SafeLimitService safeLimitService, ILogger<SafeLimitsController> logger)
    {
        _safeLimitService = safeLimitService;
        _logger = logger;
    }

    [HttpGet]
    public ActionResult<List<SafeLimit>> GetAllSafeLimits()
    {
        try
        {
            _logger.LogInformation("Getting all safe limits");
            return Ok(_safeLimitService.GetAllSafeLimits());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving safe limits");
            return StatusCode(500, "An error occurred while retrieving safe limits");
        }
    }

    [HttpGet("user/{userId}")]
    public ActionResult<SafeLimit> GetSafeLimitByUserId(string userId)
    {
        try
        {
            _logger.LogInformation("Getting safe limit for user: {UserId}", userId);
            
            var userLimit = _safeLimitService.GetSafeLimitByUserId(userId);
            
            if (userLimit == null)
            {
                return NotFound($"User with ID {userId} not found");
            }
            
            return Ok(userLimit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving safe limit for user {UserId}", userId);
            return StatusCode(500, "An error occurred while retrieving the safe limit");
        }
    }

    [HttpGet("name/{userName}")]
    public ActionResult<SafeLimit> GetSafeLimitByUserName(string userName)
    {
        try
        {
            _logger.LogInformation("Getting safe limit for user: {UserName}", userName);
            
            var userLimit = _safeLimitService.GetSafeLimitByUserName(userName);
            
            if (userLimit == null)
            {
                return NotFound($"User '{userName}' not found");
            }
            
            return Ok(userLimit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving safe limit for user {UserName}", userName);
            return StatusCode(500, "An error occurred while retrieving the safe limit");
        }
    }

    [HttpPost("check")]
    public ActionResult<bool> CheckApprovalLimit([FromBody] ApprovalCheckRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrEmpty(request.UserName))
            {
                return BadRequest("User name is required");
            }

            _logger.LogInformation(
                "Checking if user {UserName} can approve amount {Amount}", 
                request.UserName, request.InvoiceAmount);
                
            var canApprove = _safeLimitService.CheckApprovalLimit(request.UserName, request.InvoiceAmount);
            
            return Ok(new { CanApprove = canApprove });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking approval limit");
            return StatusCode(500, "An error occurred while checking the approval limit");
        }
    }

    [HttpPut("increase")]
    public ActionResult<SafeLimit> IncreaseLimit([FromBody] LimitIncreaseRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrEmpty(request.UserId))
            {
                return BadRequest("User ID is required");
            }

            if (request.NewLimit <= 0)
            {
                return BadRequest("New limit must be greater than zero");
            }

            if (string.IsNullOrEmpty(request.Justification))
            {
                return BadRequest("Justification is required for limit increases");
            }

            _logger.LogInformation(
                "Increasing limit for user {UserId} to {NewLimit}", 
                request.UserId, request.NewLimit);
                
            var updatedLimit = _safeLimitService.IncreaseLimit(
                request.UserId, request.NewLimit, request.Justification);
                
            if (updatedLimit == null)
            {
                return BadRequest("Could not increase limit. Check if the user exists and the new limit is greater than the current limit.");
            }
            
            return Ok(updatedLimit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error increasing limit");
            return StatusCode(500, "An error occurred while increasing the limit");
        }
    }
}