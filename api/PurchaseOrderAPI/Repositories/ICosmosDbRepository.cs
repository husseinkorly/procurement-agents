using PurchaseOrderAPI.Models;

namespace PurchaseOrderAPI.Repositories
{
    /// <summary>
    /// Interface for Cosmos DB repository operations
    /// </summary>
    public interface ICosmosDbRepository<T> where T : class
    {
        /// <summary>
        /// Get an item by its ID and partition key
        /// </summary>
        Task<T> GetByIdAsync(string id, string partitionKey);
        
        /// <summary>
        /// Get all items, optionally filtered by a SQL query
        /// </summary>
        Task<IEnumerable<T>> GetAllAsync(string? queryString = null);
        
        /// <summary>
        /// Create a new item in the container
        /// </summary>
        Task<T> CreateAsync(T item);
        
        /// <summary>
        /// Update an existing item in the container
        /// </summary>
        Task<T> UpdateAsync(T item, string id, string partitionKey);
        
        /// <summary>
        /// Delete an item from the container
        /// </summary>
        Task DeleteAsync(string id, string partitionKey);
        
        /// <summary>
        /// Create a database and container if they don't exist
        /// </summary>
        Task InitializeAsync();
    }
}