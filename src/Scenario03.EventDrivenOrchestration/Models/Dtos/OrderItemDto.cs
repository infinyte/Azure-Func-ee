using System.Text.Json.Serialization;

namespace Scenario03.EventDrivenOrchestration.Models.Dtos;

/// <summary>
/// A data transfer object representing an order line item in API requests and responses.
/// </summary>
/// <param name="ProductId">The unique identifier of the product.</param>
/// <param name="ProductName">The display name of the product.</param>
/// <param name="Quantity">The number of units ordered.</param>
/// <param name="UnitPrice">The price per unit.</param>
public record OrderItemDto(
    [property: JsonPropertyName("productId")] string ProductId,
    [property: JsonPropertyName("productName")] string ProductName,
    [property: JsonPropertyName("quantity")] int Quantity,
    [property: JsonPropertyName("unitPrice")] decimal UnitPrice);
