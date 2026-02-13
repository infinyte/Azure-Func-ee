using System.Text.Json.Serialization;

namespace Scenario03.EventDrivenOrchestration.Models;

/// <summary>
/// Represents a customer order that flows through the saga orchestration pipeline.
/// <see cref="TotalAmount"/> is computed from the sum of all <see cref="Items"/>.
/// </summary>
public class Order
{
    /// <summary>
    /// The unique identifier for this order. Also used as the Cosmos DB partition key.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The identifier of the customer who placed the order.
    /// </summary>
    [JsonPropertyName("customerId")]
    public string CustomerId { get; set; } = string.Empty;

    /// <summary>
    /// The line items included in this order.
    /// </summary>
    [JsonPropertyName("items")]
    public List<OrderItem> Items { get; set; } = [];

    /// <summary>
    /// The current status of the order within the saga lifecycle.
    /// </summary>
    [JsonPropertyName("status")]
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    /// <summary>
    /// The total monetary amount for the order, computed as the sum of each item's total price.
    /// </summary>
    [JsonPropertyName("totalAmount")]
    public decimal TotalAmount => Items.Sum(i => i.TotalPrice);

    /// <summary>
    /// The UTC timestamp when the order was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// The UTC timestamp when the order reached a terminal state (Completed, Failed, or Cancelled).
    /// Null while the order is still in progress.
    /// </summary>
    [JsonPropertyName("completedAt")]
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// A description of why the order failed, if applicable.
    /// </summary>
    [JsonPropertyName("failureReason")]
    public string? FailureReason { get; set; }
}
