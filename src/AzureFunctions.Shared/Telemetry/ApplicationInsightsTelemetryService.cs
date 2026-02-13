using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace AzureFunctions.Shared.Telemetry;

/// <summary>
/// Application Insights implementation of <see cref="ITelemetryService"/>.
/// Delegates all telemetry tracking to the underlying <see cref="TelemetryClient"/>.
/// </summary>
public sealed class ApplicationInsightsTelemetryService : ITelemetryService
{
    private readonly TelemetryClient _telemetryClient;

    /// <summary>
    /// Initializes a new instance of <see cref="ApplicationInsightsTelemetryService"/>.
    /// </summary>
    /// <param name="telemetryClient">The Application Insights telemetry client.</param>
    public ApplicationInsightsTelemetryService(TelemetryClient telemetryClient)
    {
        _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
    }

    /// <inheritdoc />
    public void TrackEvent(string name, IDictionary<string, string>? properties = null)
    {
        _telemetryClient.TrackEvent(name, properties);
    }

    /// <inheritdoc />
    public void TrackMetric(string name, double value, IDictionary<string, string>? properties = null)
    {
        var metric = new MetricTelemetry(name, value);

        if (properties is not null)
        {
            foreach (var kvp in properties)
            {
                metric.Properties[kvp.Key] = kvp.Value;
            }
        }

        _telemetryClient.TrackMetric(metric);
    }

    /// <inheritdoc />
    public void TrackDependency(
        string type,
        string name,
        string data,
        DateTimeOffset startTime,
        TimeSpan duration,
        bool success)
    {
        var dependency = new DependencyTelemetry(type, target: name, name, data)
        {
            Timestamp = startTime,
            Duration = duration,
            Success = success
        };

        _telemetryClient.TrackDependency(dependency);
    }

    /// <inheritdoc />
    public void TrackException(Exception exception, IDictionary<string, string>? properties = null)
    {
        _telemetryClient.TrackException(exception, properties);
    }
}
