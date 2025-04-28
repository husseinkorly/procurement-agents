using System.Text.Json;
using PurchaseOrderAPI.Models;
using PurchaseOrderAPI.Repositories;

namespace PurchaseOrderAPI.Services;

public class PurchaseOrderService
{
    private readonly ICosmosDbRepository<PurchaseOrder> _purchaseOrderRepository;
    private readonly ILogger<PurchaseOrderService> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = null // Keep property names as-is
    };
    private List<PurchaseOrder> _purchaseOrders = []; // In-memory cache

    public PurchaseOrderService(
        ICosmosDbRepository<PurchaseOrder> purchaseOrderRepository,
        ILogger<PurchaseOrderService> logger,
        IConfiguration configuration)
    {
        _purchaseOrderRepository = purchaseOrderRepository;
        _logger = logger;
        
        // Initialize and load purchase orders asynchronously
        InitializeRepositoryAndLoadPurchaseOrders().GetAwaiter().GetResult();
    }

    // Initialize the repository and load purchase orders
    private async Task InitializeRepositoryAndLoadPurchaseOrders()
    {
        try
        {
            // Initialize the repository (create database and container if they don't exist)
            await _purchaseOrderRepository.InitializeAsync();
            
            // Load all purchase orders from Cosmos DB to in-memory cache
            _purchaseOrders = (await _purchaseOrderRepository.GetAllAsync()).ToList();
            _logger.LogInformation("Loaded {Count} purchase orders from Cosmos DB", _purchaseOrders.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing repository and loading purchase orders");
            // Initialize with empty list on error
            _purchaseOrders = [];
        }
    }

    // Get all purchase orders
    public List<PurchaseOrder> GetAllPurchaseOrders()
    {
        return _purchaseOrders;
    }

    // Get a specific purchase order by number
    public PurchaseOrder? GetPurchaseOrderByNumber(string poNumber)
    {
        return _purchaseOrders.FirstOrDefault(po => po.PurchaseOrderNumber != null &&
            po.PurchaseOrderNumber.Equals(poNumber, StringComparison.OrdinalIgnoreCase));
    }

    // Get purchase orders with a specific status
    public List<PurchaseOrder> GetPurchaseOrdersByStatus(string status)
    {
        return _purchaseOrders.Where(po => po.Status != null &&
            po.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    // Create a new purchase order
    public async Task<PurchaseOrder> CreatePurchaseOrderAsync(PurchaseOrder purchaseOrder)
    {
        try
        {
            // Set Id to be the same as PurchaseOrderNumber for easier retrieval
            if (!string.IsNullOrEmpty(purchaseOrder.PurchaseOrderNumber))
            {
                purchaseOrder.Id = purchaseOrder.PurchaseOrderNumber;
            }
            
            purchaseOrder.LastModified = DateTime.UtcNow;
            
            // Create in Cosmos DB
            var createdPO = await _purchaseOrderRepository.CreateAsync(purchaseOrder);
            
            // Add to in-memory cache
            _purchaseOrders.Add(createdPO);
            
            return createdPO;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating purchase order");
            throw;
        }
    }

    // Update a purchase order status
    public async Task<PurchaseOrder?> UpdatePurchaseOrderStatusAsync(string poNumber, string status)
    {
        try
        {
            var purchaseOrder = GetPurchaseOrderByNumber(poNumber);
            
            if (purchaseOrder == null)
            {
                return null;
            }

            // Update the status
            purchaseOrder.Status = status;
            purchaseOrder.LastModified = DateTime.UtcNow;
            
            // Update in Cosmos DB
            var updatedPO = await _purchaseOrderRepository.UpdateAsync(
                purchaseOrder, 
                purchaseOrder.Id, 
                purchaseOrder.PurchaseOrderNumber);
            
            // Update in-memory cache
            var index = _purchaseOrders.FindIndex(po => po.PurchaseOrderNumber == poNumber);
            if (index >= 0)
            {
                _purchaseOrders[index] = updatedPO;
            }
            
            return updatedPO;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating purchase order status");
            throw;
        }
    }

    // Refresh the in-memory cache from Cosmos DB
    public async Task RefreshPurchaseOrderCacheAsync()
    {
        try
        {
            _purchaseOrders = (await _purchaseOrderRepository.GetAllAsync()).ToList();
            _logger.LogInformation("Refreshed purchase order cache with {Count} purchase orders", _purchaseOrders.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing purchase order cache");
        }
    }
}