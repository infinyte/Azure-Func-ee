using Microsoft.Extensions.Logging;

namespace Scenario03.EventDrivenOrchestration.Services;

/// <summary>
/// Mock implementation of <see cref="IPaymentService"/> that simulates payment processing.
/// In a production system this would call an external payment gateway via HTTP.
/// Uses <see cref="IHttpClientFactory"/> for proper HttpClient lifecycle management with
/// circuit breaker and retry policies configured via the shared resilience library.
/// </summary>
public sealed class PaymentService : IPaymentService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(IHttpClientFactory httpClientFactory, ILogger<PaymentService> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        _httpClient = httpClientFactory.CreateClient("PaymentService");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task<string> ProcessPaymentAsync(string orderId, decimal amount, string customerId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderId);
        ArgumentException.ThrowIfNullOrWhiteSpace(customerId);

        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Payment amount must be positive.");

        // In production, this would POST to the payment gateway API:
        // var response = await _httpClient.PostAsJsonAsync("/api/payments", payload, ct);
        // response.EnsureSuccessStatusCode();

        var transactionId = $"TXN-{Guid.NewGuid():N}";

        _logger.LogInformation(
            "Processed payment for order {OrderId}. Transaction ID: {TransactionId}. Amount: {Amount:C}, Customer: {CustomerId}",
            orderId, transactionId, amount, customerId);

        return Task.FromResult(transactionId);
    }

    /// <inheritdoc />
    public Task RefundPaymentAsync(string transactionId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transactionId);

        // In production, this would POST to the payment gateway refund API:
        // var response = await _httpClient.PostAsJsonAsync($"/api/payments/{transactionId}/refund", new { }, ct);
        // response.EnsureSuccessStatusCode();

        _logger.LogInformation(
            "Refunded payment transaction {TransactionId} as part of saga compensation",
            transactionId);

        return Task.CompletedTask;
    }
}
