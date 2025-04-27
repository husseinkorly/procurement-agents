using System.Text.Json.Serialization;

namespace agents.dto;

public class Invoice
{
    [JsonPropertyName("invoiceNumber")]
    public string? InvoiceNumber { get; set; }

    [JsonPropertyName("PurchaseOrderNumber")]
    public string? PurchaseOrderNumber { get; set; }

    [JsonPropertyName("SupplierName")]
    public string? SupplierName { get; set; }

    [JsonPropertyName("SupplierId")]
    public string? SupplierId { get; set; }

    [JsonPropertyName("InvoiceDate")]
    public string? InvoiceDate { get; set; }

    [JsonPropertyName("DueDate")]
    public string? DueDate { get; set; }

    [JsonPropertyName("approver")]
    public string? Approver { get; set; }
    
    [JsonPropertyName("autoCore")]
    public bool AutoCore { get; set; }

    [JsonPropertyName("lineItems")]
    public List<LineItem>? LineItems { get; set; }

    [JsonPropertyName("subtotal")]
    public double Subtotal { get; set; }

    [JsonPropertyName("tax")]
    public double Tax { get; set; }

    [JsonPropertyName("shipping")]
    public double Shipping { get; set; }

    [JsonPropertyName("total")]
    public double Total { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

public class LineItem
{
    [JsonPropertyName("itemId")]
    public string? ItemId { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("unitPrice")]
    public double UnitPrice { get; set; }

    [JsonPropertyName("totalPrice")]
    public double TotalPrice { get; set; }
}

public class InvoiceDatabase
{
    [JsonPropertyName("Invoices")]
    public List<Invoice> Invoices { get; set; } = [];
}