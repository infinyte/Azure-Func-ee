namespace Scenario05.ScheduledEtlPipeline.Models;

/// <summary>
/// Represents a single data record extracted from a source system.
/// Uses a dynamic field dictionary to support heterogeneous data schemas.
/// </summary>
public sealed class DataRecord
{
    /// <summary>
    /// The unique identifier for this record.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The name of the source system this record was extracted from.
    /// </summary>
    public string SourceName { get; set; } = string.Empty;

    /// <summary>
    /// The timestamp when this record was extracted.
    /// </summary>
    public DateTimeOffset ExtractedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Dynamic key-value fields representing the record data.
    /// </summary>
    public Dictionary<string, string> Fields { get; set; } = new();
}
