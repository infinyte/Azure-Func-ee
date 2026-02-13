using System.Text.Json.Serialization;

namespace Scenario03.EventDrivenOrchestration.Models;

/// <summary>
/// Represents a single line item within an order.
/// <see cref="TotalPrice"/> is computed as <see cref="Quantity"/> multiplied by <see cref="UnitPrice"/>.
/// </summary>
/// <param name="ProductId">The unique identifier of the product.</param>
/// <param name="ProductName">The display name of the product.</param>
/// <param name="Quantity">The number of units ordered.</param>
/// <param name="UnitPrice">The price per unit.</param>
public record OrderItem(
    [property: JsonPropertyName("productId")] string ProductId,
    [property: JsonPropertyName("productName")] string ProductName,
    [property: JsonPropertyName("quantity")] int Quantity,
    [property: JsonPropertyName("unitPrice")] decimal UnitPrice)
{
    /// <summary>
    /// The total price for this line item, computed as <see cref="Quantity"/> * <see cref="UnitPrice"/>.
    /// </summary>
    [JsonPropertyName("totalPrice")]
    public decimal TotalPrice => Quantity * UnitPrice;
}
