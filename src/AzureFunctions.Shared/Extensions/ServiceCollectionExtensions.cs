using AzureFunctions.Shared.Telemetry;
using Microsoft.Extensions.DependencyInjection;

namespace AzureFunctions.Shared.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> to register shared services
/// used across Azure Function projects.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all shared services including Application Insights telemetry
    /// and the <see cref="ITelemetryService"/> abstraction.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The configured <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddSharedServices(this IServiceCollection services)
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.AddSingleton<ITelemetryService, ApplicationInsightsTelemetryService>();

        return services;
    }
}
