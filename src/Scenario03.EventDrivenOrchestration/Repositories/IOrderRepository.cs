using Scenario03.EventDrivenOrchestration.Models;

namespace Scenario03.EventDrivenOrchestration.Repositories;

/// <summary>
/// Defines data access operations for order documents in the persistence store.
/// </summary>
public interface IOrderRepository
{
    /// <summary>
    /// Retrieves an order by its unique identifier.
    /// </summary>
    /// <param name="orderId">The order identifier (also the partition key).</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The order if found; otherwise <c>null</c>.</returns>
    Task<Order?> GetAsync(string orderId, CancellationToken ct = default);

    /// <summary>
    /// Creates or replaces an order document in the store.
    /// </summary>
    /// <param name="order">The order to persist.</param>
    /// <param name="ct">A cancellation token.</param>
    Task UpsertAsync(Order order, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all orders placed by a specific customer.
    /// </summary>
    /// <param name="customerId">The customer identifier.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A read-only list of orders for the customer.</returns>
    Task<IReadOnlyList<Order>> GetByCustomerAsync(string customerId, CancellationToken ct = default);
}
