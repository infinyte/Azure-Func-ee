using System.Text.Json.Serialization;

namespace AzureFunctions.Shared.Models;

/// <summary>
/// Represents a standardized error response returned from Azure Function endpoints.
/// </summary>
public record ErrorResponse
{
    /// <summary>
    /// A human-readable error message describing what went wrong.
    /// </summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    /// <summary>
    /// Optional additional detail about the error (e.g., validation specifics, inner exception info).
    /// </summary>
    [JsonPropertyName("detail")]
    public string? Detail { get; init; }

    /// <summary>
    /// The correlation ID for tracing this request across services.
    /// </summary>
    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; init; }

    /// <summary>
    /// A machine-readable error code for programmatic error handling.
    /// </summary>
    [JsonPropertyName("errorCode")]
    public required string ErrorCode { get; init; }

    /// <summary>
    /// The UTC timestamp when the error occurred.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
