using System.Text.Json;
using GoodReceivedAPI.Models;
using GoodReceivedAPI.Repositories;

namespace GoodReceivedAPI.Services;

public class GoodsReceivedService
{
    private readonly ICosmosDbRepository<GoodsReceivedItem> _goodsRepository;
    private readonly ILogger<GoodsReceivedService> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = null
    };
    private List<GoodsReceivedItem> _goodsItems = []; // In-memory cache

    public GoodsReceivedService(
        ICosmosDbRepository<GoodsReceivedItem> goodsRepository,
        ILogger<GoodsReceivedService> logger,
        IConfiguration configuration)
    {
        _goodsRepository = goodsRepository;
        _logger = logger;
        
        // Initialize and load goods received items asynchronously
        InitializeRepositoryAndLoadItems().GetAwaiter().GetResult();
    }

    // Initialize the repository and load goods received items
    private async Task InitializeRepositoryAndLoadItems()
    {
        try
        {
            // Initialize the repository (create database and container if they don't exist)
            await _goodsRepository.InitializeAsync();
            
            // Load all goods received items from Cosmos DB to in-memory cache
            _goodsItems = (await _goodsRepository.GetAllAsync()).ToList();
            _logger.LogInformation("Loaded {Count} goods received items from Cosmos DB", _goodsItems.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing repository and loading goods received items");
            // Initialize with empty list on error
            _goodsItems = [];
        }
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

    // Create a new goods received item
    public async Task<GoodsReceivedItem> CreateGoodsReceivedAsync(GoodsReceivedItem item)
    {
        try
        {
            // Set LastModified timestamp
            item.LastModified = DateTime.UtcNow;
            
            // Create in Cosmos DB
            var createdItem = await _goodsRepository.CreateAsync(item);
            
            // Add to in-memory cache
            _goodsItems.Add(createdItem);
            
            return createdItem;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating goods received item");
            throw;
        }
    }

    // Update a goods received item
    public async Task<GoodsReceivedItem?> UpdateGoodsReceivedAsync(
        string poNumber, string itemId, string serialNumber, 
        string assetTagNumber, string status)
    {
        try
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
                existingItem.LastModified = DateTime.UtcNow;
                
                // Update in Cosmos DB
                var updatedItem = await _goodsRepository.UpdateAsync(
                    existingItem, 
                    existingItem.Id, 
                    existingItem.PurchaseOrderNumber);
                
                // Update in-memory cache
                var index = _goodsItems.FindIndex(i => i.Id == existingItem.Id);
                if (index >= 0)
                {
                    _goodsItems[index] = updatedItem;
                }
                
                return updatedItem;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating goods received item");
            throw;
        }
    }

    // Refresh the in-memory cache from Cosmos DB
    public async Task RefreshGoodsReceivedCacheAsync()
    {
        try
        {
            _goodsItems = (await _goodsRepository.GetAllAsync()).ToList();
            _logger.LogInformation("Refreshed goods received cache with {Count} items", _goodsItems.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing goods received cache");
        }
    }
}