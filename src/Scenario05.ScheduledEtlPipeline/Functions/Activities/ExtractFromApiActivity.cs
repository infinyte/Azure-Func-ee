using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Scenario05.ScheduledEtlPipeline.Models;
using Scenario05.ScheduledEtlPipeline.Services;

namespace Scenario05.ScheduledEtlPipeline.Functions.Activities;

/// <summary>
/// Activity function that extracts data from an external API source.
/// </summary>
public sealed class ExtractFromApiActivity
{
    private readonly IExternalApiClient _apiClient;
    private readonly ILogger<ExtractFromApiActivity> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="ExtractFromApiActivity"/>.
    /// </summary>
    /// <param name="apiClient">The external API client.</param>
    /// <param name="logger">The logger instance.</param>
    public ExtractFromApiActivity(IExternalApiClient apiClient, ILogger<ExtractFromApiActivity> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Extracts data from the external API.
    /// </summary>
    /// <param name="runId">The pipeline run identifier.</param>
    /// <returns>The extraction result.</returns>
    [Function("ExtractFromApi")]
    public async Task<ExtractionResult> RunAsync([ActivityTrigger] string runId)
    {
        _logger.LogInformation("Extracting from external API for run {RunId}", runId);

        try
        {
            var records = await _apiClient.ExtractAsync().ConfigureAwait(false);

            _logger.LogInformation(
                "API extraction complete for run {RunId}. Records: {Count}",
                runId, records.Count);

            return new ExtractionResult("ExternalApi", records, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API extraction failed for run {RunId}", runId);
            return new ExtractionResult("ExternalApi", Array.Empty<DataRecord>(), false, ex.Message);
        }
    }
}
