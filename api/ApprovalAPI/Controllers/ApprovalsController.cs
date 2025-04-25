using ApprovalAPI.Models;
using ApprovalAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApprovalAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApprovalsController : ControllerBase
{
    private readonly ApprovalService _approvalService;
    private readonly ILogger<ApprovalsController> _logger;

    public ApprovalsController(ApprovalService approvalService, ILogger<ApprovalsController> logger)
    {
        _approvalService = approvalService;
        _logger = logger;
    }

    [HttpPost("{invoiceNumber}/approve")]
    public async Task<ActionResult<ApprovalResponse>> ApproveInvoice(string invoiceNumber, [FromBody] ApprovalRequest request)
    {
        try
        {
            _logger.LogInformation("Received approval request for invoice: {InvoiceNumber}", invoiceNumber);
            
            if (request == null || string.IsNullOrEmpty(request.ApproverName))
            {
                return BadRequest("Approver name is required");
            }
            
            // Use the service to process the approval
            var result = await _approvalService.ApproveInvoiceAsync(invoiceNumber, request.ApproverName);
            
            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing approval for invoice {InvoiceNumber}", invoiceNumber);
            return StatusCode(500, "An error occurred while processing the approval request");
        }
    }
    
    [HttpGet("history")]
    public async Task<ActionResult<List<ApprovalHistory>>> GetApprovalHistory([FromQuery] string? invoiceNumber = null)
    {
        try
        {
            _logger.LogInformation("Getting approval history for invoice: {InvoiceNumber}", invoiceNumber ?? "all invoices");
            
            var history = await _approvalService.GetApprovalHistoryAsync(invoiceNumber);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving approval history");
            return StatusCode(500, "An error occurred while retrieving approval history");
        }
    }
    
    [HttpGet("{invoiceNumber}/history")]
    public async Task<ActionResult<List<ApprovalHistory>>> GetInvoiceApprovalHistory(string invoiceNumber)
    {
        try
        {
            _logger.LogInformation("Getting approval history for specific invoice: {InvoiceNumber}", invoiceNumber);
            
            var history = await _approvalService.GetApprovalHistoryAsync(invoiceNumber);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving approval history for invoice {InvoiceNumber}", invoiceNumber);
            return StatusCode(500, "An error occurred while retrieving approval history");
        }
    }
}