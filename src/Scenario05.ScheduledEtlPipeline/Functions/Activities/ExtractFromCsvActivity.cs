using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Scenario05.ScheduledEtlPipeline.Models;

namespace Scenario05.ScheduledEtlPipeline.Functions.Activities;

/// <summary>
/// Activity function that extracts data by parsing a CSV file from blob storage.
/// </summary>
public sealed class ExtractFromCsvActivity
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly EtlOptions _options;
    private readonly ILogger<ExtractFromCsvActivity> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="ExtractFromCsvActivity"/>.
    /// </summary>
    /// <param name="blobServiceClient">The blob service client.</param>
    /// <param name="options">The ETL options.</param>
    /// <param name="logger">The logger instance.</param>
    public ExtractFromCsvActivity(
        BlobServiceClient blobServiceClient,
        IOptions<EtlOptions> options,
        ILogger<ExtractFromCsvActivity> logger)
    {
        _blobServiceClient = blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Extracts data from a CSV blob.
    /// </summary>
    /// <param name="runId">The pipeline run identifier.</param>
    /// <returns>The extraction result.</returns>
    [Function("ExtractFromCsv")]
    public async Task<ExtractionResult> RunAsync([ActivityTrigger] string runId)
    {
        _logger.LogInformation("Extracting from CSV for run {RunId}", runId);

        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_options.CsvSourceContainer);
            var blobClient = containerClient.GetBlobClient(_options.CsvSourceBlobName);

            if (!await blobClient.ExistsAsync().ConfigureAwait(false))
            {
                _logger.LogWarning("CSV source blob not found for run {RunId}", runId);
                return new ExtractionResult("CsvFile", Array.Empty<DataRecord>(), true);
            }

            var response = await blobClient.DownloadContentAsync().ConfigureAwait(false);
            var content = response.Value.Content.ToString();

            var records = ParseCsv(content);

            _logger.LogInformation(
                "CSV extraction complete for run {RunId}. Records: {Count}",
                runId, records.Count);

            return new ExtractionResult("CsvFile", records, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CSV extraction failed for run {RunId}", runId);
            return new ExtractionResult("CsvFile", Array.Empty<DataRecord>(), false, ex.Message);
        }
    }

    /// <summary>
    /// Parses CSV content into data records. Assumes the first row contains headers.
    /// </summary>
    internal static IReadOnlyList<DataRecord> ParseCsv(string csvContent)
    {
        if (string.IsNullOrWhiteSpace(csvContent))
        {
            return Array.Empty<DataRecord>();
        }

        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (lines.Length < 2)
        {
            return Array.Empty<DataRecord>();
        }

        var headers = lines[0].Split(',', StringSplitOptions.TrimEntries);
        var records = new List<DataRecord>();

        for (var i = 1; i < lines.Length; i++)
        {
            var values = lines[i].Split(',', StringSplitOptions.TrimEntries);
            var fields = new Dictionary<string, string>();

            for (var j = 0; j < headers.Length && j < values.Length; j++)
            {
                fields[headers[j]] = values[j];
            }

            records.Add(new DataRecord
            {
                SourceName = "CsvFile",
                Fields = fields
            });
        }

        return records.AsReadOnly();
    }
}
