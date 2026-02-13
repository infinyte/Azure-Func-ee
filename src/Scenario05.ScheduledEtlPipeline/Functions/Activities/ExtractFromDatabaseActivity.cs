using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Scenario05.ScheduledEtlPipeline.Models;

namespace Scenario05.ScheduledEtlPipeline.Functions.Activities;

/// <summary>
/// Activity function that extracts data from a database source (simulated).
/// In production, this would connect to a real database via Entity Framework or ADO.NET.
/// </summary>
public sealed class ExtractFromDatabaseActivity
{
    private readonly ILogger<ExtractFromDatabaseActivity> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="ExtractFromDatabaseActivity"/>.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public ExtractFromDatabaseActivity(ILogger<ExtractFromDatabaseActivity> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Extracts data from the simulated database source.
    /// </summary>
    /// <param name="runId">The pipeline run identifier.</param>
    /// <returns>The extraction result.</returns>
    [Function("ExtractFromDatabase")]
    public Task<ExtractionResult> RunAsync([ActivityTrigger] string runId)
    {
        _logger.LogInformation("Extracting from database for run {RunId}", runId);

        try
        {
            var records = new List<DataRecord>();

            for (var i = 1; i <= 4; i++)
            {
                records.Add(new DataRecord
                {
                    SourceName = "Database",
                    Fields = new Dictionary<string, string>
                    {
                        ["name"] = $"DB Record {i}",
                        ["email"] = $"db-user{i}@example.com",
                        ["amount"] = (i * 250.00).ToString("F2"),
                        ["category"] = "Enterprise"
                    }
                });
            }

            _logger.LogInformation(
                "Database extraction complete for run {RunId}. Records: {Count}",
                runId, records.Count);

            return Task.FromResult(new ExtractionResult("Database", records.AsReadOnly(), true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database extraction failed for run {RunId}", runId);
            return Task.FromResult(
                new ExtractionResult("Database", Array.Empty<DataRecord>(), false, ex.Message));
        }
    }
}
