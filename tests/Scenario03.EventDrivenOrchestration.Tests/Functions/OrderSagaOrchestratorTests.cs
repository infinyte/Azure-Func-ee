using FluentAssertions;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using Moq;
using Scenario03.EventDrivenOrchestration.Functions;
using Scenario03.EventDrivenOrchestration.Models;
using Xunit;

namespace Scenario03.EventDrivenOrchestration.Tests.Functions;

public class OrderSagaOrchestratorTests
{
    private readonly Mock<TaskOrchestrationContext> _mockContext;
    private readonly Order _testOrder;

    public OrderSagaOrchestratorTests()
    {
        _mockContext = new Mock<TaskOrchestrationContext>();
        _mockContext
            .Setup(c => c.CreateReplaySafeLogger(It.IsAny<string>()))
            .Returns(Mock.Of<ILogger>());

        _testOrder = new Order
        {
            Id = "order-001",
            CustomerId = "customer-001",
            Status = OrderStatus.Processing,
            Items = new List<OrderItem>
            {
                new("prod-1", "Widget", 2, 10.00m)
            }
        };

        _mockContext.Setup(c => c.GetInput<Order>()).Returns(_testOrder);
    }

    [Fact]
    public async Task RunAsync_AllStepsSucceed_ReturnsSuccessWithCompletedStatus()
    {
        // Arrange
        _mockContext
            .Setup(c => c.CallActivityAsync<string>("ReserveInventory", _testOrder, It.IsAny<TaskOptions>()))
            .ReturnsAsync("reservation-001");

        _mockContext
            .Setup(c => c.CallActivityAsync<string>("ProcessPayment", _testOrder, It.IsAny<TaskOptions>()))
            .ReturnsAsync("txn-001");

        _mockContext
            .Setup(c => c.CallActivityAsync<string>("CreateShipment", _testOrder, It.IsAny<TaskOptions>()))
            .ReturnsAsync("tracking-001");

        // Act
        var result = await OrderSagaOrchestrator.RunAsync(_mockContext.Object);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.OrderId.Should().Be("order-001");
        result.FinalStatus.Should().Be(OrderStatus.Completed);
        result.TrackingId.Should().Be("tracking-001");
        result.FailureReason.Should().BeNull();
    }

    [Fact]
    public async Task RunAsync_InventoryReservationFails_ReturnsFailureWithNoCompensation()
    {
        // Arrange — Step 1 fails.
        _mockContext
            .Setup(c => c.CallActivityAsync<string>("ReserveInventory", _testOrder, It.IsAny<TaskOptions>()))
            .ThrowsAsync(CreateTaskFailedException("ReserveInventory", "Out of stock"));

        // Act
        var result = await OrderSagaOrchestrator.RunAsync(_mockContext.Object);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.FinalStatus.Should().Be(OrderStatus.Failed);
        result.FailureReason.Should().Contain("Inventory reservation failed");

        // No compensation should be called since nothing was completed.
        _mockContext.Verify(
            c => c.CallActivityAsync("CompensateInventory", It.IsAny<string>(), It.IsAny<TaskOptions>()),
            Times.Never);
        _mockContext.Verify(
            c => c.CallActivityAsync("RefundPayment", It.IsAny<string>(), It.IsAny<TaskOptions>()),
            Times.Never);
    }

    [Fact]
    public async Task RunAsync_PaymentFails_CompensatesInventoryReservation()
    {
        // Arrange — Step 1 succeeds, Step 2 fails.
        _mockContext
            .Setup(c => c.CallActivityAsync<string>("ReserveInventory", _testOrder, It.IsAny<TaskOptions>()))
            .ReturnsAsync("reservation-002");

        _mockContext
            .Setup(c => c.CallActivityAsync<string>("ProcessPayment", _testOrder, It.IsAny<TaskOptions>()))
            .ThrowsAsync(CreateTaskFailedException("ProcessPayment", "Insufficient funds"));

        _mockContext
            .Setup(c => c.CallActivityAsync("CompensateInventory", "reservation-002", It.IsAny<TaskOptions>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await OrderSagaOrchestrator.RunAsync(_mockContext.Object);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.FinalStatus.Should().Be(OrderStatus.Failed);
        result.FailureReason.Should().Contain("Payment processing failed");

        // Inventory compensation should be called.
        _mockContext.Verify(
            c => c.CallActivityAsync("CompensateInventory", "reservation-002", It.IsAny<TaskOptions>()),
            Times.Once);

        // Payment refund should NOT be called since payment was never completed.
        _mockContext.Verify(
            c => c.CallActivityAsync("RefundPayment", It.IsAny<string>(), It.IsAny<TaskOptions>()),
            Times.Never);
    }

    [Fact]
    public async Task RunAsync_ShipmentFails_CompensatesPaymentAndInventory()
    {
        // Arrange — Steps 1 and 2 succeed, Step 3 fails.
        _mockContext
            .Setup(c => c.CallActivityAsync<string>("ReserveInventory", _testOrder, It.IsAny<TaskOptions>()))
            .ReturnsAsync("reservation-003");

        _mockContext
            .Setup(c => c.CallActivityAsync<string>("ProcessPayment", _testOrder, It.IsAny<TaskOptions>()))
            .ReturnsAsync("txn-003");

        _mockContext
            .Setup(c => c.CallActivityAsync<string>("CreateShipment", _testOrder, It.IsAny<TaskOptions>()))
            .ThrowsAsync(CreateTaskFailedException("CreateShipment", "Shipping unavailable"));

        _mockContext
            .Setup(c => c.CallActivityAsync("RefundPayment", "txn-003", It.IsAny<TaskOptions>()))
            .Returns(Task.CompletedTask);

        _mockContext
            .Setup(c => c.CallActivityAsync("CompensateInventory", "reservation-003", It.IsAny<TaskOptions>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await OrderSagaOrchestrator.RunAsync(_mockContext.Object);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.FinalStatus.Should().Be(OrderStatus.Failed);
        result.FailureReason.Should().Contain("Shipment creation failed");

        // Both compensation steps should be called (payment refund first, then inventory).
        _mockContext.Verify(
            c => c.CallActivityAsync("RefundPayment", "txn-003", It.IsAny<TaskOptions>()),
            Times.Once);
        _mockContext.Verify(
            c => c.CallActivityAsync("CompensateInventory", "reservation-003", It.IsAny<TaskOptions>()),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_NullOrderInput_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockContext.Setup(c => c.GetInput<Order>()).Returns((Order?)null);

        // Act
        var act = () => OrderSagaOrchestrator.RunAsync(_mockContext.Object);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Order input is required*");
    }

    /// <summary>
    /// Helper to create a <see cref="TaskFailedException"/> with appropriate constructor parameters.
    /// TaskFailedException requires a task name and <see cref="TaskFailureDetails"/>.
    /// </summary>
    private static TaskFailedException CreateTaskFailedException(string taskName, string errorMessage)
    {
        var failureDetails = new TaskFailureDetails(
            typeof(Exception).FullName!,
            errorMessage,
            null,
            null);

        return new TaskFailedException(taskName, 1, failureDetails);
    }
}
