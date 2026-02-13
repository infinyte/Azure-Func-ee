using System.Text.Json.Serialization;

namespace Scenario03.EventDrivenOrchestration.Models.Dtos;

/// <summary>
/// The API response representation of an order, mapped from the domain <see cref="Order"/> entity.
/// </summary>
/// <param name="Id">The unique identifier of the order.</param>
/// <param name="CustomerId">The identifier of the customer who placed the order.</param>
/// <param name="Status">The current lifecycle status of the order.</param>
/// <param name="TotalAmount">The total monetary amount for all line items.</param>
/// <param name="CreatedAt">The UTC timestamp when the order was created.</param>
/// <param name="CompletedAt">The UTC timestamp when the order reached a terminal state, if applicable.</param>
/// <param name="FailureReason">The reason for failure, if the order did not complete successfully.</param>
/// <param name="Items">The line items in the order.</param>
public record OrderResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("customerId")] string CustomerId,
    [property: JsonPropertyName("status")] OrderStatus Status,
    [property: JsonPropertyName("totalAmount")] decimal TotalAmount,
    [property: JsonPropertyName("createdAt")] DateTimeOffset CreatedAt,
    [property: JsonPropertyName("completedAt")] DateTimeOffset? CompletedAt,
    [property: JsonPropertyName("failureReason")] string? FailureReason,
    [property: JsonPropertyName("items")] List<OrderItemDto> Items)
{
    /// <summary>
    /// Creates an <see cref="OrderResponse"/> from a domain <see cref="Order"/> entity.
    /// </summary>
    /// <param name="order">The domain order to map.</param>
    /// <returns>An <see cref="OrderResponse"/> populated from the order.</returns>
    public static OrderResponse FromOrder(Order order) =>
        new(
            Id: order.Id,
            CustomerId: order.CustomerId,
            Status: order.Status,
            TotalAmount: order.TotalAmount,
            CreatedAt: order.CreatedAt,
            CompletedAt: order.CompletedAt,
            FailureReason: order.FailureReason,
            Items: order.Items.Select(i => new OrderItemDto(
                ProductId: i.ProductId,
                ProductName: i.ProductName,
                Quantity: i.Quantity,
                UnitPrice: i.UnitPrice)).ToList());
}
