using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Scenario03.EventDrivenOrchestration.Models;
using Scenario03.EventDrivenOrchestration.Services;

namespace Scenario03.EventDrivenOrchestration.Functions.Activities;

/// <summary>
/// Durable Functions activity that reserves inventory for all items in an order.
/// Called as a saga step by the <see cref="OrderSagaOrchestrator"/>.
/// </summary>
public sealed class ReserveInventoryActivity
{
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<ReserveInventoryActivity> _logger;

    public ReserveInventoryActivity(IInventoryService inventoryService, ILogger<ReserveInventoryActivity> logger)
    {
        _inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Reserves inventory for the given order's items.
    /// </summary>
    /// <param name="order">The order containing items to reserve.</param>
    /// <returns>The inventory reservation identifier.</returns>
    [Function("ReserveInventory")]
    public async Task<string> RunAsync([ActivityTrigger] Order order)
    {
        _logger.LogInformation(
            "Reserving inventory for order {OrderId} with {ItemCount} items",
            order.Id, order.Items.Count);

        var reservationId = await _inventoryService.ReserveInventoryAsync(order.Id, order.Items);

        _logger.LogInformation(
            "Inventory reserved for order {OrderId}. Reservation ID: {ReservationId}",
            order.Id, reservationId);

        return reservationId;
    }
}
