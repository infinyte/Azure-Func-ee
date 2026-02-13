using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Scenario03.EventDrivenOrchestration.Models;
using Scenario03.EventDrivenOrchestration.Models.Dtos;
using Scenario03.EventDrivenOrchestration.Repositories;
using Scenario03.EventDrivenOrchestration.Services;
using Xunit;

namespace Scenario03.EventDrivenOrchestration.Tests.Services;

public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _mockRepository;
    private readonly Mock<ILogger<OrderService>> _mockLogger;
    private readonly OrderService _sut;

    public OrderServiceTests()
    {
        _mockRepository = new Mock<IOrderRepository>();
        _mockLogger = new Mock<ILogger<OrderService>>();
        _sut = new OrderService(_mockRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task CreateOrderAsync_WithValidRequest_CreatesOrderWithPendingStatus()
    {
        // Arrange
        var request = new CreateOrderRequest(
            CustomerId: "cust-100",
            Items: new List<OrderItemDto>
            {
                new("prod-1", "Widget", 2, 10.00m),
                new("prod-2", "Gadget", 1, 25.00m)
            });

        _mockRepository
            .Setup(r => r.UpsertAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var order = await _sut.CreateOrderAsync(request);

        // Assert
        order.Should().NotBeNull();
        order.CustomerId.Should().Be("cust-100");
        order.Status.Should().Be(OrderStatus.Pending);
        order.Items.Should().HaveCount(2);
        order.Id.Should().NotBeNullOrWhiteSpace();
        order.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateOrderAsync_PersistsOrderViaRepository()
    {
        // Arrange
        var request = new CreateOrderRequest(
            CustomerId: "cust-persist",
            Items: new List<OrderItemDto>
            {
                new("prod-1", "Widget", 1, 5.00m)
            });

        _mockRepository
            .Setup(r => r.UpsertAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.CreateOrderAsync(request);

        // Assert
        _mockRepository.Verify(
            r => r.UpsertAsync(It.Is<Order>(o => o.CustomerId == "cust-persist"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateOrderAsync_MapsOrderItemDtosToOrderItems()
    {
        // Arrange
        var request = new CreateOrderRequest(
            CustomerId: "cust-map",
            Items: new List<OrderItemDto>
            {
                new("prod-A", "Alpha", 3, 12.50m)
            });

        Order? capturedOrder = null;
        _mockRepository
            .Setup(r => r.UpsertAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((o, _) => capturedOrder = o)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.CreateOrderAsync(request);

        // Assert
        capturedOrder.Should().NotBeNull();
        capturedOrder!.Items.Should().HaveCount(1);
        capturedOrder.Items[0].ProductId.Should().Be("prod-A");
        capturedOrder.Items[0].ProductName.Should().Be("Alpha");
        capturedOrder.Items[0].Quantity.Should().Be(3);
        capturedOrder.Items[0].UnitPrice.Should().Be(12.50m);
    }

    [Fact]
    public async Task CreateOrderAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _sut.CreateOrderAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CreateOrderAsync_WithEmptyCustomerId_ThrowsArgumentException()
    {
        // Arrange
        var request = new CreateOrderRequest(
            CustomerId: "",
            Items: new List<OrderItemDto> { new("p1", "Widget", 1, 1.00m) });

        // Act
        var act = () => _sut.CreateOrderAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Customer ID*");
    }

    [Fact]
    public async Task CreateOrderAsync_WithNoItems_ThrowsArgumentException()
    {
        // Arrange
        var request = new CreateOrderRequest(
            CustomerId: "cust-no-items",
            Items: new List<OrderItemDto>());

        // Act
        var act = () => _sut.CreateOrderAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*At least one order item*");
    }

    [Fact]
    public async Task GetOrderAsync_ReturnsOrderFromRepository()
    {
        // Arrange
        var orderId = "order-get-1";
        var expectedOrder = new Order
        {
            Id = orderId,
            CustomerId = "cust-get",
            Status = OrderStatus.Completed
        };

        _mockRepository
            .Setup(r => r.GetAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedOrder);

        // Act
        var result = await _sut.GetOrderAsync(orderId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(orderId);
        result.CustomerId.Should().Be("cust-get");
    }

    [Fact]
    public async Task GetOrderAsync_WhenNotFound_ReturnsNull()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        // Act
        var result = await _sut.GetOrderAsync("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetOrderAsync_WithNullOrderId_ThrowsArgumentException()
    {
        // Act
        var act = () => _sut.GetOrderAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_UpdatesStatusAndPersists()
    {
        // Arrange
        var orderId = "order-update-1";
        var existingOrder = new Order
        {
            Id = orderId,
            CustomerId = "cust-update",
            Status = OrderStatus.Processing
        };

        _mockRepository
            .Setup(r => r.GetAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);

        _mockRepository
            .Setup(r => r.UpsertAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.UpdateOrderStatusAsync(orderId, OrderStatus.Completed);

        // Assert
        _mockRepository.Verify(
            r => r.UpsertAsync(
                It.Is<Order>(o => o.Status == OrderStatus.Completed),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_WhenOrderNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetAsync("missing-order", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        // Act
        var act = () => _sut.UpdateOrderStatusAsync("missing-order", OrderStatus.Completed);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*missing-order*");
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_WithReason_SetsFailureReason()
    {
        // Arrange
        var orderId = "order-reason";
        var order = new Order { Id = orderId, CustomerId = "cust-r", Status = OrderStatus.Processing };

        _mockRepository.Setup(r => r.GetAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _mockRepository.Setup(r => r.UpsertAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        await _sut.UpdateOrderStatusAsync(orderId, OrderStatus.Failed, "Payment declined");

        // Assert
        _mockRepository.Verify(
            r => r.UpsertAsync(
                It.Is<Order>(o => o.FailureReason == "Payment declined" && o.Status == OrderStatus.Failed),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(OrderStatus.Completed)]
    [InlineData(OrderStatus.Failed)]
    [InlineData(OrderStatus.Cancelled)]
    public async Task UpdateOrderStatusAsync_TerminalStatus_SetsCompletedAt(OrderStatus terminalStatus)
    {
        // Arrange
        var orderId = "order-terminal";
        var order = new Order { Id = orderId, CustomerId = "cust-t", Status = OrderStatus.Processing };

        _mockRepository.Setup(r => r.GetAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _mockRepository.Setup(r => r.UpsertAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        await _sut.UpdateOrderStatusAsync(orderId, terminalStatus);

        // Assert
        _mockRepository.Verify(
            r => r.UpsertAsync(
                It.Is<Order>(o => o.CompletedAt.HasValue),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_NonTerminalStatus_DoesNotSetCompletedAt()
    {
        // Arrange
        var orderId = "order-non-terminal";
        var order = new Order { Id = orderId, CustomerId = "cust-nt", Status = OrderStatus.Pending };

        _mockRepository.Setup(r => r.GetAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _mockRepository.Setup(r => r.UpsertAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        await _sut.UpdateOrderStatusAsync(orderId, OrderStatus.Processing);

        // Assert
        _mockRepository.Verify(
            r => r.UpsertAsync(
                It.Is<Order>(o => !o.CompletedAt.HasValue),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
