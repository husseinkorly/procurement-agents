namespace ApprovalAPI.Models;


public class ApprovalResponse
{
    public string? InvoiceNumber { get; set; }
    public string? Status { get; set; }
    public string? Message { get; set; }
    public bool Success { get; set; }
}

public class ApprovalLimitCheckResult
{
    public bool CanApprove { get; set; }
    public decimal ApprovalLimit { get; set; }
    public decimal InvoiceAmount { get; set; }
}

// Integration models for communicating with other APIs
public class Invoice
{
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

public class PurchaseOrder
{
    public string? PurchaseOrderNumber { get; set; }
    public string? SupplierName { get; set; }
    public string? SupplierId { get; set; }
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

public class GoodsReceivedItem
{
    public string? SerialNumber { get; set; }
    public string? AssetTagNumber { get; set; }
    public string? Status { get; set; }
    public string? PurchaseOrderNumber { get; set; }
    public string? ItemId { get; set; }
    public string? ReceivedDate { get; set; }
}

// Class to track approval history
public class ApprovalHistory
{
    public string? id { get; set; }
    public string? InvoiceNumber { get; set; }
    public string? ApproverName { get; set; }
    public string? Action { get; set; }
    public string? Comments { get; set; }
}

// Class to store Cosmos DB configuration
public class CosmosDbOptions
{
    public string? EndpointUri { get; set; }
    public string? PrimaryKey { get; set; }
    public string? DatabaseName { get; set; }
    public string? ContainerName { get; set; }
}