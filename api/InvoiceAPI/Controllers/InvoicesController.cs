using InvoiceAPI.Models;
using InvoiceAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Text.Json;

namespace InvoiceAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvoicesController : ControllerBase
{
    private readonly InvoiceService _invoiceService;
    private readonly ILogger<InvoicesController> _logger;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public InvoicesController(InvoiceService invoiceService, ILogger<InvoicesController> logger, IConfiguration configuration)
    {
        _invoiceService = invoiceService;
        _logger = logger;
        _configuration = configuration;
        _httpClient = new HttpClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    [HttpGet]
    public ActionResult<List<Invoice>> GetAllInvoices([FromQuery] string? status = null)
    {
        try
        {
            _logger.LogInformation("Getting invoices with status: {Status}", status ?? "all");
            
            if (string.IsNullOrEmpty(status))
            {
                return Ok(_invoiceService.GetAllInvoices());
            }
            
            return Ok(_invoiceService.GetInvoicesByStatus(status));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoices");
            return StatusCode(500, "An error occurred while retrieving invoices");
        }
    }

    [HttpGet("{invoiceNumber}")]
    public ActionResult<Invoice> GetInvoice(string invoiceNumber)
    {
        try
        {
            _logger.LogInformation("Getting invoice details for: {InvoiceNumber}", invoiceNumber);
            
            var invoice = _invoiceService.GetInvoiceByNumber(invoiceNumber);
            
            if (invoice == null)
            {
                return NotFound($"Invoice {invoiceNumber} not found");
            }
            
            return Ok(invoice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoice {InvoiceNumber}", invoiceNumber);
            return StatusCode(500, "An error occurred while retrieving the invoice");
        }
    }

    [HttpPut("{invoiceNumber}/approve")]
    public async Task<ActionResult<Invoice>> ApproveInvoice(string invoiceNumber, [FromBody] ApprovalRequest request)
    {
        try
        {
            _logger.LogInformation("Approving invoice: {InvoiceNumber}", invoiceNumber);
            
            if (request == null || string.IsNullOrEmpty(request.ApproverName))
            {
                return BadRequest("Approver name is required");
            }
            
            // Get the invoice details
            var invoice = _invoiceService.GetInvoiceByNumber(invoiceNumber);
            
            if (invoice == null)
            {
                return NotFound($"Invoice {invoiceNumber} not found");
            }

            // Verify that the approver name matches
            if (!string.Equals(invoice.Approver, request.ApproverName, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest($"Only {invoice.Approver} is authorized to approve this invoice");
            }

            // Check if invoice is already approved
            if (invoice.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest($"Invoice {invoiceNumber} is already approved");
            }

            // Check if invoice is in pending approval status
            if (!invoice.Status.Equals("Pending Approval", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest($"Invoice {invoiceNumber} is not in 'Pending Approval' status");
            }

            // Check if the approver has sufficient safe limit
            string safeLimitApiUrl = _configuration["ApiEndpoints:SafeLimitApi"] ?? "http://safelimitapi:8080";
            var checkUrl = $"{safeLimitApiUrl}/api/SafeLimits/check";
            
            try
            {
                var checkRequest = new 
                {
                    UserName = request.ApproverName,
                    InvoiceAmount = invoice.Total
                };
                
                var checkResponse = await _httpClient.PostAsJsonAsync(checkUrl, checkRequest);
                
                if (!checkResponse.IsSuccessStatusCode)
                {
                    return BadRequest($"Failed to verify approval limit: {checkResponse.StatusCode}");
                }

                var checkContent = await checkResponse.Content.ReadAsStringAsync();
                var limitResult = JsonSerializer.Deserialize<ApprovalLimitCheckResult>(checkContent, _jsonOptions);

                if (limitResult != null && !limitResult.CanApprove)
                {
                    return BadRequest($"Approval limit exceeded. {request.ApproverName} is not authorized to approve invoices of ${invoice.Total}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking safe limit for user {UserName}", request.ApproverName);
                return StatusCode(500, $"Error checking approval limit: {ex.Message}");
            }

            // Validate Purchase Order status (should not be closed)
            string poApiUrl = _configuration["ApiEndpoints:PurchaseOrderApi"] ?? "http://localhost:5294";
            var poUrl = $"{poApiUrl}/api/PurchaseOrders/{invoice.PurchaseOrderNumber}";
            
            try
            {
                var poResponse = await _httpClient.GetAsync(poUrl);
                if (!poResponse.IsSuccessStatusCode)
                {
                    return BadRequest($"Unable to validate purchase order {invoice.PurchaseOrderNumber}");
                }

                var poContent = await poResponse.Content.ReadAsStringAsync();
                var purchaseOrder = JsonSerializer.Deserialize<PurchaseOrder>(poContent, _jsonOptions);

                if (purchaseOrder != null && purchaseOrder.Status.Equals("Closed", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest($"Cannot approve invoice {invoiceNumber} because purchase order {invoice.PurchaseOrderNumber} is closed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating purchase order {PONumber}", invoice.PurchaseOrderNumber);
                return StatusCode(500, $"Error validating purchase order: {ex.Message}");
            }

            // Validate that all goods for the invoice items are received
            string gsrApiUrl = _configuration["ApiEndpoints:GoodsReceivedApi"] ?? "http://localhost:5284";
            
            foreach (var item in invoice.LineItems)
            {
                var gsrUrl = $"{gsrApiUrl}/api/GoodsReceived/po/{invoice.PurchaseOrderNumber}?itemId={item.ItemId}";
                
                try
                {
                    var gsrResponse = await _httpClient.GetAsync(gsrUrl);
                    
                    // If the goods received data is not found or there's an error, reject the approval
                    if (!gsrResponse.IsSuccessStatusCode)
                    {
                        return BadRequest($"Cannot approve invoice {invoiceNumber}. Goods received data for item {item.ItemId} not found.");
                    }

                    var gsrContent = await gsrResponse.Content.ReadAsStringAsync();
                    var goodsItems = JsonSerializer.Deserialize<List<GoodsReceivedItem>>(gsrContent, _jsonOptions);
                    
                    // Check if any goods for this item are not received
                    if (goodsItems == null || !goodsItems.Any() || goodsItems.Any(g => g.Status != "Received"))
                    {
                        return BadRequest($"Cannot approve invoice {invoiceNumber}. Goods for item {item.ItemId} are not marked as received.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error validating goods received for item {ItemId}", item.ItemId);
                    return StatusCode(500, $"Error validating goods received: {ex.Message}");
                }
            }
            
            // All validations passed, approve the invoice
            var updatedInvoice = await _invoiceService.ApproveInvoiceAsync(invoiceNumber);
            
            if (updatedInvoice == null)
            {
                return StatusCode(500, "Failed to update invoice status");
            }
            
            return Ok(updatedInvoice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving invoice {InvoiceNumber}", invoiceNumber);
            return StatusCode(500, "An error occurred while approving the invoice");
        }
    }

    [HttpGet("pending")]
    public ActionResult<List<Invoice>> GetPendingInvoices()
    {
        try
        {
            _logger.LogInformation("Getting pending invoices");
            
            var pendingInvoices = _invoiceService.GetInvoicesByStatus("Pending Approval");
            
            return Ok(pendingInvoices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending invoices");
            return StatusCode(500, "An error occurred while retrieving pending invoices");
        }
    }
}