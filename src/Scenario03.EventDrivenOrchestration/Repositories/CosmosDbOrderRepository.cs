using System.Net;
using System.Text.Json;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Scenario03.EventDrivenOrchestration.Models;

namespace Scenario03.EventDrivenOrchestration.Repositories;

/// <summary>
/// Cosmos DB implementation of <see cref="IOrderRepository"/>.
/// Uses the order ID as the partition key (<c>/id</c>) for efficient point reads.
/// Serialization uses <see cref="System.Text.Json"/> via the Cosmos SDK's built-in support.
/// </summary>
public sealed class CosmosDbOrderRepository : IOrderRepository
{
    private readonly Container _container;
    private readonly ILogger<CosmosDbOrderRepository> _logger;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public CosmosDbOrderRepository(Container container, ILogger<CosmosDbOrderRepository> logger)
    {
        _container = container ?? throw new ArgumentNullException(nameof(container));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Order?> GetAsync(string orderId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderId);

        try
        {
            var response = await _container.ReadItemAsync<Order>(
                orderId,
                new PartitionKey(orderId),
                cancellationToken: ct);

            _logger.LogDebug(
                "Retrieved order {OrderId}. RU charge: {RequestCharge}",
                orderId, response.RequestCharge);

            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogDebug("Order {OrderId} not found in Cosmos DB", orderId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task UpsertAsync(Order order, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(order);

        var response = await _container.UpsertItemAsync(
            order,
            new PartitionKey(order.Id),
            cancellationToken: ct);

        _logger.LogDebug(
            "Upserted order {OrderId}. Status: {StatusCode}, RU charge: {RequestCharge}",
            order.Id, response.StatusCode, response.RequestCharge);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Order>> GetByCustomerAsync(string customerId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerId);

        var query = new QueryDefinition("SELECT * FROM c WHERE c.customerId = @customerId")
            .WithParameter("@customerId", customerId);

        var iterator = _container.GetItemQueryIterator<Order>(query);
        var results = new List<Order>();

        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(ct);

            _logger.LogDebug(
                "Query page for customer {CustomerId} returned {Count} items. RU charge: {RequestCharge}",
                customerId, page.Count, page.RequestCharge);

            results.AddRange(page);
        }

        return results.AsReadOnly();
    }
}
