using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Scenario03.EventDrivenOrchestration.Services;

namespace Scenario03.EventDrivenOrchestration.Functions.Activities;

/// <summary>
/// Durable Functions compensating activity that refunds a previously processed payment.
/// Called by the <see cref="OrderSagaOrchestrator"/> when a downstream saga step fails.
/// </summary>
public sealed class RefundPaymentActivity
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<RefundPaymentActivity> _logger;

    public RefundPaymentActivity(IPaymentService paymentService, ILogger<RefundPaymentActivity> logger)
    {
        _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Refunds the payment transaction identified by the given transaction ID.
    /// </summary>
    /// <param name="transactionId">The transaction identifier to refund.</param>
    [Function("RefundPayment")]
    public async Task RunAsync([ActivityTrigger] string transactionId)
    {
        _logger.LogInformation(
            "Refunding payment transaction {TransactionId}",
            transactionId);

        await _paymentService.RefundPaymentAsync(transactionId);

        _logger.LogInformation(
            "Successfully refunded payment transaction {TransactionId}",
            transactionId);
    }
}
