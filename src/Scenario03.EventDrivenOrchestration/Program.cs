using Azure.Identity;
using AzureFunctions.Shared.Extensions;
using AzureFunctions.Shared.Middleware;
using AzureFunctions.Shared.Resilience;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Scenario03.EventDrivenOrchestration.Models;
using Scenario03.EventDrivenOrchestration.Repositories;
using Scenario03.EventDrivenOrchestration.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication(builder =>
    {
        builder.UseMiddleware<CorrelationIdMiddleware>();
        builder.UseMiddleware<ExceptionHandlingMiddleware>();
    })
    .ConfigureServices((context, services) =>
    {
        // Register shared services (Application Insights, telemetry).
        services.AddSharedServices();

        // Bind configuration options.
        services.Configure<OrderProcessingOptions>(
            context.Configuration.GetSection(OrderProcessingOptions.SectionName));

        // Register Cosmos DB client and container.
        services.AddSingleton<CosmosClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<OrderProcessingOptions>>().Value;

            var cosmosOptions = new CosmosClientOptions
            {
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                }
            };

            // Use DefaultAzureCredential when no connection string is configured (production).
            if (string.IsNullOrWhiteSpace(options.CosmosConnectionString))
            {
                var accountEndpoint = context.Configuration["CosmosAccountEndpoint"]
                    ?? throw new InvalidOperationException(
                        "Either OrderProcessing:CosmosConnectionString or CosmosAccountEndpoint must be configured.");

                return new CosmosClient(accountEndpoint, new DefaultAzureCredential(), cosmosOptions);
            }

            return new CosmosClient(options.CosmosConnectionString, cosmosOptions);
        });

        services.AddSingleton<Container>(sp =>
        {
            var cosmosClient = sp.GetRequiredService<CosmosClient>();
            var options = sp.GetRequiredService<IOptions<OrderProcessingOptions>>().Value;

            return cosmosClient.GetContainer(options.CosmosDatabaseName, options.CosmosContainerName);
        });

        // Register repositories.
        services.AddSingleton<IOrderRepository, CosmosDbOrderRepository>();

        // Register application services.
        services.AddSingleton<IOrderService, OrderService>();
        services.AddSingleton<IInventoryService, InventoryService>();
        services.AddSingleton<IPaymentService, PaymentService>();

        // Register HTTP clients with standard resilience policies for external service calls.
        services.AddHttpClient("InventoryService", client =>
        {
            client.BaseAddress = new Uri(
                context.Configuration["InventoryService:BaseUrl"] ?? "https://inventory-api.internal");
            client.Timeout = TimeSpan.FromSeconds(30);
        }).AddStandardResilience();

        services.AddHttpClient("PaymentService", client =>
        {
            client.BaseAddress = new Uri(
                context.Configuration["PaymentService:BaseUrl"] ?? "https://payment-api.internal");
            client.Timeout = TimeSpan.FromSeconds(30);
        }).AddStandardResilience();
    })
    .Build();

host.Run();
