namespace InvoiceAPI.Models;

public class InvoiceLineItem
{
    public string? ItemId { get; set; }
    public string? Description { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class Invoice
{
    public string? InvoiceNumber { get; set; }
    public string? PurchaseOrderNumber { get; set; }
    public string? SupplierName { get; set; }
    public string? SupplierId { get; set; }
    public string? InvoiceDate { get; set; }
    public string? DueDate { get; set; }
    public string? Approver { get; set; }
    public List<InvoiceLineItem> LineItems { get; set; } = [];
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Shipping { get; set; }
    public decimal Total { get; set; }
    public string? Currency { get; set; }
    public string? Status { get; set; }
}

public class InvoiceDatabase
{
    public List<Invoice> Invoices { get; set; } = [];
}

public class ApprovalRequest
{
    public string? ApproverName { get; set; }
}