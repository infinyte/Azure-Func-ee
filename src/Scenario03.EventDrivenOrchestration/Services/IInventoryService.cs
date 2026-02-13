using Scenario03.EventDrivenOrchestration.Models;

namespace Scenario03.EventDrivenOrchestration.Services;

/// <summary>
/// Defines operations for reserving and releasing inventory during order processing.
/// </summary>
public interface IInventoryService
{
    /// <summary>
    /// Reserves inventory for the specified order items.
    /// </summary>
    /// <param name="orderId">The order identifier requesting the reservation.</param>
    /// <param name="items">The list of order items to reserve.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A reservation identifier that can be used to release the reservation if needed.</returns>
    Task<string> ReserveInventoryAsync(string orderId, List<OrderItem> items, CancellationToken ct = default);

    /// <summary>
    /// Releases a previously made inventory reservation as part of saga compensation.
    /// </summary>
    /// <param name="reservationId">The reservation identifier returned by <see cref="ReserveInventoryAsync"/>.</param>
    /// <param name="ct">A cancellation token.</param>
    Task ReleaseInventoryAsync(string reservationId, CancellationToken ct = default);
}
