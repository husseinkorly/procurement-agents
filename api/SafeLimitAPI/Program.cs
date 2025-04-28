using Microsoft.Azure.Cosmos;
using SafeLimitAPI.Models;
using SafeLimitAPI.Repositories;
using SafeLimitAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure Cosmos DB
ConfigureCosmosDb(builder);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

void ConfigureCosmosDb(WebApplicationBuilder builder)
{
    // Bind Cosmos DB configuration
    var cosmosDbOptions = new CosmosDbOptions();
    builder.Configuration.GetSection("CosmosDb").Bind(cosmosDbOptions);

    // Register CosmosClient as singleton
    builder.Services.AddSingleton(serviceProvider =>
    {
        var logger = serviceProvider.GetRequiredService<ILogger<CosmosClient>>();
        
        // Configure Cosmos Client options for resilient connections
        var options = new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Direct,
            SerializerOptions = new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            },
            MaxRetryAttemptsOnRateLimitedRequests = 9,
            MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(30)
        };

        logger.LogInformation("Initializing Cosmos DB client with endpoint {Endpoint}", cosmosDbOptions.EndpointUri);
        
        return new CosmosClient(
            cosmosDbOptions.EndpointUri,
            cosmosDbOptions.PrimaryKey,
            options);
    });

    // Register repository for SafeLimit
    builder.Services.AddSingleton<ICosmosDbRepository<SafeLimit>>(serviceProvider =>
    {
        var cosmosClient = serviceProvider.GetRequiredService<CosmosClient>();
        var logger = serviceProvider.GetRequiredService<ILogger<CosmosDbRepository<SafeLimit>>>();
        
        return new CosmosDbRepository<SafeLimit>(
            cosmosClient,
            cosmosDbOptions.DatabaseName,
            cosmosDbOptions.ContainerName,
            logger);
    });

    // Register Safe Limit Service
    builder.Services.AddSingleton<SafeLimitService>();
}
