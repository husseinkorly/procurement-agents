using System.Text.Json;
using SafeLimitAPI.Models;
using SafeLimitAPI.Repositories;

namespace SafeLimitAPI.Services;

public class SafeLimitService
{
    private readonly ICosmosDbRepository<SafeLimit> _safeLimitRepository;
    private readonly ILogger<SafeLimitService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private List<SafeLimit> _safeLimits = []; // In-memory cache

    public SafeLimitService(
        ICosmosDbRepository<SafeLimit> safeLimitRepository,
        ILogger<SafeLimitService> logger, 
        IConfiguration configuration)
    {
        _safeLimitRepository = safeLimitRepository;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
        
        // Initialize and load safe limits asynchronously
        InitializeRepositoryAndLoadSafeLimits().GetAwaiter().GetResult();
    }

    // Initialize the repository and load safe limits
    private async Task InitializeRepositoryAndLoadSafeLimits()
    {
        try
        {
            // Initialize the repository (create database and container if they don't exist)
            await _safeLimitRepository.InitializeAsync();
            
            // Load all safe limits from Cosmos DB to in-memory cache
            _safeLimits = (await _safeLimitRepository.GetAllAsync()).ToList();
            _logger.LogInformation("Loaded {Count} safe limits from Cosmos DB", _safeLimits.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing repository and loading safe limits");
            // Initialize with empty list on error
            _safeLimits = [];
        }
    }

    public List<SafeLimit> GetAllSafeLimits()
    {
        try
        {
            return _safeLimits;
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
            return _safeLimits.FirstOrDefault(sl => sl.UserId == userId);
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
            return _safeLimits.FirstOrDefault(sl => 
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

    public async Task<SafeLimit?> IncreaseLimit(string userId, decimal newLimit, string justification)
    {
        try
        {
            var userLimit = GetSafeLimitByUserId(userId);

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
            userLimit.LastModified = DateTime.UtcNow;
            
            // Update in Cosmos DB
            var updatedLimit = await _safeLimitRepository.UpdateAsync(
                userLimit, 
                userLimit.Id, 
                userLimit.UserName);
                
            // Update in-memory cache
            var index = _safeLimits.FindIndex(sl => sl.Id == userLimit.Id);
            if (index >= 0)
            {
                _safeLimits[index] = updatedLimit;
            }
            
            _logger.LogInformation(
                "Approval limit for user {UserId} increased to {NewLimit}. Justification: {Justification}",
                userId, newLimit, justification);
                
            return updatedLimit;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error increasing limit for user {UserId}", userId);
            throw;
        }
    }

    // Create a new safe limit
    public async Task<SafeLimit> CreateSafeLimit(SafeLimit safeLimit)
    {
        try
        {
            // Set LastModified timestamp
            safeLimit.LastModified = DateTime.UtcNow;
            
            // Create in Cosmos DB
            var createdLimit = await _safeLimitRepository.CreateAsync(safeLimit);
            
            // Add to in-memory cache
            _safeLimits.Add(createdLimit);
            
            return createdLimit;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating safe limit");
            throw;
        }
    }

    // Refresh the in-memory cache from Cosmos DB
    public async Task RefreshSafeLimitCacheAsync()
    {
        try
        {
            _safeLimits = (await _safeLimitRepository.GetAllAsync()).ToList();
            _logger.LogInformation("Refreshed safe limit cache with {Count} items", _safeLimits.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing safe limit cache");
        }
    }
}