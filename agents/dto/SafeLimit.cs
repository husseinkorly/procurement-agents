using System.Text.Json.Serialization;

namespace agents.dto;

public class SafeLimit
{
    [JsonPropertyName("userId")]
    public string? UserId { get; set; }

    [JsonPropertyName("userName")]
    public string? UserName { get; set; }

    [JsonPropertyName("approvalLimit")]
    public decimal ApprovalLimit { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }
}

public class ApprovalCheckResult
{
    [JsonPropertyName("canApprove")]
    public bool CanApprove { get; set; }
}