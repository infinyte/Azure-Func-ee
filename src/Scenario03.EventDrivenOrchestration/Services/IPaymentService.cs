namespace Scenario03.EventDrivenOrchestration.Services;

/// <summary>
/// Defines operations for processing and refunding payments during order processing.
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Processes a payment for the specified order.
    /// </summary>
    /// <param name="orderId">The order identifier associated with the payment.</param>
    /// <param name="amount">The monetary amount to charge.</param>
    /// <param name="customerId">The identifier of the customer being charged.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A transaction identifier that can be used to refund the payment if needed.</returns>
    Task<string> ProcessPaymentAsync(string orderId, decimal amount, string customerId, CancellationToken ct = default);

    /// <summary>
    /// Refunds a previously processed payment as part of saga compensation.
    /// </summary>
    /// <param name="transactionId">The transaction identifier returned by <see cref="ProcessPaymentAsync"/>.</param>
    /// <param name="ct">A cancellation token.</param>
    Task RefundPaymentAsync(string transactionId, CancellationToken ct = default);
}
