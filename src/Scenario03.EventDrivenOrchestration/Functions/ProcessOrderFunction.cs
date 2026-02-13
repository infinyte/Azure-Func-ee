using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Scenario03.EventDrivenOrchestration.Models;
using Scenario03.EventDrivenOrchestration.Models.Dtos;
using Scenario03.EventDrivenOrchestration.Services;

namespace Scenario03.EventDrivenOrchestration.Functions;

/// <summary>
/// Service Bus triggered function that receives order creation requests, persists the order,
/// and starts the durable saga orchestration for fulfillment.
/// Uses manual message completion to ensure the message is only completed after the
/// orchestration has been successfully scheduled.
/// </summary>
public sealed class ProcessOrderFunction
{
    private readonly IOrderService _orderService;
    private readonly ILogger<ProcessOrderFunction> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public ProcessOrderFunction(IOrderService orderService, ILogger<ProcessOrderFunction> logger)
    {
        _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Processes an incoming order message from the Service Bus queue.
    /// Creates the order in the data store and starts the saga orchestration.
    /// </summary>
    /// <param name="message">The Service Bus message containing the order request.</param>
    /// <param name="messageActions">Actions for completing or dead-lettering the message.</param>
    /// <param name="durableClient">The durable task client for starting orchestrations.</param>
    [Function("ProcessOrder")]
    public async Task RunAsync(
        [ServiceBusTrigger("orders", Connection = "ServiceBusConnection")] ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions,
        [DurableClient] DurableTaskClient durableClient)
    {
        _logger.LogInformation(
            "Received order message. MessageId: {MessageId}, CorrelationId: {CorrelationId}",
            message.MessageId, message.CorrelationId);

        CreateOrderRequest? request = null;

        try
        {
            request = JsonSerializer.Deserialize<CreateOrderRequest>(message.Body, JsonOptions);

            if (request is null)
            {
                _logger.LogError("Failed to deserialize order message. Body was null after deserialization.");
                await messageActions.DeadLetterMessageAsync(
                    message,
                    deadLetterReason: "DeserializationFailed",
                    deadLetterErrorDescription: "Message body could not be deserialized to CreateOrderRequest.");
                return;
            }

            // Create the order in the data store.
            var order = await _orderService.CreateOrderAsync(request);

            _logger.LogInformation(
                "Order {OrderId} created for customer {CustomerId}. Starting saga orchestration.",
                order.Id, order.CustomerId);

            // Update status to Processing before starting the saga.
            await _orderService.UpdateOrderStatusAsync(order.Id, OrderStatus.Processing);

            // Start the durable saga orchestration.
            var instanceId = await durableClient.ScheduleNewOrchestrationInstanceAsync(
                "OrderSagaOrchestrator",
                order);

            _logger.LogInformation(
                "Saga orchestration started for order {OrderId}. Instance ID: {InstanceId}",
                order.Id, instanceId);

            // Complete the message only after the orchestration has been scheduled.
            await messageActions.CompleteMessageAsync(message);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON in order message. MessageId: {MessageId}", message.MessageId);

            await messageActions.DeadLetterMessageAsync(
                message,
                deadLetterReason: "InvalidJson",
                deadLetterErrorDescription: $"Message body is not valid JSON: {ex.Message}");
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid order request. MessageId: {MessageId}", message.MessageId);

            await messageActions.DeadLetterMessageAsync(
                message,
                deadLetterReason: "ValidationFailed",
                deadLetterErrorDescription: $"Order request validation failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error processing order message. MessageId: {MessageId}, CustomerId: {CustomerId}",
                message.MessageId, request?.CustomerId ?? "unknown");

            await messageActions.DeadLetterMessageAsync(
                message,
                deadLetterReason: "ProcessingFailed",
                deadLetterErrorDescription: $"Unexpected error: {ex.Message}");
        }
    }
}
