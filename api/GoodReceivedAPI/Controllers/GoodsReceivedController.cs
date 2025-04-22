using GoodReceivedAPI.Models;
using GoodReceivedAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace GoodReceivedAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GoodsReceivedController : ControllerBase
{
    private readonly GoodsReceivedService _goodsReceivedService;
    private readonly ILogger<GoodsReceivedController> _logger;

    public GoodsReceivedController(GoodsReceivedService goodsReceivedService, ILogger<GoodsReceivedController> logger)
    {
        _goodsReceivedService = goodsReceivedService;
        _logger = logger;
    }

    [HttpGet("po/{poNumber}")]
    public ActionResult<List<GoodsReceivedItem>> GetGoodsReceivedByPO(string poNumber, [FromQuery] string? itemId = null)
    {
        try
        {
            _logger.LogInformation("Getting goods received for PO: {PONumber}, Item: {ItemId}", poNumber, itemId ?? "all");
            
            var goodsItems = _goodsReceivedService.GetGoodsReceivedByPO(poNumber, itemId);
            
            if (!goodsItems.Any())
            {
                return NotFound($"No goods received found for PO {poNumber}");
            }
            
            return Ok(goodsItems);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving goods received for PO {PONumber}", poNumber);
            return StatusCode(500, "An error occurred while retrieving goods received data");
        }
    }

    [HttpPut("update")]
    public async Task<ActionResult<GoodsReceivedItem>> UpdateGoodsReceived(
        [FromQuery] string poNumber, 
        [FromQuery] string itemId,
        [FromQuery] string serialNumber, 
        [FromQuery] string assetTagNumber, 
        [FromQuery] string status)
    {
        try
        {
            _logger.LogInformation(
                "Updating goods received for PO: {PONumber}, Item: {ItemId}, Serial: {SerialNumber}, Asset: {AssetTagNumber}, Status: {Status}", 
                poNumber, itemId, serialNumber, assetTagNumber, status);
            
            var updatedItem = await _goodsReceivedService.UpdateGoodsReceivedAsync(
                poNumber, itemId, serialNumber, assetTagNumber, status);
            
            if (updatedItem == null)
            {
                return NotFound($"Item {itemId} on PO {poNumber} not found. Cannot update non-existent record.");
            }
            
            return Ok(updatedItem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating goods received for PO {PONumber}, Item {ItemId}", poNumber, itemId);
            return StatusCode(500, "An error occurred while updating goods received data");
        }
    }
}