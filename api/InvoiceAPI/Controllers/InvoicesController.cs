using InvoiceAPI.Models;
using InvoiceAPI.Services;
using Microsoft.AspNetCore.Mvc;
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

    public InvoicesController(InvoiceService invoiceService, ILogger<InvoicesController> logger, IConfiguration configuration)
    {
        _invoiceService = invoiceService;
        _logger = logger;
        _configuration = configuration;
        _httpClient = new HttpClient();
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

    [HttpPut("{invoiceNumber}/status")]
    public async Task<ActionResult<Invoice>> UpdateInvoiceStatus(string invoiceNumber, [FromBody] string newStatus)
    {
        try
        {
            _logger.LogInformation("Updating invoice status: {InvoiceNumber} to {Status}",
                invoiceNumber, newStatus);

            if (string.IsNullOrEmpty(newStatus))
            {
                return BadRequest("New status are required");
            }

            // Get the invoice details
            var invoice = _invoiceService.GetInvoiceByNumber(invoiceNumber);

            if (invoice == null)
            {
                return NotFound($"Invoice {invoiceNumber} not found");
            }

            // Update the status
            var updatedInvoice = await _invoiceService.UpdateInvoiceStatusAsync(
                invoiceNumber, newStatus);

            if (updatedInvoice == null)
            {
                return StatusCode(500, "Failed to update invoice status");
            }

            return Ok(updatedInvoice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for invoice {InvoiceNumber}", invoiceNumber);
            return StatusCode(500, "An error occurred while updating the invoice status");
        }
    }

    [HttpPost]
    public async Task<ActionResult<Invoice>> CreateInvoice([FromBody] Invoice invoice)
    {
        try
        {
            _logger.LogInformation("Creating new invoice for PO: {PONumber}", invoice.PurchaseOrderNumber);

            // Validation checks
            List<string> validationErrors = new List<string>();

            if (string.IsNullOrEmpty(invoice.PurchaseOrderNumber))
            {
                validationErrors.Add("Purchase order number is required");
            }

            if (string.IsNullOrEmpty(invoice.SupplierName))
            {
                validationErrors.Add("Supplier name is required");
            }

            if (string.IsNullOrEmpty(invoice.SupplierId))
            {
                validationErrors.Add("Supplier ID is required");
            }

            if (invoice.LineItems == null || invoice.LineItems.Count == 0)
            {
                validationErrors.Add("At least one line item is required");
            }

            if (validationErrors.Count > 0)
            {
                return BadRequest(new { Errors = validationErrors });
            }

            // Verify that the purchase order exists and is not closed
            string poApiUrl = _configuration["ApiEndpoints:PurchaseOrderApi"] ?? "http://localhost:5294";
            var poUrl = $"{poApiUrl}/api/PurchaseOrders/{invoice.PurchaseOrderNumber}";
            try
            {
                var poResponse = await _httpClient.GetAsync(poUrl);
                if (!poResponse.IsSuccessStatusCode)
                {
                    return BadRequest($"Purchase order {invoice.PurchaseOrderNumber} could not be found or validated");
                }

                var poContent = await poResponse.Content.ReadAsStringAsync();
                var purchaseOrder = JsonSerializer.Deserialize<PurchaseOrder>(poContent);

                if (purchaseOrder?.Status != null && purchaseOrder.Status.Equals("Closed", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest($"Cannot create invoice because purchase order {invoice.PurchaseOrderNumber} is closed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating purchase order {PONumber}", invoice.PurchaseOrderNumber);
                return StatusCode(500, $"Error validating purchase order: {ex.Message}");
            }

            // Create the invoice
            try
            {
                var createdInvoice = await _invoiceService.CreateInvoiceAsync(invoice);

                if (createdInvoice == null)
                {
                    _logger.LogError("Error creating invoice - service returned null ");
                    return StatusCode(500, "Unknown error creating invoice - service returned null");
                }

                return CreatedAtAction(nameof(GetInvoice), new { invoiceNumber = createdInvoice.InvoiceNumber }, createdInvoice);
            }
            catch (InvalidOperationException ex)
            {
                // These are business rule violations (duplicate invoice number, duplicate PO, etc.)
                _logger.LogWarning(ex, "Business rule violation when creating invoice");

                // Return the detailed error message from the service
                return Conflict(new { Message = ex.Message }); // HTTP 409 Conflict
            }
            catch (Exception ex)
            {
                // Log the detailed exception
                _logger.LogError(ex, "Unhandled exception in CreateInvoice");
                return StatusCode(500, $"Failed to create invoice: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating invoice for PO {PONumber}", invoice.PurchaseOrderNumber);
            return StatusCode(500, $"An error occurred while creating the invoice: {ex.Message}");
        }
    }
}