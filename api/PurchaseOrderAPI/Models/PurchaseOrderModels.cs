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
    public string? PurchaseOrderNumber { get; set; }
    public string? SupplierName { get; set; }
    public string? SupplierId { get; set; }
    public string? OrderDate { get; set; }
    public string? ExpectedDeliveryDate { get; set; }
    public string? ShippingAddress { get; set; }
    public List<PurchaseOrderLineItem> LineItems { get; set; } = [];
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Shipping { get; set; }
    public decimal Total { get; set; }
    public string? Currency { get; set; }
    public string? Status { get; set; }
    public string? RequestorName { get; set; }
    public string? ApprovalDate { get; set; }
}

public class PurchaseOrderDatabase
{
    public List<PurchaseOrder> PurchaseOrders { get; set; } = [];
}