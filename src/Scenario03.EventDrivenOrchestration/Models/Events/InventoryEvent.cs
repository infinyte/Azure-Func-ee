using System.Text.Json.Serialization;

namespace Scenario03.EventDrivenOrchestration.Models.Events;

/// <summary>
/// Represents an inventory domain event received via Event Grid, such as stock updates or low-stock alerts.
/// </summary>
/// <param name="EventType">The type of inventory event (e.g., "InventoryUpdated", "StockLow").</param>
/// <param name="ProductId">The identifier of the product affected by this event.</param>
/// <param name="Quantity">The quantity associated with the event (e.g., current stock level or quantity changed).</param>
/// <param name="OccurredAt">The UTC timestamp when the event occurred in the source system.</param>
public record InventoryEvent(
    [property: JsonPropertyName("eventType")] string EventType,
    [property: JsonPropertyName("productId")] string ProductId,
    [property: JsonPropertyName("quantity")] int Quantity,
    [property: JsonPropertyName("occurredAt")] DateTimeOffset OccurredAt);
