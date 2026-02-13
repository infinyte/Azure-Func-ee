using Azure.Data.Tables;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using AzureFunctions.Shared.Middleware;
using AzureFunctions.Shared.Telemetry;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scenario01.DocumentProcessing.Models;
using Scenario01.DocumentProcessing.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication(builder =>
    {
        builder.UseMiddleware<CorrelationIdMiddleware>();
        builder.UseMiddleware<ExceptionHandlingMiddleware>();
    })
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        // Bind configuration options.
        services.Configure<DocumentProcessingOptions>(
            configuration.GetSection("DocumentProcessing"));

        // Register Application Insights telemetry.
        services.AddApplicationInsightsTelemetryWorkerService();

        // Register shared telemetry service.
        services.AddSingleton<ITelemetryService, ApplicationInsightsTelemetryService>();

        // Register Azure Storage SDK clients.
        // Prefer managed identity via DefaultAzureCredential for deployed environments;
        // fall back to connection strings for local development.
        var storageConnectionString = configuration["AzureWebJobsStorage"];

        if (!string.IsNullOrEmpty(storageConnectionString)
            && storageConnectionString != "UseDevelopmentStorage=true"
            && storageConnectionString.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            // URI-based registration with DefaultAzureCredential (managed identity).
            var storageUri = new Uri(storageConnectionString);
            var credential = new DefaultAzureCredential();

            services.AddSingleton(_ => new TableServiceClient(storageUri, credential));
            services.AddSingleton(_ => new BlobServiceClient(storageUri, credential));
            services.AddSingleton(_ => new QueueServiceClient(storageUri, credential));
        }
        else
        {
            // Connection string-based registration (local development / emulator).
            var connectionString = storageConnectionString ?? "UseDevelopmentStorage=true";
            services.AddSingleton(_ => new TableServiceClient(connectionString));
            services.AddSingleton(_ => new BlobServiceClient(connectionString));
            services.AddSingleton(_ => new QueueServiceClient(connectionString));
        }

        // Register application services.
        services.AddSingleton<IDocumentRepository, TableStorageDocumentRepository>();
        services.AddSingleton<IDocumentProcessingService, DocumentProcessingService>();
        services.AddSingleton<IClassificationService, SimpleClassificationService>();
    })
    .Build();

host.Run();
