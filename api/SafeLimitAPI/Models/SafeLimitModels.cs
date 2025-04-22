namespace SafeLimitAPI.Models;

public class SafeLimit
{
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public decimal ApprovalLimit { get; set; }
    public string? Currency { get; set; }
    public string? Role { get; set; }
}

public class SafeLimitDatabase
{
    public List<SafeLimit> SafeLimits { get; set; } = [];
}

public class LimitIncreaseRequest
{
    public string? UserId { get; set; }
    public decimal NewLimit { get; set; }
    public string? Justification { get; set; }
}

public class ApprovalCheckRequest
{
    public string? UserName { get; set; }
    public decimal InvoiceAmount { get; set; }
}