namespace Scenario05.ScheduledEtlPipeline.Models.Dtos;

/// <summary>
/// Response DTO representing the status of a pipeline run.
/// </summary>
public sealed record PipelineRunResponse
{
    /// <summary>
    /// The unique identifier for this pipeline run.
    /// </summary>
    public string RunId { get; init; } = string.Empty;

    /// <summary>
    /// The current status of the pipeline run.
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// The date and time the pipeline run was started.
    /// </summary>
    public DateTimeOffset StartedAt { get; init; }

    /// <summary>
    /// The date and time the pipeline run completed, if applicable.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>
    /// The trigger source that initiated this run.
    /// </summary>
    public string TriggerSource { get; init; } = string.Empty;

    /// <summary>
    /// The total number of records extracted.
    /// </summary>
    public int TotalRecordsExtracted { get; init; }

    /// <summary>
    /// The number of valid records.
    /// </summary>
    public int ValidRecordCount { get; init; }

    /// <summary>
    /// The number of invalid records.
    /// </summary>
    public int InvalidRecordCount { get; init; }

    /// <summary>
    /// The number of records loaded to output.
    /// </summary>
    public int RecordsLoaded { get; init; }

    /// <summary>
    /// An error message if the run failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a response from a <see cref="PipelineRun"/> entity.
    /// </summary>
    /// <param name="run">The pipeline run entity.</param>
    /// <returns>A new response DTO.</returns>
    public static PipelineRunResponse FromPipelineRun(PipelineRun run) => new()
    {
        RunId = run.RunId,
        Status = ((PipelineStatus)run.Status).ToString(),
        StartedAt = run.StartedAt,
        CompletedAt = run.CompletedAt,
        TriggerSource = run.TriggerSource,
        TotalRecordsExtracted = run.TotalRecordsExtracted,
        ValidRecordCount = run.ValidRecordCount,
        InvalidRecordCount = run.InvalidRecordCount,
        RecordsLoaded = run.RecordsLoaded,
        ErrorMessage = run.ErrorMessage
    };
}
