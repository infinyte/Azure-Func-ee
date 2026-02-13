using System.Text.Json.Serialization;

namespace Scenario03.EventDrivenOrchestration.Models;

/// <summary>
/// The final outcome of an order saga orchestration, returned by the orchestrator function.
/// </summary>
/// <param name="IsSuccess">Whether the saga completed all steps successfully.</param>
/// <param name="OrderId">The unique identifier of the order that was processed.</param>
/// <param name="FinalStatus">The terminal status of the order after saga completion.</param>
/// <param name="TrackingId">The shipment tracking ID, available when the order was shipped successfully.</param>
/// <param name="FailureReason">A description of the failure, available when the saga did not complete successfully.</param>
public record OrderResult(
    [property: JsonPropertyName("isSuccess")] bool IsSuccess,
    [property: JsonPropertyName("orderId")] string OrderId,
    [property: JsonPropertyName("finalStatus")] OrderStatus FinalStatus,
    [property: JsonPropertyName("trackingId")] string? TrackingId = null,
    [property: JsonPropertyName("failureReason")] string? FailureReason = null);
