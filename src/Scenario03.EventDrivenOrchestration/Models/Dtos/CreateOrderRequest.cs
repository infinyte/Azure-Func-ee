using System.Text.Json.Serialization;

namespace Scenario03.EventDrivenOrchestration.Models.Dtos;

/// <summary>
/// Represents the inbound request to create a new order, received via the Service Bus queue
/// or the HTTP API endpoint.
/// </summary>
/// <param name="CustomerId">The identifier of the customer placing the order.</param>
/// <param name="Items">The line items to include in the order.</param>
public record CreateOrderRequest(
    [property: JsonPropertyName("customerId")] string CustomerId,
    [property: JsonPropertyName("items")] List<OrderItemDto> Items);
