namespace PurchaseOrderAPI.Models;

public class PurchaseOrderLineItem
{
    public string? ItemId { get; set; }
    public string? Description { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Status { get; set; }
}

public class PurchaseOrder
{
    // Add ID property required for Cosmos DB
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string? PurchaseOrderNumber { get; set; }
    public string? SupplierName { get; set; }
    public string? SupplierId { get; set; }
    public string? OrderDate { get; set; }
    public string? ExpectedDeliveryDate { get; set; }
    public string? ShippingAddress { get; set; }
    public bool AutoCore { get; set; }
    public List<PurchaseOrderLineItem> LineItems { get; set; } = [];
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Shipping { get; set; }
    public decimal Total { get; set; }
    public string? Currency { get; set; }
    public string? Status { get; set; }
    public string? RequestorName { get; set; }
    public string? ApprovalDate { get; set; }
    // Add timestamp for optimistic concurrency
    public string? _etag { get; set; }
    // Add timestamp for tracking when the record was last modified
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}

public class PurchaseOrderDatabase
{
    public List<PurchaseOrder> PurchaseOrders { get; set; } = [];
}

// Class to store Cosmos DB configuration
public class CosmosDbOptions
{
    public string? EndpointUri { get; set; }
    public string? PrimaryKey { get; set; }
    public string? DatabaseName { get; set; }
    public string? ContainerName { get; set; }
}