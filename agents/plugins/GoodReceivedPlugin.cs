using System.ComponentModel;
using System.Text.Json;
using Microsoft.SemanticKernel;
using agents.dto;

namespace agents.plugins;

public class GoodReceivedPlugin
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = null
    };

    public GoodReceivedPlugin()
    {
        _httpClient = new HttpClient();
        _baseUrl = "http://localhost:5284";
    }

    [KernelFunction("get_good_received")]
    [Description("Get the goods received information for a specific purchase order and item.")]
    public async Task<string> GetGoodReceived(
        [Description("The purchase order number")] string poNumber,
        [Description("The item ID (optional)")] string? itemId = null)
    {
        try
        {
            string url = $"{_baseUrl}/api/GoodsReceived/po/{poNumber}";

            if (!string.IsNullOrEmpty(itemId))
            {
                url += $"?itemId={itemId}";
            }

            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                
                // Deserialize the response into a list or single item
                if (string.IsNullOrEmpty(itemId))
                {
                    // Multiple items expected - format them nicely
                    var goodsItems = JsonSerializer.Deserialize<List<GoodsReceivedItem>>(content, _jsonOptions);
                    
                    if (goodsItems == null || !goodsItems.Any())
                    {
                        return $"No goods received found for Purchase Order {poNumber}.";
                    }
                    
                    var result = new System.Text.StringBuilder();
                    result.AppendLine($"Found {goodsItems.Count} goods received record(s) for Purchase Order: {poNumber}");
                    result.AppendLine();
                    
                    foreach (var item in goodsItems)
                    {
                        result.AppendLine($"Item ID: {item.ItemId}");
                        result.AppendLine($"Status: {item.Status}");
                        
                        if (!string.IsNullOrEmpty(item.SerialNumber))
                            result.AppendLine($"Serial Number: {item.SerialNumber}");
                            
                        if (!string.IsNullOrEmpty(item.AssetTagNumber))
                            result.AppendLine($"Asset Tag: {item.AssetTagNumber}");
                            
                        if (!string.IsNullOrEmpty(item.ReceivedDate))
                            result.AppendLine($"Received Date: {item.ReceivedDate}");
                            
                        result.AppendLine();
                    }
                    
                    return result.ToString().TrimEnd();
                }
                else
                {
                    // Single item expected
                    var goodsItem = JsonSerializer.Deserialize<GoodsReceivedItem>(content, _jsonOptions);
                    
                    if (goodsItem == null)
                    {
                        return $"Error parsing goods received data for PO: {poNumber}, Item: {itemId}.";
                    }
                    
                    var result = new System.Text.StringBuilder();
                    result.AppendLine($"Goods received information for PO: {poNumber}, Item: {itemId}");
                    result.AppendLine($"Status: {goodsItem.Status}");
                    
                    if (!string.IsNullOrEmpty(goodsItem.SerialNumber))
                        result.AppendLine($"Serial Number: {goodsItem.SerialNumber}");
                        
                    if (!string.IsNullOrEmpty(goodsItem.AssetTagNumber))
                        result.AppendLine($"Asset Tag: {goodsItem.AssetTagNumber}");
                        
                    if (!string.IsNullOrEmpty(goodsItem.ReceivedDate))
                        result.AppendLine($"Received Date: {goodsItem.ReceivedDate}");
                    
                    return result.ToString();
                }
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                if (itemId != null)
                    return $"No goods received information found for PO: {poNumber} and Item ID: {itemId}.";
                else
                    return $"No goods received information found for PO: {poNumber}.";
            }
            else
            {
                return $"Error retrieving goods received data: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}";
            }
        }
        catch (Exception ex)
        {
            return $"Error connecting to Goods Received API: {ex.Message}";
        }
    }

    [KernelFunction("update_good_received")]
    [Description("""
            Update the goods received information for a specific item.
                - serial number, asset tag number must be provided by user before updating the status.
        """)]
    public async Task<string> UpdateGoodReceivedAsync(
        [Description("The purchase order number")] string poNumber,
        [Description("The item ID")] string itemId,
        [Description("The serial number")] string serialNumber,
        [Description("The asset tag number")] string assetTagNumber,
        [Description("The goods received status (Received or Not Received)")] string status)
    {
        try
        {
            string url = $"{_baseUrl}/api/GoodsReceived/update?poNumber={poNumber}&itemId={itemId}&serialNumber={serialNumber}&assetTagNumber={assetTagNumber}&status={status}";

            var response = await _httpClient.PutAsync(url, null);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var updatedItem = JsonSerializer.Deserialize<GoodsReceivedItem>(content, _jsonOptions);
                
                if (updatedItem == null)
                {
                    return $"Item updated successfully, but could not parse the response data.";
                }
                
                var result = new System.Text.StringBuilder();
                result.AppendLine($"Successfully updated goods received for PO: {poNumber}, Item: {itemId}");
                result.AppendLine($"Status: {updatedItem.Status}");
                result.AppendLine($"Serial Number: {updatedItem.SerialNumber}");
                result.AppendLine($"Asset Tag: {updatedItem.AssetTagNumber}");
                
                if (!string.IsNullOrEmpty(updatedItem.ReceivedDate))
                {
                    result.AppendLine($"Received Date: {updatedItem.ReceivedDate}");
                }
                
                return result.ToString();
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return $"Error: No goods received record found for Purchase Order {poNumber} and Item ID {itemId}. Cannot update non-existent record.";
            }
            else
            {
                return $"Error updating goods received data: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}";
            }
        }
        catch (Exception ex)
        {
            return $"Error connecting to Goods Received API: {ex.Message}";
        }
    }
}
