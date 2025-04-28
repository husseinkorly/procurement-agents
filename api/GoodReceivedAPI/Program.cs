using GoodReceivedAPI.Models;
using GoodReceivedAPI.Repositories;
using GoodReceivedAPI.Services;
using Microsoft.Azure.Cosmos;

namespace GoodReceivedAPI;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();

        // Configure Cosmos DB
        ConfigureCosmosDb(builder);

        // Add CORS policy for cross-service communication
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        app.UseHttpsRedirection();
        app.UseCors();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }

    private static void ConfigureCosmosDb(WebApplicationBuilder builder)
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

        // Register repository for GoodsReceivedItem
        builder.Services.AddSingleton<ICosmosDbRepository<GoodsReceivedItem>>(serviceProvider =>
        {
            var cosmosClient = serviceProvider.GetRequiredService<CosmosClient>();
            var logger = serviceProvider.GetRequiredService<ILogger<CosmosDbRepository<GoodsReceivedItem>>>();

            return new CosmosDbRepository<GoodsReceivedItem>(
                cosmosClient,
                cosmosDbOptions.DatabaseName ?? throw new ArgumentNullException(nameof(cosmosDbOptions.DatabaseName)),
                cosmosDbOptions.ContainerName ?? throw new ArgumentNullException(nameof(cosmosDbOptions.ContainerName)),
                logger);
        });

        // Register Goods Received Service
        builder.Services.AddSingleton<GoodsReceivedService>();
    }
}
