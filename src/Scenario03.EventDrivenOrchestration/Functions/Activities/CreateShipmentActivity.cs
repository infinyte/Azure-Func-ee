using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Scenario03.EventDrivenOrchestration.Models;

namespace Scenario03.EventDrivenOrchestration.Functions.Activities;

/// <summary>
/// Durable Functions activity that creates a shipment for a completed order.
/// Called as a saga step by the <see cref="OrderSagaOrchestrator"/>.
/// Simulates integration with an external shipping provider.
/// </summary>
public sealed class CreateShipmentActivity
{
    private readonly ILogger<CreateShipmentActivity> _logger;

    public CreateShipmentActivity(ILogger<CreateShipmentActivity> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a shipment for the given order.
    /// </summary>
    /// <param name="order">The order to ship.</param>
    /// <returns>The shipment tracking identifier.</returns>
    [Function("CreateShipment")]
    public Task<string> RunAsync([ActivityTrigger] Order order)
    {
        _logger.LogInformation(
            "Creating shipment for order {OrderId}. Customer: {CustomerId}, Items: {ItemCount}",
            order.Id, order.CustomerId, order.Items.Count);

        // In production, this would call an external shipping provider API.
        var trackingId = $"SHIP-{Guid.NewGuid():N}";

        _logger.LogInformation(
            "Shipment created for order {OrderId}. Tracking ID: {TrackingId}",
            order.Id, trackingId);

        return Task.FromResult(trackingId);
    }
}
