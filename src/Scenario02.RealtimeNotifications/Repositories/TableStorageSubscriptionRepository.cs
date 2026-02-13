using Azure.Data.Tables;
using Microsoft.Extensions.Options;
using Scenario02.RealtimeNotifications.Models;

namespace Scenario02.RealtimeNotifications.Repositories;

/// <summary>
/// Azure Table Storage implementation of <see cref="ISubscriptionRepository"/>.
/// </summary>
public sealed class TableStorageSubscriptionRepository : ISubscriptionRepository
{
    private readonly TableServiceClient _tableServiceClient;
    private readonly NotificationOptions _options;
    private readonly Lazy<Task<TableClient>> _tableClientLazy;

    /// <summary>
    /// Initializes a new instance of <see cref="TableStorageSubscriptionRepository"/>.
    /// </summary>
    /// <param name="tableServiceClient">The Azure Table Storage service client.</param>
    /// <param name="options">Notification configuration options.</param>
    public TableStorageSubscriptionRepository(
        TableServiceClient tableServiceClient,
        IOptions<NotificationOptions> options)
    {
        _tableServiceClient = tableServiceClient ?? throw new ArgumentNullException(nameof(tableServiceClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _tableClientLazy = new Lazy<Task<TableClient>>(InitializeTableClientAsync);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserSubscription>> GetByUserAsync(string userId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var tableClient = await _tableClientLazy.Value.ConfigureAwait(false);
        var filter = $"PartitionKey eq '{userId}'";

        var results = new List<UserSubscription>();
        await foreach (var entity in tableClient.QueryAsync<UserSubscription>(
            filter: filter, cancellationToken: ct).ConfigureAwait(false))
        {
            results.Add(entity);
        }

        return results.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task UpsertAsync(UserSubscription subscription, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(subscription);

        var tableClient = await _tableClientLazy.Value.ConfigureAwait(false);
        await tableClient.UpsertEntityAsync(subscription, TableUpdateMode.Replace, ct).ConfigureAwait(false);
    }

    private async Task<TableClient> InitializeTableClientAsync()
    {
        var tableClient = _tableServiceClient.GetTableClient(_options.SubscriptionsTable);
        await tableClient.CreateIfNotExistsAsync().ConfigureAwait(false);
        return tableClient;
    }
}
