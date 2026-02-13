namespace Scenario05.ScheduledEtlPipeline.Models.Dtos;

/// <summary>
/// Request DTO for manually triggering an ETL pipeline run.
/// </summary>
/// <param name="Sources">Optional list of source names to extract from. Null means all sources.</param>
/// <param name="Description">Optional description for this manual run.</param>
public sealed record TriggerEtlRequest(
    IReadOnlyList<string>? Sources = null,
    string? Description = null);
