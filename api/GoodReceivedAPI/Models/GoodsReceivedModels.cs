namespace GoodReceivedAPI.Models;

public class GoodsReceivedItem
{
    public string? id { get; set; }
    public string? SerialNumber { get; set; }
    public string? AssetTagNumber { get; set; }
    public string? Status { get; set; }
    public string? PurchaseOrderNumber { get; set; }
    public string? ItemId { get; set; }
    public string? ReceivedDate { get; set; }
}

// Class to store Cosmos DB configuration
public class CosmosDbOptions
{
    public string? EndpointUri { get; set; }
    public string? PrimaryKey { get; set; }
    public string? DatabaseName { get; set; }
    public string? ContainerName { get; set; }
}