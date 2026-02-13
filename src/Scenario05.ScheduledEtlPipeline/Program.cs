using Azure.Data.Tables;
using Azure.Identity;
using Azure.Storage.Blobs;
using AzureFunctions.Shared.Extensions;
using AzureFunctions.Shared.Middleware;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scenario05.ScheduledEtlPipeline.Models;
using Scenario05.ScheduledEtlPipeline.Repositories;
using Scenario05.ScheduledEtlPipeline.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication(builder =>
    {
        builder.UseMiddleware<CorrelationIdMiddleware>();
        builder.UseMiddleware<ExceptionHandlingMiddleware>();
    })
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        // Register shared services (Application Insights, telemetry).
        services.AddSharedServices();

        // Bind configuration options.
        services.Configure<EtlOptions>(
            configuration.GetSection(EtlOptions.SectionName));

        // Register Azure Storage SDK clients.
        var storageConnectionString = configuration["AzureWebJobsStorage"];

        if (!string.IsNullOrEmpty(storageConnectionString)
            && storageConnectionString != "UseDevelopmentStorage=true"
            && storageConnectionString.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            var storageUri = new Uri(storageConnectionString);
            var credential = new DefaultAzureCredential();

            services.AddSingleton(_ => new TableServiceClient(storageUri, credential));
            services.AddSingleton(_ => new BlobServiceClient(storageUri, credential));
        }
        else
        {
            var connectionString = storageConnectionString ?? "UseDevelopmentStorage=true";
            services.AddSingleton(_ => new TableServiceClient(connectionString));
            services.AddSingleton(_ => new BlobServiceClient(connectionString));
        }

        // Register repositories.
        services.AddSingleton<IPipelineRepository, TableStoragePipelineRepository>();

        // Register application services.
        services.AddSingleton<IPipelineService, PipelineService>();
        services.AddSingleton<IDataValidator, RuleBasedDataValidator>();
        services.AddSingleton<IDataTransformer, MappingDataTransformer>();
        services.AddSingleton<IExternalApiClient, SimulatedExternalApiClient>();

        // Register HTTP client with resilience for external API calls.
        services.AddHttpClient("ExternalApi", client =>
        {
            client.BaseAddress = new Uri(
                configuration["EtlPipeline:ExternalApiBaseUrl"] ?? "https://api.external-data.internal");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
    })
    .Build();

host.Run();
