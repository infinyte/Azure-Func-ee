using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using Scenario05.ScheduledEtlPipeline.Models;

namespace Scenario05.ScheduledEtlPipeline.Functions;

/// <summary>
/// Durable Functions orchestrator implementing the fan-out/fan-in pattern for ETL processing.
/// Extracts data from three sources in parallel (fan-out), merges results, then validates,
/// transforms, and loads data sequentially (fan-in).
/// </summary>
public static class EtlOrchestratorFunction
{
    /// <summary>
    /// Orchestrates the ETL pipeline: parallel extraction → merge → validate → transform → load.
    /// </summary>
    /// <param name="context">The durable task orchestration context.</param>
    /// <returns>A summary of the pipeline execution.</returns>
    [Function("EtlOrchestrator")]
    public static async Task<EtlOrchestratorResult> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var logger = context.CreateReplaySafeLogger("EtlOrchestrator");
        var runId = context.GetInput<string>()
            ?? throw new InvalidOperationException("Pipeline run ID is required.");

        logger.LogInformation("Starting ETL orchestration for run {RunId}", runId);

        // ---- Fan-out: Extract from 3 sources in parallel ----
        var apiTask = context.CallActivityAsync<ExtractionResult>("ExtractFromApi", runId);
        var csvTask = context.CallActivityAsync<ExtractionResult>("ExtractFromCsv", runId);
        var dbTask = context.CallActivityAsync<ExtractionResult>("ExtractFromDatabase", runId);

        var extractionResults = await Task.WhenAll(apiTask, csvTask, dbTask);

        // Merge results from all sources.
        var allRecords = new List<DataRecord>();
        var failedSources = new List<string>();

        foreach (var result in extractionResults)
        {
            if (result.IsSuccess)
            {
                allRecords.AddRange(result.Records);
                logger.LogInformation(
                    "Source {Source} extracted {Count} records",
                    result.SourceName, result.Records.Count);
            }
            else
            {
                failedSources.Add(result.SourceName);
                logger.LogWarning(
                    "Source {Source} extraction failed: {Error}",
                    result.SourceName, result.ErrorMessage);
            }
        }

        if (allRecords.Count == 0)
        {
            logger.LogWarning("No records extracted from any source for run {RunId}", runId);
            return new EtlOrchestratorResult(runId, 0, 0, 0, 0, failedSources);
        }

        logger.LogInformation("Total records extracted: {Count} from {SourceCount} sources",
            allRecords.Count, extractionResults.Length - failedSources.Count);

        // ---- Fan-in: Validate ----
        var validationResult = await context.CallActivityAsync<ValidationResult>(
            "ValidateData", allRecords);

        logger.LogInformation(
            "Validation complete. Valid: {Valid}, Invalid: {Invalid}",
            validationResult.ValidRecords.Count, validationResult.InvalidRecords.Count);

        if (validationResult.ValidRecords.Count == 0)
        {
            logger.LogWarning("No valid records after validation for run {RunId}", runId);
            return new EtlOrchestratorResult(
                runId, allRecords.Count,
                validationResult.ValidRecords.Count,
                validationResult.InvalidRecords.Count,
                0, failedSources);
        }

        // ---- Transform ----
        var transformedRecords = await context.CallActivityAsync<IReadOnlyList<DataRecord>>(
            "TransformData", validationResult.ValidRecords);

        logger.LogInformation("Transformed {Count} records", transformedRecords.Count);

        // ---- Load ----
        var loadedCount = await context.CallActivityAsync<int>(
            "LoadData", new LoadDataInput(runId, transformedRecords));

        logger.LogInformation("Loaded {Count} records for run {RunId}", loadedCount, runId);

        return new EtlOrchestratorResult(
            runId,
            allRecords.Count,
            validationResult.ValidRecords.Count,
            validationResult.InvalidRecords.Count,
            loadedCount,
            failedSources);
    }
}

/// <summary>
/// The result of an ETL orchestration execution.
/// </summary>
/// <param name="RunId">The pipeline run identifier.</param>
/// <param name="TotalExtracted">Total records extracted from all sources.</param>
/// <param name="ValidCount">Number of records that passed validation.</param>
/// <param name="InvalidCount">Number of records that failed validation.</param>
/// <param name="LoadedCount">Number of records loaded to the output store.</param>
/// <param name="FailedSources">Names of sources that failed extraction.</param>
public sealed record EtlOrchestratorResult(
    string RunId,
    int TotalExtracted,
    int ValidCount,
    int InvalidCount,
    int LoadedCount,
    IReadOnlyList<string> FailedSources);

/// <summary>
/// Input for the LoadData activity.
/// </summary>
/// <param name="RunId">The pipeline run identifier.</param>
/// <param name="Records">The records to load.</param>
public sealed record LoadDataInput(string RunId, IReadOnlyList<DataRecord> Records);
