using System.Text.Json;
using Azure.Messaging.EventGrid;
using AzureFunctions.Shared.Telemetry;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Scenario03.EventDrivenOrchestration.Models.Events;

namespace Scenario03.EventDrivenOrchestration.Functions;

/// <summary>
/// Event Grid triggered function that handles inventory domain events such as
/// stock updates and low-stock alerts. Logs event details and tracks telemetry
/// for monitoring and alerting.
/// </summary>
public sealed class HandleInventoryEventFunction
{
    private readonly ITelemetryService _telemetryService;
    private readonly ILogger<HandleInventoryEventFunction> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public HandleInventoryEventFunction(ITelemetryService telemetryService, ILogger<HandleInventoryEventFunction> logger)
    {
        _telemetryService = telemetryService ?? throw new ArgumentNullException(nameof(telemetryService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Processes an inventory event received from Event Grid.
    /// </summary>
    /// <param name="eventGridEvent">The Event Grid event containing inventory data.</param>
    [Function("HandleInventoryEvent")]
    public void Run([EventGridTrigger] EventGridEvent eventGridEvent)
    {
        _logger.LogInformation(
            "Received inventory event. Subject: {Subject}, EventType: {EventType}, Id: {EventId}",
            eventGridEvent.Subject, eventGridEvent.EventType, eventGridEvent.Id);

        InventoryEvent? inventoryEvent = null;

        if (eventGridEvent.Data is not null)
        {
            inventoryEvent = eventGridEvent.Data.ToObjectFromJson<InventoryEvent>(JsonOptions);
        }

        if (inventoryEvent is null)
        {
            _logger.LogWarning(
                "Could not deserialize inventory event data. Event ID: {EventId}",
                eventGridEvent.Id);
            return;
        }

        _logger.LogInformation(
            "Inventory event processed. Type: {EventType}, Product: {ProductId}, Quantity: {Quantity}, OccurredAt: {OccurredAt}",
            inventoryEvent.EventType, inventoryEvent.ProductId, inventoryEvent.Quantity, inventoryEvent.OccurredAt);

        // Track telemetry for monitoring and alerting.
        var properties = new Dictionary<string, string>
        {
            ["eventType"] = inventoryEvent.EventType,
            ["productId"] = inventoryEvent.ProductId,
            ["eventGridEventId"] = eventGridEvent.Id
        };

        _telemetryService.TrackEvent("InventoryEventReceived", properties);
        _telemetryService.TrackMetric("InventoryQuantity", inventoryEvent.Quantity, properties);

        // Flag low-stock alerts for operational awareness.
        if (inventoryEvent.EventType == "StockLow")
        {
            _logger.LogWarning(
                "LOW STOCK ALERT: Product {ProductId} has only {Quantity} units remaining",
                inventoryEvent.ProductId, inventoryEvent.Quantity);

            _telemetryService.TrackEvent("LowStockAlert", properties);
        }
    }
}
