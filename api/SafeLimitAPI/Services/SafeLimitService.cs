using System.Text.Json;
using SafeLimitAPI.Models;

namespace SafeLimitAPI.Services;

public class SafeLimitService
{
    private readonly ILogger<SafeLimitService> _logger;
    private readonly string _dataFilePath;
    private readonly JsonSerializerOptions _jsonOptions;

    public SafeLimitService(ILogger<SafeLimitService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _dataFilePath = configuration["DataFilePath"] ?? "data/safe-limits.json";
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
    }

    public List<SafeLimit> GetAllSafeLimits()
    {
        try
        {
            var database = LoadDatabase();
            return database.SafeLimits;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving safe limits");
            throw;
        }
    }

    public SafeLimit? GetSafeLimitByUserId(string userId)
    {
        try
        {
            var database = LoadDatabase();
            return database.SafeLimits.FirstOrDefault(sl => sl.UserId == userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving safe limit for user {UserId}", userId);
            throw;
        }
    }
    
    public SafeLimit? GetSafeLimitByUserName(string userName)
    {
        try
        {
            var database = LoadDatabase();
            return database.SafeLimits.FirstOrDefault(sl => 
                string.Equals(sl.UserName, userName, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving safe limit for user {UserName}", userName);
            throw;
        }
    }

    public bool CheckApprovalLimit(string userName, decimal invoiceAmount)
    {
        try
        {
            var userLimit = GetSafeLimitByUserName(userName);
            
            if (userLimit == null)
            {
                _logger.LogWarning("User {UserName} not found in safe limits database", userName);
                return false;
            }

            return userLimit.ApprovalLimit >= invoiceAmount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking approval limit for user {UserName}", userName);
            throw;
        }
    }

    public SafeLimit? IncreaseLimit(string userId, decimal newLimit, string justification)
    {
        try
        {
            var database = LoadDatabase();
            var userLimit = database.SafeLimits.FirstOrDefault(sl => sl.UserId == userId);

            if (userLimit == null)
            {
                _logger.LogWarning("User {UserId} not found in safe limits database", userId);
                return null;
            }

            // Only allow increasing the limit, not decreasing
            if (newLimit <= userLimit.ApprovalLimit)
            {
                _logger.LogWarning(
                    "New limit ({NewLimit}) must be greater than current limit ({CurrentLimit})",
                    newLimit, userLimit.ApprovalLimit);
                return null;
            }

            // Update the user's limit
            userLimit.ApprovalLimit = newLimit;
            
            // Save the updated database
            SaveDatabase(database);
            
            _logger.LogInformation(
                "Approval limit for user {UserId} increased to {NewLimit}. Justification: {Justification}",
                userId, newLimit, justification);
                
            return userLimit;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error increasing limit for user {UserId}", userId);
            throw;
        }
    }

    private SafeLimitDatabase LoadDatabase()
    {
        if (!File.Exists(_dataFilePath))
        {
            _logger.LogWarning("Safe limits database file not found at {FilePath}", _dataFilePath);
            return new SafeLimitDatabase();
        }

        var json = File.ReadAllText(_dataFilePath);
        var database = JsonSerializer.Deserialize<SafeLimitDatabase>(json, _jsonOptions);
        
        return database ?? new SafeLimitDatabase();
    }

    private void SaveDatabase(SafeLimitDatabase database)
    {
        var json = JsonSerializer.Serialize(database, _jsonOptions);
        File.WriteAllText(_dataFilePath, json);
    }
}