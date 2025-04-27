using Microsoft.AspNetCore.Mvc;
using PurchaseOrderAPI.Models;
using PurchaseOrderAPI.Services;

namespace PurchaseOrderAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PurchaseOrdersController : ControllerBase
{
    private readonly PurchaseOrderService _purchaseOrderService;
    private readonly ILogger<PurchaseOrdersController> _logger;

    public PurchaseOrdersController(PurchaseOrderService purchaseOrderService, ILogger<PurchaseOrdersController> logger)
    {
        _purchaseOrderService = purchaseOrderService;
        _logger = logger;
    }

    [HttpGet]
    public ActionResult<List<PurchaseOrder>> GetAllPurchaseOrders([FromQuery] string? status = null)
    {
        try
        {
            _logger.LogInformation("Getting purchase orders with status: {Status}", status ?? "all");
            
            if (string.IsNullOrEmpty(status))
            {
                return Ok(_purchaseOrderService.GetAllPurchaseOrders());
            }
            
            return Ok(_purchaseOrderService.GetPurchaseOrdersByStatus(status));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving purchase orders");
            return StatusCode(500, "An error occurred while retrieving purchase orders");
        }
    }

    [HttpGet("{poNumber}")]
    public ActionResult<PurchaseOrder> GetPurchaseOrder(string poNumber)
    {
        try
        {
            _logger.LogInformation("Getting purchase order details for: {PONumber}", poNumber);
            
            var purchaseOrder = _purchaseOrderService.GetPurchaseOrderByNumber(poNumber);
            
            if (purchaseOrder == null)
            {
                return NotFound($"Purchase order {poNumber} not found");
            }
            
            return Ok(purchaseOrder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving purchase order {PONumber}", poNumber);
            return StatusCode(500, "An error occurred while retrieving the purchase order");
        }
    }

    [HttpPut("{poNumber}/status")]
    public async Task<ActionResult<PurchaseOrder>> UpdatePurchaseOrderStatus(string poNumber, [FromQuery] string status)
    {
        try
        {
            _logger.LogInformation("Updating purchase order status for: {PONumber} to {Status}", poNumber, status);
            
            var purchaseOrder = await _purchaseOrderService.UpdatePurchaseOrderStatusAsync(poNumber, status);
            
            if (purchaseOrder == null)
            {
                return NotFound($"Purchase order {poNumber} not found");
            }
            
            return Ok(purchaseOrder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating purchase order {PONumber} status", poNumber);
            return StatusCode(500, "An error occurred while updating the purchase order status");
        }
    }
    
    [HttpPut("{poNumber}/decrement-draft")]
    public async Task<ActionResult<PurchaseOrder>> DecrementDraftCount(string poNumber)
    {
        try
        {
            _logger.LogInformation("Decrementing draft count for purchase order: {PONumber}", poNumber);
            
            var updatedPO = await _purchaseOrderService.DecrementDraftCountAsync(poNumber);
            
            if (updatedPO == null)
            {
                return NotFound($"Purchase order {poNumber} not found");
            }
            
            return Ok(updatedPO);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrementing draft count for purchase order {PONumber}", poNumber);
            return StatusCode(500, "An error occurred while updating the purchase order draft count");
        }
    }
}