using Scenario05.ScheduledEtlPipeline.Models;

namespace Scenario05.ScheduledEtlPipeline.Repositories;

/// <summary>
/// Repository for persisting and retrieving ETL pipeline run records.
/// </summary>
public interface IPipelineRepository
{
    /// <summary>
    /// Gets a pipeline run by its unique identifier.
    /// </summary>
    /// <param name="runId">The run identifier.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The pipeline run, or null if not found.</returns>
    Task<PipelineRun?> GetAsync(string runId, CancellationToken ct = default);

    /// <summary>
    /// Creates or updates a pipeline run record.
    /// </summary>
    /// <param name="run">The pipeline run entity to persist.</param>
    /// <param name="ct">A cancellation token.</param>
    Task UpsertAsync(PipelineRun run, CancellationToken ct = default);

    /// <summary>
    /// Gets all pipeline runs with the specified status.
    /// </summary>
    /// <param name="status">The status to filter by.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A read-only list of matching pipeline runs.</returns>
    Task<IReadOnlyList<PipelineRun>> GetByStatusAsync(PipelineStatus status, CancellationToken ct = default);
}
