using System.Text;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Moq;
using Scenario03.EventDrivenOrchestration.Functions;
using Scenario03.EventDrivenOrchestration.Models;
using Scenario03.EventDrivenOrchestration.Models.Dtos;
using Scenario03.EventDrivenOrchestration.Services;
using Xunit;

namespace Scenario03.EventDrivenOrchestration.Tests.Functions;

public class ProcessOrderFunctionTests
{
    private readonly Mock<IOrderService> _mockOrderService;
    private readonly Mock<ILogger<ProcessOrderFunction>> _mockLogger;
    private readonly Mock<ServiceBusMessageActions> _mockMessageActions;
    private readonly Mock<DurableTaskClient> _mockDurableClient;
    private readonly ProcessOrderFunction _sut;

    public ProcessOrderFunctionTests()
    {
        _mockOrderService = new Mock<IOrderService>();
        _mockLogger = new Mock<ILogger<ProcessOrderFunction>>();
        _mockMessageActions = new Mock<ServiceBusMessageActions>();
        _mockDurableClient = new Mock<DurableTaskClient>("test");

        _sut = new ProcessOrderFunction(_mockOrderService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task RunAsync_ValidMessage_CreatesOrderAndStartsOrchestration()
    {
        // Arrange
        var request = new CreateOrderRequest(
            CustomerId: "cust-001",
            Items: new List<OrderItemDto>
            {
                new("prod-1", "Widget", 2, 10.00m)
            });

        var order = new Order
        {
            Id = "order-new",
            CustomerId = "cust-001",
            Status = OrderStatus.Pending,
            Items = new List<OrderItem>
            {
                new("prod-1", "Widget", 2, 10.00m)
            }
        };

        _mockOrderService
            .Setup(s => s.CreateOrderAsync(It.IsAny<CreateOrderRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _mockOrderService
            .Setup(s => s.UpdateOrderStatusAsync(order.Id, OrderStatus.Processing, null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockDurableClient
            .Setup(c => c.ScheduleNewOrchestrationInstanceAsync(
                "OrderSagaOrchestrator",
                order,
                It.IsAny<StartOrchestrationOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("saga-order-new");

        _mockMessageActions
            .Setup(a => a.CompleteMessageAsync(It.IsAny<ServiceBusReceivedMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var message = CreateServiceBusMessage(request);

        // Act
        await _sut.RunAsync(message, _mockMessageActions.Object, _mockDurableClient.Object);

        // Assert
        _mockOrderService.Verify(
            s => s.CreateOrderAsync(It.IsAny<CreateOrderRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _mockOrderService.Verify(
            s => s.UpdateOrderStatusAsync(order.Id, OrderStatus.Processing, null, It.IsAny<CancellationToken>()),
            Times.Once);

        _mockDurableClient.Verify(
            c => c.ScheduleNewOrchestrationInstanceAsync(
                "OrderSagaOrchestrator",
                order,
                It.IsAny<StartOrchestrationOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mockMessageActions.Verify(
            a => a.CompleteMessageAsync(message, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_InvalidJsonMessage_DeadLettersMessage()
    {
        // Arrange
        var message = CreateServiceBusMessageFromString("not valid json {{{");

        SetupDeadLetterMock();

        // Act
        await _sut.RunAsync(message, _mockMessageActions.Object, _mockDurableClient.Object);

        // Assert
        _mockMessageActions.Verify(
            a => a.DeadLetterMessageAsync(
                message,
                It.IsAny<Dictionary<string, object>>(),
                "InvalidJson",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mockOrderService.Verify(
            s => s.CreateOrderAsync(It.IsAny<CreateOrderRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RunAsync_DeserializesToNull_DeadLettersMessage()
    {
        // Arrange — "null" is valid JSON but deserializes to null.
        var message = CreateServiceBusMessageFromString("null");

        SetupDeadLetterMock();

        // Act
        await _sut.RunAsync(message, _mockMessageActions.Object, _mockDurableClient.Object);

        // Assert
        _mockMessageActions.Verify(
            a => a.DeadLetterMessageAsync(
                message,
                It.IsAny<Dictionary<string, object>>(),
                "DeserializationFailed",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_OrderServiceThrowsArgumentException_DeadLettersMessage()
    {
        // Arrange
        var request = new CreateOrderRequest(
            CustomerId: "",
            Items: new List<OrderItemDto>());

        _mockOrderService
            .Setup(s => s.CreateOrderAsync(It.IsAny<CreateOrderRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Customer ID is required."));

        var message = CreateServiceBusMessage(request);

        SetupDeadLetterMock();

        // Act
        await _sut.RunAsync(message, _mockMessageActions.Object, _mockDurableClient.Object);

        // Assert
        _mockMessageActions.Verify(
            a => a.DeadLetterMessageAsync(
                message,
                It.IsAny<Dictionary<string, object>>(),
                "ValidationFailed",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_UnexpectedException_DeadLettersMessage()
    {
        // Arrange
        var request = new CreateOrderRequest(
            CustomerId: "cust-001",
            Items: new List<OrderItemDto> { new("prod-1", "Widget", 1, 5.00m) });

        _mockOrderService
            .Setup(s => s.CreateOrderAsync(It.IsAny<CreateOrderRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        var message = CreateServiceBusMessage(request);

        SetupDeadLetterMock();

        // Act
        await _sut.RunAsync(message, _mockMessageActions.Object, _mockDurableClient.Object);

        // Assert
        _mockMessageActions.Verify(
            a => a.DeadLetterMessageAsync(
                message,
                It.IsAny<Dictionary<string, object>>(),
                "ProcessingFailed",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Sets up the mock for <see cref="ServiceBusMessageActions.DeadLetterMessageAsync"/> to accept any invocation.
    /// </summary>
    private void SetupDeadLetterMock()
    {
        _mockMessageActions
            .Setup(a => a.DeadLetterMessageAsync(
                It.IsAny<ServiceBusReceivedMessage>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    /// <summary>
    /// Creates a <see cref="ServiceBusReceivedMessage"/> from a typed request object.
    /// </summary>
    private static ServiceBusReceivedMessage CreateServiceBusMessage<T>(T body)
    {
        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        return CreateServiceBusMessageFromString(json);
    }

    /// <summary>
    /// Creates a <see cref="ServiceBusReceivedMessage"/> from a raw JSON string.
    /// </summary>
    private static ServiceBusReceivedMessage CreateServiceBusMessageFromString(string body)
    {
        return ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString(body),
            messageId: Guid.NewGuid().ToString(),
            correlationId: Guid.NewGuid().ToString());
    }
}
