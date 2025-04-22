namespace GoodReceivedAPI.Models;

public class GoodsReceivedItem
{
    public string? SerialNumber { get; set; }
    public string? AssetTagNumber { get; set; }
    public string? Status { get; set; }
    public string? PurchaseOrderNumber { get; set; }
    public string? ItemId { get; set; }
    public string? ReceivedDate { get; set; }
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