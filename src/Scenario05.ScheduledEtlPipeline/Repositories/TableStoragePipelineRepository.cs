using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Options;
using Scenario05.ScheduledEtlPipeline.Models;

namespace Scenario05.ScheduledEtlPipeline.Repositories;

/// <summary>
/// Azure Table Storage implementation of <see cref="IPipelineRepository"/>.
/// </summary>
public sealed class TableStoragePipelineRepository : IPipelineRepository
{
    private readonly TableServiceClient _tableServiceClient;
    private readonly EtlOptions _options;
    private readonly Lazy<Task<TableClient>> _tableClientLazy;

    /// <summary>
    /// Initializes a new instance of <see cref="TableStoragePipelineRepository"/>.
    /// </summary>
    /// <param name="tableServiceClient">The Azure Table Storage service client.</param>
    /// <param name="options">ETL pipeline configuration options.</param>
    public TableStoragePipelineRepository(
        TableServiceClient tableServiceClient,
        IOptions<EtlOptions> options)
    {
        _tableServiceClient = tableServiceClient ?? throw new ArgumentNullException(nameof(tableServiceClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _tableClientLazy = new Lazy<Task<TableClient>>(InitializeTableClientAsync);
    }

    /// <inheritdoc />
    public async Task<PipelineRun?> GetAsync(string runId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);

        var tableClient = await _tableClientLazy.Value.ConfigureAwait(false);

        try
        {
            var response = await tableClient.GetEntityAsync<PipelineRun>(
                PipelineRun.DefaultPartitionKey,
                runId,
                cancellationToken: ct).ConfigureAwait(false);

            return response.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task UpsertAsync(PipelineRun run, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(run);

        var tableClient = await _tableClientLazy.Value.ConfigureAwait(false);

        await tableClient.UpsertEntityAsync(
            run,
            TableUpdateMode.Replace,
            ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PipelineRun>> GetByStatusAsync(PipelineStatus status, CancellationToken ct)
    {
        var tableClient = await _tableClientLazy.Value.ConfigureAwait(false);

        var statusValue = (int)status;
        var filter = $"PartitionKey eq '{PipelineRun.DefaultPartitionKey}' and Status eq {statusValue}";

        var results = new List<PipelineRun>();

        await foreach (var entity in tableClient.QueryAsync<PipelineRun>(
            filter: filter,
            cancellationToken: ct).ConfigureAwait(false))
        {
            results.Add(entity);
        }

        return results.AsReadOnly();
    }

    private async Task<TableClient> InitializeTableClientAsync()
    {
        var tableClient = _tableServiceClient.GetTableClient(_options.PipelineRunsTable);
        await tableClient.CreateIfNotExistsAsync().ConfigureAwait(false);
        return tableClient;
    }
}
