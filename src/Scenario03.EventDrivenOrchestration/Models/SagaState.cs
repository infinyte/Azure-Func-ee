using System.Text.Json.Serialization;

namespace Scenario03.EventDrivenOrchestration.Models;

/// <summary>
/// Tracks the progress and compensation state of an order saga orchestration.
/// Used by the orchestrator to determine which compensating actions are needed if a step fails.
/// </summary>
public class SagaState
{
    /// <summary>
    /// The order identifier that this saga is processing.
    /// </summary>
    [JsonPropertyName("orderId")]
    public string OrderId { get; set; } = string.Empty;

    /// <summary>
    /// Whether inventory has been successfully reserved for this order.
    /// </summary>
    [JsonPropertyName("inventoryReserved")]
    public bool InventoryReserved { get; set; }

    /// <summary>
    /// The reservation identifier returned by the inventory service, used for compensation.
    /// </summary>
    [JsonPropertyName("inventoryReservationId")]
    public string? InventoryReservationId { get; set; }

    /// <summary>
    /// Whether payment has been successfully processed for this order.
    /// </summary>
    [JsonPropertyName("paymentProcessed")]
    public bool PaymentProcessed { get; set; }

    /// <summary>
    /// The transaction identifier returned by the payment service, used for refund compensation.
    /// </summary>
    [JsonPropertyName("paymentTransactionId")]
    public string? PaymentTransactionId { get; set; }

    /// <summary>
    /// Whether a shipment has been created for this order.
    /// </summary>
    [JsonPropertyName("shipmentCreated")]
    public bool ShipmentCreated { get; set; }

    /// <summary>
    /// The tracking identifier returned by the shipment service.
    /// </summary>
    [JsonPropertyName("shipmentTrackingId")]
    public string? ShipmentTrackingId { get; set; }

    /// <summary>
    /// An ordered list of saga steps that completed successfully.
    /// </summary>
    [JsonPropertyName("completedSteps")]
    public List<string> CompletedSteps { get; set; } = [];

    /// <summary>
    /// An ordered list of saga steps that have been compensated (rolled back).
    /// </summary>
    [JsonPropertyName("compensatedSteps")]
    public List<string> CompensatedSteps { get; set; } = [];

    /// <summary>
    /// The name of the step that caused the saga to fail, if any.
    /// </summary>
    [JsonPropertyName("failureStep")]
    public string? FailureStep { get; set; }

    /// <summary>
    /// A description of why the failing step failed.
    /// </summary>
    [JsonPropertyName("failureReason")]
    public string? FailureReason { get; set; }
}
