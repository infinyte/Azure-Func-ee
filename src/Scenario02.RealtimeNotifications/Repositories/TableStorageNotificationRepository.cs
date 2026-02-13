using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Options;
using Scenario02.RealtimeNotifications.Models;

namespace Scenario02.RealtimeNotifications.Repositories;

/// <summary>
/// Azure Table Storage implementation of <see cref="INotificationRepository"/>.
/// </summary>
public sealed class TableStorageNotificationRepository : INotificationRepository
{
    private readonly TableServiceClient _tableServiceClient;
    private readonly NotificationOptions _options;
    private readonly Lazy<Task<TableClient>> _tableClientLazy;

    /// <summary>
    /// Initializes a new instance of <see cref="TableStorageNotificationRepository"/>.
    /// </summary>
    /// <param name="tableServiceClient">The Azure Table Storage service client.</param>
    /// <param name="options">Notification configuration options.</param>
    public TableStorageNotificationRepository(
        TableServiceClient tableServiceClient,
        IOptions<NotificationOptions> options)
    {
        _tableServiceClient = tableServiceClient ?? throw new ArgumentNullException(nameof(tableServiceClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _tableClientLazy = new Lazy<Task<TableClient>>(InitializeTableClientAsync);
    }

    /// <inheritdoc />
    public async Task<Notification?> GetAsync(string userId, string notificationId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(notificationId);

        var tableClient = await _tableClientLazy.Value.ConfigureAwait(false);

        try
        {
            var response = await tableClient.GetEntityAsync<Notification>(
                userId, notificationId, cancellationToken: ct).ConfigureAwait(false);
            return response.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Notification>> GetByUserAsync(string userId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var tableClient = await _tableClientLazy.Value.ConfigureAwait(false);
        var filter = $"PartitionKey eq '{userId}'";

        var results = new List<Notification>();
        await foreach (var entity in tableClient.QueryAsync<Notification>(
            filter: filter, cancellationToken: ct).ConfigureAwait(false))
        {
            results.Add(entity);
        }

        return results.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Notification>> GetUnreadByUserAsync(string userId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var tableClient = await _tableClientLazy.Value.ConfigureAwait(false);
        var pendingStatus = (int)NotificationStatus.Pending;
        var deliveredStatus = (int)NotificationStatus.Delivered;
        var filter = $"PartitionKey eq '{userId}' and (Status eq {pendingStatus} or Status eq {deliveredStatus})";

        var results = new List<Notification>();
        await foreach (var entity in tableClient.QueryAsync<Notification>(
            filter: filter, cancellationToken: ct).ConfigureAwait(false))
        {
            results.Add(entity);
        }

        return results.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task UpsertAsync(Notification notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var tableClient = await _tableClientLazy.Value.ConfigureAwait(false);
        await tableClient.UpsertEntityAsync(notification, TableUpdateMode.Replace, ct).ConfigureAwait(false);
    }

    private async Task<TableClient> InitializeTableClientAsync()
    {
        var tableClient = _tableServiceClient.GetTableClient(_options.NotificationsTable);
        await tableClient.CreateIfNotExistsAsync().ConfigureAwait(false);
        return tableClient;
    }
}
