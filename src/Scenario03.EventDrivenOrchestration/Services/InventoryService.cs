using Microsoft.Extensions.Logging;
using Scenario03.EventDrivenOrchestration.Models;

namespace Scenario03.EventDrivenOrchestration.Services;

/// <summary>
/// Mock implementation of <see cref="IInventoryService"/> that simulates inventory API calls.
/// In a production system this would call an external inventory microservice via HTTP.
/// Uses <see cref="IHttpClientFactory"/> for proper HttpClient lifecycle management with
/// circuit breaker and retry policies configured via the shared resilience library.
/// </summary>
public sealed class InventoryService : IInventoryService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(IHttpClientFactory httpClientFactory, ILogger<InventoryService> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        _httpClient = httpClientFactory.CreateClient("InventoryService");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task<string> ReserveInventoryAsync(string orderId, List<OrderItem> items, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderId);
        ArgumentNullException.ThrowIfNull(items);

        // In production, this would POST to the inventory service API:
        // var response = await _httpClient.PostAsJsonAsync("/api/inventory/reservations", payload, ct);
        // response.EnsureSuccessStatusCode();

        var reservationId = $"INV-{Guid.NewGuid():N}";

        _logger.LogInformation(
            "Reserved inventory for order {OrderId}. Reservation ID: {ReservationId}. Items: {ItemCount}, Products: [{ProductIds}]",
            orderId,
            reservationId,
            items.Count,
            string.Join(", ", items.Select(i => $"{i.ProductId} x{i.Quantity}")));

        return Task.FromResult(reservationId);
    }

    /// <inheritdoc />
    public Task ReleaseInventoryAsync(string reservationId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reservationId);

        // In production, this would DELETE or POST to the inventory service API:
        // var response = await _httpClient.DeleteAsync($"/api/inventory/reservations/{reservationId}", ct);
        // response.EnsureSuccessStatusCode();

        _logger.LogInformation(
            "Released inventory reservation {ReservationId} as part of saga compensation",
            reservationId);

        return Task.CompletedTask;
    }
}
