using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Options;
using Scenario01.DocumentProcessing.Models;

namespace Scenario01.DocumentProcessing.Services;

/// <summary>
/// Azure Table Storage implementation of <see cref="IDocumentRepository"/>.
/// Uses a lazy initialization pattern to ensure the table exists before first use.
/// </summary>
public sealed class TableStorageDocumentRepository : IDocumentRepository
{
    private readonly TableServiceClient _tableServiceClient;
    private readonly DocumentProcessingOptions _options;
    private readonly Lazy<Task<TableClient>> _tableClientLazy;

    /// <summary>
    /// Initializes a new instance of <see cref="TableStorageDocumentRepository"/>.
    /// </summary>
    /// <param name="tableServiceClient">The Azure Table Storage service client.</param>
    /// <param name="options">Document processing configuration options.</param>
    public TableStorageDocumentRepository(
        TableServiceClient tableServiceClient,
        IOptions<DocumentProcessingOptions> options)
    {
        _tableServiceClient = tableServiceClient ?? throw new ArgumentNullException(nameof(tableServiceClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        _tableClientLazy = new Lazy<Task<TableClient>>(InitializeTableClientAsync);
    }

    /// <inheritdoc />
    public async Task<DocumentMetadata?> GetAsync(string documentId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentId);

        var tableClient = await _tableClientLazy.Value.ConfigureAwait(false);

        try
        {
            var response = await tableClient.GetEntityAsync<DocumentMetadata>(
                DocumentMetadata.DefaultPartitionKey,
                documentId,
                cancellationToken: ct).ConfigureAwait(false);

            return response.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DocumentMetadata>> GetByStatusAsync(DocumentStatus status, CancellationToken ct)
    {
        var tableClient = await _tableClientLazy.Value.ConfigureAwait(false);

        var statusValue = (int)status;
        var filter = $"PartitionKey eq '{DocumentMetadata.DefaultPartitionKey}' and Status eq {statusValue}";

        var results = new List<DocumentMetadata>();

        await foreach (var entity in tableClient.QueryAsync<DocumentMetadata>(
            filter: filter,
            cancellationToken: ct).ConfigureAwait(false))
        {
            results.Add(entity);
        }

        return results.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task UpsertAsync(DocumentMetadata document, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(document);

        var tableClient = await _tableClientLazy.Value.ConfigureAwait(false);

        await tableClient.UpsertEntityAsync(
            document,
            TableUpdateMode.Replace,
            ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DocumentMetadata>> GetProcessedSinceDateAsync(
        DateTimeOffset since,
        CancellationToken ct)
    {
        var tableClient = await _tableClientLazy.Value.ConfigureAwait(false);

        // Query for documents that have a ProcessedAt value on or after the given date.
        // Table Storage OData filter uses ISO 8601 format for DateTimeOffset comparisons.
        var sinceString = since.UtcDateTime.ToString("o");
        var filter = $"PartitionKey eq '{DocumentMetadata.DefaultPartitionKey}' and ProcessedAt ge datetime'{sinceString}'";

        var results = new List<DocumentMetadata>();

        await foreach (var entity in tableClient.QueryAsync<DocumentMetadata>(
            filter: filter,
            cancellationToken: ct).ConfigureAwait(false))
        {
            results.Add(entity);
        }

        return results.AsReadOnly();
    }

    /// <summary>
    /// Initializes the table client and ensures the table exists in Azure Table Storage.
    /// </summary>
    private async Task<TableClient> InitializeTableClientAsync()
    {
        var tableClient = _tableServiceClient.GetTableClient(_options.TableName);
        await tableClient.CreateIfNotExistsAsync().ConfigureAwait(false);
        return tableClient;
    }
}
