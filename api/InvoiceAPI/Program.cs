using InvoiceAPI.Models;
using InvoiceAPI.Repositories;
using InvoiceAPI.Services;
using Microsoft.Azure.Cosmos;

namespace InvoiceAPI;

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
        // Bind Cosmos DB configuration from appsettings.json
        var cosmosDbOptions = new CosmosDbOptions();
        builder.Configuration.GetSection("CosmosDb").Bind(cosmosDbOptions);

        // Register CosmosClient as a singleton
        builder.Services.AddSingleton(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<CosmosClient>>();
            // Configure Cosmos Client options
            var options = new CosmosClientOptions
            {
                ConnectionMode = ConnectionMode.Direct,
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.Default,
                    IgnoreNullValues = false
                },
                // Configure retry options for resilient connections
                MaxRetryAttemptsOnRateLimitedRequests = 9,
                MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(30)
            };
            logger.LogInformation("Initializing Cosmos DB client with endpoint {Endpoint}", cosmosDbOptions.EndpointUri);

            return new CosmosClient(
                cosmosDbOptions.EndpointUri,
                cosmosDbOptions.PrimaryKey,
                options);
        });

        // Register generic repository for Invoice
        builder.Services.AddSingleton<ICosmosDbRepository<Invoice>>(serviceProvider =>
        {
            var cosmosClient = serviceProvider.GetRequiredService<CosmosClient>();
            var logger = serviceProvider.GetRequiredService<ILogger<CosmosDbRepository<Invoice>>>();

            return new CosmosDbRepository<Invoice>(
                cosmosClient,
                cosmosDbOptions.DatabaseName ?? throw new ArgumentNullException(nameof(cosmosDbOptions.DatabaseName)),
                cosmosDbOptions.ContainerName ?? throw new ArgumentNullException(nameof(cosmosDbOptions.ContainerName)),
                logger);
        });

        // Register our Invoice Service as a singleton
        builder.Services.AddSingleton<InvoiceService>();
    }
}