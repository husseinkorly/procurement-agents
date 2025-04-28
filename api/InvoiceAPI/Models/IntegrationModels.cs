namespace InvoiceAPI.Models;

public class PurchaseOrder
{
    public string? PurchaseOrderNumber { get; set; }
    public string? SupplierName { get; set; }
    public string? Status { get; set; }
    public DateTime? OrderDate { get; set; }
    public bool AutoCore { get; set; }
    public decimal Total { get; set; }
    public List<PurchaseOrderItem>? Items { get; set; }
}

public class PurchaseOrderItem
{
    public string? ItemId { get; set; }
    public string? Description { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
