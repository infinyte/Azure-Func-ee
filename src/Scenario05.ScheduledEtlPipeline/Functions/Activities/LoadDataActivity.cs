using System.Text;
using System.Text.Json;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Scenario05.ScheduledEtlPipeline.Models;

namespace Scenario05.ScheduledEtlPipeline.Functions.Activities;

/// <summary>
/// Activity function that loads transformed data records as JSON to the output blob container.
/// </summary>
public sealed class LoadDataActivity
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly EtlOptions _options;
    private readonly ILogger<LoadDataActivity> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    /// <summary>
    /// Initializes a new instance of <see cref="LoadDataActivity"/>.
    /// </summary>
    /// <param name="blobServiceClient">The blob service client.</param>
    /// <param name="options">The ETL options.</param>
    /// <param name="logger">The logger instance.</param>
    public LoadDataActivity(
        BlobServiceClient blobServiceClient,
        IOptions<EtlOptions> options,
        ILogger<LoadDataActivity> logger)
    {
        _blobServiceClient = blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Loads transformed records as JSON blobs to the output container.
    /// </summary>
    /// <param name="input">The load data input containing the run ID and records.</param>
    /// <returns>The number of records loaded.</returns>
    [Function("LoadData")]
    public async Task<int> RunAsync([ActivityTrigger] LoadDataInput input)
    {
        _logger.LogInformation("Loading {Count} records for run {RunId}", input.Records.Count, input.RunId);

        var containerClient = _blobServiceClient.GetBlobContainerClient(_options.OutputContainer);
        await containerClient.CreateIfNotExistsAsync().ConfigureAwait(false);

        var blobName = $"{input.RunId}/{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-output.json";
        var blobClient = containerClient.GetBlobClient(blobName);

        var json = JsonSerializer.Serialize(input.Records, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);

        using var stream = new MemoryStream(bytes);
        await blobClient.UploadAsync(stream, overwrite: true).ConfigureAwait(false);

        _logger.LogInformation(
            "Loaded {Count} records to blob {BlobName} for run {RunId}",
            input.Records.Count, blobName, input.RunId);

        return input.Records.Count;
    }
}
