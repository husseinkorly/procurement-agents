namespace GoodReceivedAPI.Models;

public class GoodsReceivedItem
{
    // Add ID property required for Cosmos DB
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string? SerialNumber { get; set; }
    public string? AssetTagNumber { get; set; }
    public string? Status { get; set; }
    public string? PurchaseOrderNumber { get; set; }
    public string? ItemId { get; set; }
    public string? ReceivedDate { get; set; }
    // Add timestamp for optimistic concurrency
    public string? _etag { get; set; }
    // Add timestamp for tracking when the record was last modified
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}

public class GoodsReceivedDatabase
{
    public List<GoodsReceivedItem> goodsReceived { get; set; } = [];
    
    [System.Text.Json.Serialization.JsonIgnore]
    public List<GoodsReceivedItem> Items 
    { 
        get => goodsReceived;
        set => goodsReceived = value;
    }
}

// Class to store Cosmos DB configuration
public class CosmosDbOptions
{
    public string? EndpointUri { get; set; }
    public string? PrimaryKey { get; set; }
    public string? DatabaseName { get; set; }
    public string? ContainerName { get; set; }
}