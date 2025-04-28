namespace SafeLimitAPI.Models;

public class SafeLimit
{
    // Add ID property required for Cosmos DB
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public decimal ApprovalLimit { get; set; }
    public string? Currency { get; set; }
    public string? Role { get; set; }
    // Add timestamp for optimistic concurrency
    public string? _etag { get; set; }
    // Add timestamp for tracking when the record was last modified
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
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

// Class to store Cosmos DB configuration
public class CosmosDbOptions
{
    public string? EndpointUri { get; set; }
    public string? PrimaryKey { get; set; }
    public string? DatabaseName { get; set; }
    public string? ContainerName { get; set; }
}