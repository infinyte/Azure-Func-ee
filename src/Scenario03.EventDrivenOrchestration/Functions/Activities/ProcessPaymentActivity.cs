using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Scenario03.EventDrivenOrchestration.Models;
using Scenario03.EventDrivenOrchestration.Services;

namespace Scenario03.EventDrivenOrchestration.Functions.Activities;

/// <summary>
/// Durable Functions activity that processes payment for an order.
/// Called as a saga step by the <see cref="OrderSagaOrchestrator"/>.
/// </summary>
public sealed class ProcessPaymentActivity
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<ProcessPaymentActivity> _logger;

    public ProcessPaymentActivity(IPaymentService paymentService, ILogger<ProcessPaymentActivity> logger)
    {
        _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Processes payment for the given order.
    /// </summary>
    /// <param name="order">The order to charge.</param>
    /// <returns>The payment transaction identifier.</returns>
    [Function("ProcessPayment")]
    public async Task<string> RunAsync([ActivityTrigger] Order order)
    {
        _logger.LogInformation(
            "Processing payment for order {OrderId}. Amount: {Amount:C}, Customer: {CustomerId}",
            order.Id, order.TotalAmount, order.CustomerId);

        var transactionId = await _paymentService.ProcessPaymentAsync(
            order.Id, order.TotalAmount, order.CustomerId);

        _logger.LogInformation(
            "Payment processed for order {OrderId}. Transaction ID: {TransactionId}",
            order.Id, transactionId);

        return transactionId;
    }
}
