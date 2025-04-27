using System.Text.Json.Serialization;

namespace agents.dto;

public class PurchaseOrder
{
    [JsonPropertyName("purchaseOrderNumber")]
    public string? PurchaseOrderNumber { get; set; }

    [JsonPropertyName("supplierName")]
    public string? SupplierName { get; set; }

    [JsonPropertyName("supplierId")]
    public string? SupplierId { get; set; }

    [JsonPropertyName("orderDate")]
    public string? OrderDate { get; set; }

    [JsonPropertyName("expectedDeliveryDate")]
    public string? ExpectedDeliveryDate { get; set; }

    [JsonPropertyName("shippingAddress")]
    public string? ShippingAddress { get; set; }
    
    [JsonPropertyName("autoCore")]
    public bool AutoCore { get; set; }

    [JsonPropertyName("lineItems")]
    public List<POLineItem>? LineItems { get; set; }

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

    [JsonPropertyName("requestorName")]
    public string? RequestorName { get; set; }

    [JsonPropertyName("approvalDate")]
    public string? ApprovalDate { get; set; }

    [JsonPropertyName("drafts")]
    public int Drafts { get; set; }
}

public class POLineItem
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

public class PurchaseOrderDatabase
{
    [JsonPropertyName("purchaseOrders")]
    public List<PurchaseOrder> PurchaseOrders { get; set; } = [];
}