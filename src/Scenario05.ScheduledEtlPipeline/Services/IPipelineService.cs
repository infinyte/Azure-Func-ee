using Scenario05.ScheduledEtlPipeline.Models;

namespace Scenario05.ScheduledEtlPipeline.Services;

/// <summary>
/// Service for managing ETL pipeline run lifecycle.
/// </summary>
public interface IPipelineService
{
    /// <summary>
    /// Creates a new pipeline run record.
    /// </summary>
    /// <param name="triggerSource">The source that triggered this run.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The created pipeline run.</returns>
    Task<PipelineRun> CreateRunAsync(string triggerSource, CancellationToken ct = default);

    /// <summary>
    /// Updates the status of a pipeline run.
    /// </summary>
    /// <param name="runId">The run identifier.</param>
    /// <param name="status">The new status.</param>
    /// <param name="ct">A cancellation token.</param>
    Task UpdateStatusAsync(string runId, PipelineStatus status, CancellationToken ct = default);

    /// <summary>
    /// Marks a pipeline run as completed with summary statistics.
    /// </summary>
    /// <param name="runId">The run identifier.</param>
    /// <param name="totalExtracted">Total records extracted.</param>
    /// <param name="validCount">Number of valid records.</param>
    /// <param name="invalidCount">Number of invalid records.</param>
    /// <param name="loadedCount">Number of records loaded.</param>
    /// <param name="ct">A cancellation token.</param>
    Task CompleteRunAsync(string runId, int totalExtracted, int validCount, int invalidCount, int loadedCount, CancellationToken ct = default);

    /// <summary>
    /// Marks a pipeline run as failed.
    /// </summary>
    /// <param name="runId">The run identifier.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="ct">A cancellation token.</param>
    Task FailRunAsync(string runId, string errorMessage, CancellationToken ct = default);

    /// <summary>
    /// Gets a pipeline run by its identifier.
    /// </summary>
    /// <param name="runId">The run identifier.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The pipeline run, or null if not found.</returns>
    Task<PipelineRun?> GetRunAsync(string runId, CancellationToken ct = default);
}
