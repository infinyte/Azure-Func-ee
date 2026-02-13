using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Scenario03.EventDrivenOrchestration.Functions.Activities;
using Scenario03.EventDrivenOrchestration.Models;
using Scenario03.EventDrivenOrchestration.Services;
using Xunit;

namespace Scenario03.EventDrivenOrchestration.Tests.Functions.Activities;

public class ReserveInventoryActivityTests
{
    private readonly Mock<IInventoryService> _mockInventoryService;
    private readonly Mock<ILogger<ReserveInventoryActivity>> _mockLogger;
    private readonly ReserveInventoryActivity _sut;

    public ReserveInventoryActivityTests()
    {
        _mockInventoryService = new Mock<IInventoryService>();
        _mockLogger = new Mock<ILogger<ReserveInventoryActivity>>();
        _sut = new ReserveInventoryActivity(_mockInventoryService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task RunAsync_CallsInventoryServiceReserveInventoryAsync()
    {
        // Arrange
        var order = new Order
        {
            Id = "order-reserve-1",
            CustomerId = "cust-1",
            Items = new List<OrderItem>
            {
                new("prod-1", "Widget", 3, 15.00m),
                new("prod-2", "Gadget", 1, 25.00m)
            }
        };

        _mockInventoryService
            .Setup(s => s.ReserveInventoryAsync(
                order.Id,
                order.Items,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("reservation-abc");

        // Act
        var reservationId = await _sut.RunAsync(order);

        // Assert
        reservationId.Should().Be("reservation-abc");
        _mockInventoryService.Verify(
            s => s.ReserveInventoryAsync(order.Id, order.Items, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_WithMultipleItems_PassesAllItemsToService()
    {
        // Arrange
        var items = new List<OrderItem>
        {
            new("prod-1", "Widget A", 1, 10.00m),
            new("prod-2", "Widget B", 2, 20.00m),
            new("prod-3", "Widget C", 3, 30.00m)
        };

        var order = new Order
        {
            Id = "order-multi",
            CustomerId = "cust-2",
            Items = items
        };

        List<OrderItem>? capturedItems = null;

        _mockInventoryService
            .Setup(s => s.ReserveInventoryAsync(
                It.IsAny<string>(),
                It.IsAny<List<OrderItem>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, List<OrderItem>, CancellationToken>((_, i, _) => capturedItems = i)
            .ReturnsAsync("reservation-multi");

        // Act
        await _sut.RunAsync(order);

        // Assert
        capturedItems.Should().NotBeNull();
        capturedItems.Should().HaveCount(3);
        capturedItems![0].ProductId.Should().Be("prod-1");
        capturedItems[1].ProductId.Should().Be("prod-2");
        capturedItems[2].ProductId.Should().Be("prod-3");
    }

    [Fact]
    public async Task RunAsync_ReturnsReservationIdFromService()
    {
        // Arrange
        var order = new Order
        {
            Id = "order-return",
            CustomerId = "cust-3",
            Items = new List<OrderItem>
            {
                new("prod-1", "Test Product", 1, 5.00m)
            }
        };

        _mockInventoryService
            .Setup(s => s.ReserveInventoryAsync(
                It.IsAny<string>(),
                It.IsAny<List<OrderItem>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("reservation-xyz-789");

        // Act
        var result = await _sut.RunAsync(order);

        // Assert
        result.Should().Be("reservation-xyz-789");
    }

    [Fact]
    public void Constructor_WithNullInventoryService_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ReserveInventoryActivity(null!, _mockLogger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("inventoryService");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ReserveInventoryActivity(_mockInventoryService.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }
}
