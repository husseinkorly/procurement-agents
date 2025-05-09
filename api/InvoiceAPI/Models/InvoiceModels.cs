using System.Text.Json.Serialization;

namespace InvoiceAPI.Models;

public class Invoice
{
    public string? id { get; set; }
    public string? InvoiceNumber { get; set; }
    public string? MicrosoftInvoiceNumber { get; set; }
    public string? PurchaseOrderNumber { get; set; }
    public string? SupplierName { get; set; }
    public string? SupplierId { get; set; }
    public string? InvoiceDate { get; set; }
    public string? DueDate { get; set; }
    public string? Approver { get; set; }
    public bool AutoCore { get; set; }
    public List<InvoiceLineItem> LineItems { get; set; } = [];
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Shipping { get; set; }
    public decimal Total { get; set; }
    public string? Currency { get; set; }
    public string? Status { get; set; }
}

public class InvoiceLineItem
{
    public string? ItemId { get; set; }
    public string? Description { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class CosmosDbOptions
{
    public string? EndpointUri { get; set; }
    public string? PrimaryKey { get; set; }
    public string? DatabaseName { get; set; }
    public string? ContainerName { get; set; }
}