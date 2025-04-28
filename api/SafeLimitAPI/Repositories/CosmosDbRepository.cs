using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace SafeLimitAPI.Repositories
{
    /// <summary>
    /// Implementation of Cosmos DB repository
    /// </summary>
    public class CosmosDbRepository<T> : ICosmosDbRepository<T> where T : class
    {
        private readonly CosmosClient _cosmosClient;
        private readonly ILogger<CosmosDbRepository<T>> _logger;
        private readonly string _databaseName;
        private readonly string _containerName;
        private Container _container;

        public CosmosDbRepository(
            CosmosClient cosmosClient,
            string databaseName,
            string containerName,
            ILogger<CosmosDbRepository<T>> logger)
        {
            _cosmosClient = cosmosClient;
            _databaseName = databaseName;
            _containerName = containerName;
            _logger = logger;
            _container = _cosmosClient.GetContainer(_databaseName, _containerName);
        }

        /// <summary>
        /// Initialize the database and container if they don't exist
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                // Create the database if it doesn't exist
                DatabaseResponse database = await _cosmosClient.CreateDatabaseIfNotExistsAsync(_databaseName);
                _logger.LogInformation("Database {DatabaseName} initialized", _databaseName);

                // Create the container if it doesn't exist
                ContainerResponse container = await database.Database.CreateContainerIfNotExistsAsync(
                    id: _containerName,
                    partitionKeyPath: "/UserName",
                    throughput: 400
                );
                _container = container.Container;
                _logger.LogInformation("Container {ContainerName} initialized", _containerName);
            }
            catch (CosmosException ex)
            {
                _logger.LogError(ex, "Error initializing database or container. StatusCode: {StatusCode}", ex.StatusCode);
                throw;
            }
        }

        /// <summary>
        /// Get an item by its ID and partition key
        /// </summary>
        public async Task<T> GetByIdAsync(string id, string partitionKey)
        {
            try
            {
                ItemResponse<T> response = await _container.ReadItemAsync<T>(id, new PartitionKey(partitionKey));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Item with id {Id} not found", id);
                return null;
            }
            catch (CosmosException ex)
            {
                _logger.LogError(ex, "Error getting item with ID {Id}. StatusCode: {StatusCode}", id, ex.StatusCode);
                throw;
            }
        }

        /// <summary>
        /// Get all items, optionally filtered by a SQL query
        /// </summary>
        public async Task<IEnumerable<T>> GetAllAsync(string? queryString = null)
        {
            try
            {
                List<T> results = new List<T>();

                QueryDefinition query = queryString != null 
                    ? new QueryDefinition(queryString) 
                    : new QueryDefinition("SELECT * FROM c");

                // Use query iterator with max concurrency for better performance
                FeedIterator<T> iterator = _container.GetItemQueryIterator<T>(
                    query,
                    requestOptions: new QueryRequestOptions 
                    { 
                        MaxConcurrency = 10, // Allow parallel query execution
                        MaxItemCount = 100   // Items per page
                    }
                );

                while (iterator.HasMoreResults)
                {
                    FeedResponse<T> response = await iterator.ReadNextAsync();
                    results.AddRange(response);
                }

                return results;
            }
            catch (CosmosException ex)
            {
                _logger.LogError(ex, "Error getting all items. StatusCode: {StatusCode}", ex.StatusCode);
                throw;
            }
        }

        /// <summary>
        /// Create a new item in the container
        /// </summary>
        public async Task<T> CreateAsync(T item)
        {
            try
            {
                ItemResponse<T> response = await _container.CreateItemAsync(item);
                _logger.LogInformation("Item created. Request charge: {RequestCharge} RU", response.RequestCharge);
                return response.Resource;
            }
            catch (CosmosException ex)
            {
                _logger.LogError(ex, "Error creating item. StatusCode: {StatusCode}", ex.StatusCode);
                throw;
            }
        }

        /// <summary>
        /// Update an existing item in the container
        /// </summary>
        public async Task<T> UpdateAsync(T item, string id, string partitionKey)
        {
            try
            {
                ItemResponse<T> response = await _container.UpsertItemAsync(
                    item, 
                    new PartitionKey(partitionKey),
                    new ItemRequestOptions { IfMatchEtag = null } // No ETag check for now
                );
                
                _logger.LogInformation("Item with ID {Id} updated. Request charge: {RequestCharge} RU", id, response.RequestCharge);
                return response.Resource;
            }
            catch (CosmosException ex)
            {
                _logger.LogError(ex, "Error updating item with ID {Id}. StatusCode: {StatusCode}", id, ex.StatusCode);
                throw;
            }
        }

        /// <summary>
        /// Delete an item from the container
        /// </summary>
        public async Task DeleteAsync(string id, string partitionKey)
        {
            try
            {
                ItemResponse<T> response = await _container.DeleteItemAsync<T>(id, new PartitionKey(partitionKey));
                _logger.LogInformation("Item with ID {Id} deleted. Request charge: {RequestCharge} RU", id, response.RequestCharge);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Item with ID {Id} not found for deletion", id);
            }
            catch (CosmosException ex)
            {
                _logger.LogError(ex, "Error deleting item with ID {Id}. StatusCode: {StatusCode}", id, ex.StatusCode);
                throw;
            }
        }
    }
}