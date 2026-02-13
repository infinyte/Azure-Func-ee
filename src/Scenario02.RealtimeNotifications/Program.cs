using Azure.Data.Tables;
using Azure.Identity;
using Azure.Storage.Queues;
using AzureFunctions.Shared.Extensions;
using AzureFunctions.Shared.Middleware;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scenario02.RealtimeNotifications.Models;
using Scenario02.RealtimeNotifications.Repositories;
using Scenario02.RealtimeNotifications.Services;

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
        services.Configure<NotificationOptions>(
            configuration.GetSection(NotificationOptions.SectionName));

        // Register Azure Storage SDK clients.
        var storageConnectionString = configuration["AzureWebJobsStorage"];

        if (!string.IsNullOrEmpty(storageConnectionString)
            && storageConnectionString != "UseDevelopmentStorage=true"
            && storageConnectionString.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            var storageUri = new Uri(storageConnectionString);
            var credential = new DefaultAzureCredential();

            services.AddSingleton(_ => new TableServiceClient(storageUri, credential));
            services.AddSingleton(_ => new QueueServiceClient(storageUri, credential));
        }
        else
        {
            var connectionString = storageConnectionString ?? "UseDevelopmentStorage=true";
            services.AddSingleton(_ => new TableServiceClient(connectionString));
            services.AddSingleton(_ => new QueueServiceClient(connectionString));
        }

        // Register repositories.
        services.AddSingleton<INotificationRepository, TableStorageNotificationRepository>();
        services.AddSingleton<ISubscriptionRepository, TableStorageSubscriptionRepository>();

        // Register application services.
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<IEmailService, SimulatedEmailService>();
        services.AddSingleton<ITemplateService, TemplateService>();
    })
    .Build();

host.Run();
