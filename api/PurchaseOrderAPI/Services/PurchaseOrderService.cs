using System.Text.Json;
using PurchaseOrderAPI.Models;

namespace PurchaseOrderAPI.Services;

public class PurchaseOrderService
{
    private readonly string _purchaseOrdersFilePath;
    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = null // Keep property names as-is
    };
    private List<PurchaseOrder> _purchaseOrders = [];

    public PurchaseOrderService(IConfiguration configuration)
    {
        // Set the path to the purchase orders database
        _purchaseOrdersFilePath = configuration["DataFilePath"] ?? 
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "purchase-orders.json");
        
        // Load purchase orders immediately
        LoadPurchaseOrdersFromFile().GetAwaiter().GetResult();
    }

    // Helper method to read purchase orders from the JSON file
    private async Task<List<PurchaseOrder>> LoadPurchaseOrdersFromFile()
    {
        if (!File.Exists(_purchaseOrdersFilePath))
        {
            var directory = Path.GetDirectoryName(_purchaseOrdersFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // Create an empty database if the file doesn't exist
            await SavePurchaseOrdersToFile(new List<PurchaseOrder>());
            return new List<PurchaseOrder>();
        }

        string json = await File.ReadAllTextAsync(_purchaseOrdersFilePath);
        
        // Handle empty file case
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<PurchaseOrder>();
        }
        
        var database = JsonSerializer.Deserialize<PurchaseOrderDatabase>(json, _jsonOptions);
        _purchaseOrders = database?.PurchaseOrders ?? new List<PurchaseOrder>();
        return _purchaseOrders;
    }

    // Helper method to save purchase orders to the JSON file
    private async Task SavePurchaseOrdersToFile(List<PurchaseOrder>? purchaseOrders = null)
    {
        var database = new PurchaseOrderDatabase { PurchaseOrders = purchaseOrders ?? _purchaseOrders };
        string json = JsonSerializer.Serialize(database, _jsonOptions);
        await File.WriteAllTextAsync(_purchaseOrdersFilePath, json);
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

    // Update a purchase order status
    public async Task<PurchaseOrder?> UpdatePurchaseOrderStatusAsync(string poNumber, string status)
    {
        var purchaseOrder = GetPurchaseOrderByNumber(poNumber);
        
        if (purchaseOrder == null)
        {
            return null;
        }

        // Update the status
        purchaseOrder.Status = status;
        
        // Save changes
        await SavePurchaseOrdersToFile();
        
        return purchaseOrder;
    }
    
    // Decrement the draft count for a purchase order
    public async Task<PurchaseOrder?> DecrementDraftCountAsync(string poNumber)
    {
        var purchaseOrder = GetPurchaseOrderByNumber(poNumber);
        
        if (purchaseOrder == null)
        {
            return null;
        }

        // Only decrement if the draft count is greater than 0
        if (purchaseOrder.Drafts > 0)
        {
            purchaseOrder.Drafts--;
            
            // Save changes
            await SavePurchaseOrdersToFile();
        }
        
        return purchaseOrder;
    }
}