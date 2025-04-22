using System.Text.Json.Serialization;

namespace agents.dto;

public class GoodsReceivedItem
{
    [JsonPropertyName("serialNumber")]
    public string? SerialNumber { get; set; }

    [JsonPropertyName("assetTagNumber")]
    public string? AssetTagNumber { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("purchaseOrderNumber")]
    public string? PurchaseOrderNumber { get; set; }
    
    [JsonPropertyName("itemId")]
    public string? ItemId { get; set; }

    [JsonPropertyName("receivedDate")]
    public string? ReceivedDate { get; set; }
}

public class GoodsReceivedDatabase
{
    [JsonPropertyName("goodsReceived")]
    public List<GoodsReceivedItem> Items { get; set; } = [];
}