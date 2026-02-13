using Microsoft.Extensions.Logging;
using Scenario05.ScheduledEtlPipeline.Models;
using Scenario05.ScheduledEtlPipeline.Repositories;

namespace Scenario05.ScheduledEtlPipeline.Services;

/// <summary>
/// Default implementation of <see cref="IPipelineService"/> for managing pipeline run lifecycle.
/// </summary>
public sealed class PipelineService : IPipelineService
{
    private readonly IPipelineRepository _repository;
    private readonly ILogger<PipelineService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="PipelineService"/>.
    /// </summary>
    /// <param name="repository">The pipeline repository.</param>
    /// <param name="logger">The logger instance.</param>
    public PipelineService(IPipelineRepository repository, ILogger<PipelineService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<PipelineRun> CreateRunAsync(string triggerSource, CancellationToken ct)
    {
        var runId = Guid.NewGuid().ToString();

        var run = new PipelineRun
        {
            PartitionKey = PipelineRun.DefaultPartitionKey,
            RowKey = runId,
            RunId = runId,
            Status = (int)PipelineStatus.Pending,
            StartedAt = DateTimeOffset.UtcNow,
            TriggerSource = triggerSource
        };

        await _repository.UpsertAsync(run, ct).ConfigureAwait(false);

        _logger.LogInformation("Created pipeline run {RunId} triggered by {Source}", runId, triggerSource);

        return run;
    }

    /// <inheritdoc />
    public async Task UpdateStatusAsync(string runId, PipelineStatus status, CancellationToken ct)
    {
        var run = await _repository.GetAsync(runId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Pipeline run '{runId}' not found.");

        run.Status = (int)status;
        await _repository.UpsertAsync(run, ct).ConfigureAwait(false);

        _logger.LogInformation("Updated pipeline run {RunId} status to {Status}", runId, status);
    }

    /// <inheritdoc />
    public async Task CompleteRunAsync(string runId, int totalExtracted, int validCount, int invalidCount, int loadedCount, CancellationToken ct)
    {
        var run = await _repository.GetAsync(runId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Pipeline run '{runId}' not found.");

        run.Status = (int)PipelineStatus.Completed;
        run.CompletedAt = DateTimeOffset.UtcNow;
        run.TotalRecordsExtracted = totalExtracted;
        run.ValidRecordCount = validCount;
        run.InvalidRecordCount = invalidCount;
        run.RecordsLoaded = loadedCount;

        await _repository.UpsertAsync(run, ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Pipeline run {RunId} completed. Extracted: {Extracted}, Valid: {Valid}, Invalid: {Invalid}, Loaded: {Loaded}",
            runId, totalExtracted, validCount, invalidCount, loadedCount);
    }

    /// <inheritdoc />
    public async Task FailRunAsync(string runId, string errorMessage, CancellationToken ct)
    {
        var run = await _repository.GetAsync(runId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Pipeline run '{runId}' not found.");

        run.Status = (int)PipelineStatus.Failed;
        run.CompletedAt = DateTimeOffset.UtcNow;
        run.ErrorMessage = errorMessage;

        await _repository.UpsertAsync(run, ct).ConfigureAwait(false);

        _logger.LogError("Pipeline run {RunId} failed: {Error}", runId, errorMessage);
    }

    /// <inheritdoc />
    public async Task<PipelineRun?> GetRunAsync(string runId, CancellationToken ct)
    {
        return await _repository.GetAsync(runId, ct).ConfigureAwait(false);
    }
}
