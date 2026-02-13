using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using Scenario03.EventDrivenOrchestration.Models;

namespace Scenario03.EventDrivenOrchestration.Functions;

/// <summary>
/// Durable Functions orchestrator that implements the saga pattern for order processing.
/// Executes three sequential steps (inventory reservation, payment processing, shipment creation)
/// and performs compensating transactions in reverse order if any step fails.
/// </summary>
public static class OrderSagaOrchestrator
{
    /// <summary>
    /// Orchestrates the order fulfillment saga. Each step records its outcome in a
    /// <see cref="SagaState"/> so that the appropriate compensating actions can be taken on failure.
    /// </summary>
    /// <param name="context">The durable task orchestration context.</param>
    /// <returns>An <see cref="OrderResult"/> describing the final outcome.</returns>
    [Function("OrderSagaOrchestrator")]
    public static async Task<OrderResult> RunAsync([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var logger = context.CreateReplaySafeLogger("OrderSagaOrchestrator");
        var order = context.GetInput<Order>()
            ?? throw new InvalidOperationException("Order input is required for the saga orchestrator.");

        var sagaState = new SagaState { OrderId = order.Id };

        logger.LogInformation("Starting order saga for order {OrderId}", order.Id);

        // ---- Step 1: Reserve Inventory ----
        try
        {
            logger.LogInformation("Step 1: Reserving inventory for order {OrderId}", order.Id);

            var reservationId = await context.CallActivityAsync<string>("ReserveInventory", order);

            sagaState.InventoryReserved = true;
            sagaState.InventoryReservationId = reservationId;
            sagaState.CompletedSteps.Add("ReserveInventory");

            logger.LogInformation(
                "Step 1 completed: Inventory reserved for order {OrderId}. Reservation: {ReservationId}",
                order.Id, reservationId);
        }
        catch (TaskFailedException ex)
        {
            logger.LogError(
                "Step 1 failed: Inventory reservation failed for order {OrderId}. Error: {Error}",
                order.Id, ex.Message);

            sagaState.FailureStep = "ReserveInventory";
            sagaState.FailureReason = ex.Message;

            // No compensation needed -- nothing was completed yet.
            return new OrderResult(
                IsSuccess: false,
                OrderId: order.Id,
                FinalStatus: OrderStatus.Failed,
                FailureReason: $"Inventory reservation failed: {ex.Message}");
        }

        // ---- Step 2: Process Payment ----
        try
        {
            logger.LogInformation("Step 2: Processing payment for order {OrderId}", order.Id);

            var transactionId = await context.CallActivityAsync<string>("ProcessPayment", order);

            sagaState.PaymentProcessed = true;
            sagaState.PaymentTransactionId = transactionId;
            sagaState.CompletedSteps.Add("ProcessPayment");

            logger.LogInformation(
                "Step 2 completed: Payment processed for order {OrderId}. Transaction: {TransactionId}",
                order.Id, transactionId);
        }
        catch (TaskFailedException ex)
        {
            logger.LogError(
                "Step 2 failed: Payment processing failed for order {OrderId}. Error: {Error}",
                order.Id, ex.Message);

            sagaState.FailureStep = "ProcessPayment";
            sagaState.FailureReason = ex.Message;

            // Compensate Step 1: Release inventory reservation.
            await CompensateAsync(context, sagaState, logger);

            return new OrderResult(
                IsSuccess: false,
                OrderId: order.Id,
                FinalStatus: OrderStatus.Failed,
                FailureReason: $"Payment processing failed: {ex.Message}");
        }

        // ---- Step 3: Create Shipment ----
        try
        {
            logger.LogInformation("Step 3: Creating shipment for order {OrderId}", order.Id);

            var trackingId = await context.CallActivityAsync<string>("CreateShipment", order);

            sagaState.ShipmentCreated = true;
            sagaState.ShipmentTrackingId = trackingId;
            sagaState.CompletedSteps.Add("CreateShipment");

            logger.LogInformation(
                "Step 3 completed: Shipment created for order {OrderId}. Tracking: {TrackingId}",
                order.Id, trackingId);
        }
        catch (TaskFailedException ex)
        {
            logger.LogError(
                "Step 3 failed: Shipment creation failed for order {OrderId}. Error: {Error}",
                order.Id, ex.Message);

            sagaState.FailureStep = "CreateShipment";
            sagaState.FailureReason = ex.Message;

            // Compensate Steps 1 and 2: Refund payment, then release inventory.
            await CompensateAsync(context, sagaState, logger);

            return new OrderResult(
                IsSuccess: false,
                OrderId: order.Id,
                FinalStatus: OrderStatus.Failed,
                FailureReason: $"Shipment creation failed: {ex.Message}");
        }

        // ---- All steps succeeded ----
        logger.LogInformation(
            "Order saga completed successfully for order {OrderId}. Tracking: {TrackingId}",
            order.Id, sagaState.ShipmentTrackingId);

        return new OrderResult(
            IsSuccess: true,
            OrderId: order.Id,
            FinalStatus: OrderStatus.Completed,
            TrackingId: sagaState.ShipmentTrackingId);
    }

    /// <summary>
    /// Executes compensating transactions in reverse order based on what has been completed so far.
    /// </summary>
    private static async Task CompensateAsync(
        TaskOrchestrationContext context,
        SagaState sagaState,
        ILogger logger)
    {
        logger.LogWarning(
            "Starting compensation for order {OrderId}. Failed at step: {FailureStep}",
            sagaState.OrderId, sagaState.FailureStep);

        // Compensate payment first (reverse order).
        if (sagaState.PaymentProcessed && sagaState.PaymentTransactionId is not null)
        {
            try
            {
                logger.LogInformation(
                    "Compensating payment transaction {TransactionId} for order {OrderId}",
                    sagaState.PaymentTransactionId, sagaState.OrderId);

                await context.CallActivityAsync("RefundPayment", sagaState.PaymentTransactionId);
                sagaState.CompensatedSteps.Add("RefundPayment");

                logger.LogInformation(
                    "Payment compensation succeeded for order {OrderId}",
                    sagaState.OrderId);
            }
            catch (TaskFailedException ex)
            {
                logger.LogError(
                    "Payment compensation FAILED for order {OrderId}. Transaction: {TransactionId}. Error: {Error}. Manual intervention required.",
                    sagaState.OrderId, sagaState.PaymentTransactionId, ex.Message);
            }
        }

        // Compensate inventory reservation.
        if (sagaState.InventoryReserved && sagaState.InventoryReservationId is not null)
        {
            try
            {
                logger.LogInformation(
                    "Compensating inventory reservation {ReservationId} for order {OrderId}",
                    sagaState.InventoryReservationId, sagaState.OrderId);

                await context.CallActivityAsync("CompensateInventory", sagaState.InventoryReservationId);
                sagaState.CompensatedSteps.Add("CompensateInventory");

                logger.LogInformation(
                    "Inventory compensation succeeded for order {OrderId}",
                    sagaState.OrderId);
            }
            catch (TaskFailedException ex)
            {
                logger.LogError(
                    "Inventory compensation FAILED for order {OrderId}. Reservation: {ReservationId}. Error: {Error}. Manual intervention required.",
                    sagaState.OrderId, sagaState.InventoryReservationId, ex.Message);
            }
        }

        logger.LogWarning(
            "Compensation completed for order {OrderId}. Compensated steps: [{CompensatedSteps}]",
            sagaState.OrderId, string.Join(", ", sagaState.CompensatedSteps));
    }
}
