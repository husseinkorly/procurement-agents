using ApprovalAPI.Models;
using ApprovalAPI.Repositories;
using ApprovalAPI.Services;
using Microsoft.Azure.Cosmos;

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

    // Register repository for ApprovalHistory
    builder.Services.AddSingleton<ICosmosDbRepository<ApprovalHistory>>(serviceProvider =>
    {
        var cosmosClient = serviceProvider.GetRequiredService<CosmosClient>();
        var logger = serviceProvider.GetRequiredService<ILogger<CosmosDbRepository<ApprovalHistory>>>();

        if (string.IsNullOrEmpty(cosmosDbOptions.DatabaseName) || string.IsNullOrEmpty(cosmosDbOptions.ContainerName))
        {
            throw new ArgumentNullException("CosmosDb configuration", "DatabaseName and ContainerName must be specified in the configuration");
        }

        return new CosmosDbRepository<ApprovalHistory>(
            cosmosClient,
            cosmosDbOptions.DatabaseName,
            cosmosDbOptions.ContainerName,
            logger);
    });

    // Register Approval Service
    builder.Services.AddSingleton<ApprovalService>();
}
