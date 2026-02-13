namespace AzureFunctions.Shared.Telemetry;

/// <summary>
/// Abstraction over application telemetry, enabling consistent tracking of events,
/// metrics, dependencies, and exceptions across all Azure Function implementations.
/// </summary>
public interface ITelemetryService
{
    /// <summary>
    /// Tracks a named custom event with optional properties.
    /// </summary>
    /// <param name="name">The event name.</param>
    /// <param name="properties">Optional key-value pairs providing additional context.</param>
    void TrackEvent(string name, IDictionary<string, string>? properties = null);

    /// <summary>
    /// Tracks a named metric value with optional properties.
    /// </summary>
    /// <param name="name">The metric name.</param>
    /// <param name="value">The metric value.</param>
    /// <param name="properties">Optional key-value pairs providing additional context.</param>
    void TrackMetric(string name, double value, IDictionary<string, string>? properties = null);

    /// <summary>
    /// Tracks a dependency call (e.g., HTTP, SQL, Azure Storage) with timing and success information.
    /// </summary>
    /// <param name="type">The dependency type (e.g., "HTTP", "SQL", "Azure Blob").</param>
    /// <param name="name">The dependency name or target.</param>
    /// <param name="data">The command or call detail (e.g., URL, query).</param>
    /// <param name="startTime">The UTC start time of the dependency call.</param>
    /// <param name="duration">The duration of the dependency call.</param>
    /// <param name="success">Whether the dependency call succeeded.</param>
    void TrackDependency(string type, string name, string data, DateTimeOffset startTime, TimeSpan duration, bool success);

    /// <summary>
    /// Tracks an exception with optional properties for diagnostic context.
    /// </summary>
    /// <param name="exception">The exception to track.</param>
    /// <param name="properties">Optional key-value pairs providing additional context.</param>
    void TrackException(Exception exception, IDictionary<string, string>? properties = null);
}
