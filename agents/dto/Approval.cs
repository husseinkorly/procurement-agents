using System.Text.Json.Serialization;

namespace agents.dto;

public class ApprovalResponse
{
    [JsonPropertyName("invoiceNumber")]
    public string? InvoiceNumber { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; }
}

public class ApprovalHistory
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("invoiceNumber")]
    public string? InvoiceNumber { get; set; }

    [JsonPropertyName("approverName")]
    public string? ApproverName { get; set; }

    [JsonPropertyName("action")]
    public string? Action { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("comments")]
    public string? Comments { get; set; }
}