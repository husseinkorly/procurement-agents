using System.Text.Json;
using GoodReceivedAPI.Models;

namespace GoodReceivedAPI.Services;

public class GoodsReceivedService
{
    private readonly string _goodsReceivedFilePath;
    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = null
    };
    private List<GoodsReceivedItem> _goodsItems = [];

    public GoodsReceivedService(IConfiguration configuration)
    {
        // Set the path to the goods received database
        _goodsReceivedFilePath = configuration["DataFilePath"] ?? 
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "gsr.json");
        
        // Load goods received data immediately
        LoadGoodsReceivedFromFile().GetAwaiter().GetResult();
    }

    // Helper method to read goods received data from the JSON file
    private async Task<List<GoodsReceivedItem>> LoadGoodsReceivedFromFile()
    {
        if (!File.Exists(_goodsReceivedFilePath))
        {
            var directory = Path.GetDirectoryName(_goodsReceivedFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // Create an empty database if the file doesn't exist
            await SaveGoodsReceivedToFile(new List<GoodsReceivedItem>());
            return new List<GoodsReceivedItem>();
        }

        string json = await File.ReadAllTextAsync(_goodsReceivedFilePath);
        
        // Handle empty file case
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<GoodsReceivedItem>();
        }
        
        var database = JsonSerializer.Deserialize<GoodsReceivedDatabase>(json, _jsonOptions);
        _goodsItems = database?.Items ?? new List<GoodsReceivedItem>();
        return _goodsItems;
    }

    // Helper method to save goods received data to the JSON file
    private async Task SaveGoodsReceivedToFile(List<GoodsReceivedItem>? items = null)
    {
        var database = new GoodsReceivedDatabase { goodsReceived = items ?? _goodsItems };
        string json = JsonSerializer.Serialize(database, _jsonOptions);
        await File.WriteAllTextAsync(_goodsReceivedFilePath, json);
    }

    // Get goods received items by purchase order number
    public List<GoodsReceivedItem> GetGoodsReceivedByPO(string poNumber, string? itemId = null)
    {
        var goodsItems = _goodsItems.Where(g => g.PurchaseOrderNumber != null &&
            g.PurchaseOrderNumber.Equals(poNumber, StringComparison.OrdinalIgnoreCase));
            
        if (itemId != null)
        {
            goodsItems = goodsItems.Where(g => g.ItemId != null && 
                g.ItemId.Equals(itemId, StringComparison.OrdinalIgnoreCase));
        }

        return goodsItems.ToList();
    }

    // Update a goods received item
    public async Task<GoodsReceivedItem?> UpdateGoodsReceivedAsync(
        string poNumber, string itemId, string serialNumber, 
        string assetTagNumber, string status)
    {
        // Find if there's an existing entry
        var existingItem = _goodsItems.FirstOrDefault(g => 
            g.PurchaseOrderNumber != null && g.PurchaseOrderNumber.Equals(poNumber, StringComparison.OrdinalIgnoreCase) &&
            g.ItemId != null && g.ItemId.Equals(itemId, StringComparison.OrdinalIgnoreCase));

        if (existingItem != null)
        {
            // Update existing record
            existingItem.SerialNumber = serialNumber;
            existingItem.AssetTagNumber = assetTagNumber;
            existingItem.Status = status;
            existingItem.ReceivedDate = status.Equals("Received", StringComparison.OrdinalIgnoreCase) 
                ? DateTime.Now.ToString("yyyy-MM-dd") 
                : null;
            
            // Save changes
            await SaveGoodsReceivedToFile();
            
            return existingItem;
        }

        return null;
    }
}