using Azure;
using Azure.Data.Tables;

namespace Scenario05.ScheduledEtlPipeline.Models;

/// <summary>
/// Represents a single ETL pipeline execution stored in Azure Table Storage.
/// Uses "PipelineRun" as the partition key and the run identifier as the row key.
/// </summary>
public sealed class PipelineRun : ITableEntity
{
    /// <summary>
    /// The default partition key for pipeline run entities.
    /// </summary>
    public const string DefaultPartitionKey = "PipelineRun";

    /// <inheritdoc />
    public string PartitionKey { get; set; } = DefaultPartitionKey;

    /// <inheritdoc />
    public string RowKey { get; set; } = string.Empty;

    /// <inheritdoc />
    public DateTimeOffset? Timestamp { get; set; }

    /// <inheritdoc />
    public ETag ETag { get; set; }

    /// <summary>
    /// The unique identifier for this pipeline run.
    /// </summary>
    public string RunId { get; set; } = string.Empty;

    /// <summary>
    /// The current status of the pipeline run.
    /// </summary>
    public int Status { get; set; } = (int)PipelineStatus.Pending;

    /// <summary>
    /// The date and time the pipeline run was started.
    /// </summary>
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// The date and time the pipeline run completed, if applicable.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// The trigger source that initiated this run (e.g., "Scheduled", "Manual").
    /// </summary>
    public string TriggerSource { get; set; } = string.Empty;

    /// <summary>
    /// The total number of records extracted from all sources.
    /// </summary>
    public int TotalRecordsExtracted { get; set; }

    /// <summary>
    /// The number of records that passed validation.
    /// </summary>
    public int ValidRecordCount { get; set; }

    /// <summary>
    /// The number of records that failed validation.
    /// </summary>
    public int InvalidRecordCount { get; set; }

    /// <summary>
    /// The number of records successfully loaded to the output store.
    /// </summary>
    public int RecordsLoaded { get; set; }

    /// <summary>
    /// An error message if the pipeline run failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The Durable Functions orchestration instance ID, if applicable.
    /// </summary>
    public string? OrchestrationId { get; set; }
}
