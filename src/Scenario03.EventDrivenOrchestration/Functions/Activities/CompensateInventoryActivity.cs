using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Scenario03.EventDrivenOrchestration.Services;

namespace Scenario03.EventDrivenOrchestration.Functions.Activities;

/// <summary>
/// Durable Functions compensating activity that releases a previously made inventory reservation.
/// Called by the <see cref="OrderSagaOrchestrator"/> when a downstream saga step fails.
/// </summary>
public sealed class CompensateInventoryActivity
{
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<CompensateInventoryActivity> _logger;

    public CompensateInventoryActivity(IInventoryService inventoryService, ILogger<CompensateInventoryActivity> logger)
    {
        _inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Releases the inventory reservation identified by the given reservation ID.
    /// </summary>
    /// <param name="reservationId">The reservation identifier to release.</param>
    [Function("CompensateInventory")]
    public async Task RunAsync([ActivityTrigger] string reservationId)
    {
        _logger.LogInformation(
            "Compensating inventory reservation {ReservationId}",
            reservationId);

        await _inventoryService.ReleaseInventoryAsync(reservationId);

        _logger.LogInformation(
            "Successfully compensated inventory reservation {ReservationId}",
            reservationId);
    }
}
