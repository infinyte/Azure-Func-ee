using Scenario03.EventDrivenOrchestration.Models;
using Scenario03.EventDrivenOrchestration.Models.Dtos;

namespace Scenario03.EventDrivenOrchestration.Services;

/// <summary>
/// Defines operations for creating, querying, and updating orders.
/// </summary>
public interface IOrderService
{
    /// <summary>
    /// Creates a new order from the given request, assigning it a unique identifier and Pending status.
    /// </summary>
    /// <param name="request">The order creation request containing customer and item information.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The newly created order.</returns>
    Task<Order> CreateOrderAsync(CreateOrderRequest request, CancellationToken ct = default);

    /// <summary>
    /// Retrieves an order by its unique identifier.
    /// </summary>
    /// <param name="orderId">The order identifier.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The order if found; otherwise <c>null</c>.</returns>
    Task<Order?> GetOrderAsync(string orderId, CancellationToken ct = default);

    /// <summary>
    /// Updates the status of an existing order, optionally recording a failure reason.
    /// </summary>
    /// <param name="orderId">The order identifier.</param>
    /// <param name="status">The new status to apply.</param>
    /// <param name="reason">An optional reason describing why the status changed (used for failures).</param>
    /// <param name="ct">A cancellation token.</param>
    Task UpdateOrderStatusAsync(string orderId, OrderStatus status, string? reason = null, CancellationToken ct = default);
}
