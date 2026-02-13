using System.Net;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Moq;
using Scenario03.EventDrivenOrchestration.Models;
using Scenario03.EventDrivenOrchestration.Repositories;
using Xunit;

namespace Scenario03.EventDrivenOrchestration.Tests.Repositories;

public class CosmosDbOrderRepositoryTests
{
    private readonly Mock<Container> _mockContainer;
    private readonly Mock<ILogger<CosmosDbOrderRepository>> _mockLogger;
    private readonly CosmosDbOrderRepository _sut;

    public CosmosDbOrderRepositoryTests()
    {
        _mockContainer = new Mock<Container>();
        _mockLogger = new Mock<ILogger<CosmosDbOrderRepository>>();
        _sut = new CosmosDbOrderRepository(_mockContainer.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task UpsertAsync_UpsertItemInContainer()
    {
        // Arrange
        var order = new Order
        {
            Id = "order-upsert-1",
            CustomerId = "cust-1",
            Status = OrderStatus.Pending,
            Items = new List<OrderItem>
            {
                new("prod-1", "Widget", 1, 10.00m)
            }
        };

        var mockResponse = new Mock<ItemResponse<Order>>();
        mockResponse.Setup(r => r.StatusCode).Returns(HttpStatusCode.OK);
        mockResponse.Setup(r => r.RequestCharge).Returns(5.0);

        _mockContainer
            .Setup(c => c.UpsertItemAsync(
                order,
                It.Is<PartitionKey>(pk => pk.Equals(new PartitionKey(order.Id))),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse.Object);

        // Act
        await _sut.UpsertAsync(order);

        // Assert
        _mockContainer.Verify(
            c => c.UpsertItemAsync(
                order,
                It.Is<PartitionKey>(pk => pk.Equals(new PartitionKey("order-upsert-1"))),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAsync_WhenOrderExists_ReturnsOrder()
    {
        // Arrange
        var orderId = "order-get-1";
        var expectedOrder = new Order
        {
            Id = orderId,
            CustomerId = "cust-get",
            Status = OrderStatus.Completed
        };

        var mockResponse = new Mock<ItemResponse<Order>>();
        mockResponse.Setup(r => r.Resource).Returns(expectedOrder);
        mockResponse.Setup(r => r.RequestCharge).Returns(1.0);

        _mockContainer
            .Setup(c => c.ReadItemAsync<Order>(
                orderId,
                It.Is<PartitionKey>(pk => pk.Equals(new PartitionKey(orderId))),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse.Object);

        // Act
        var result = await _sut.GetAsync(orderId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(orderId);
        result.CustomerId.Should().Be("cust-get");
    }

    [Fact]
    public async Task GetAsync_WhenOrderNotFound_ReturnsNull()
    {
        // Arrange
        var orderId = "order-not-found";

        _mockContainer
            .Setup(c => c.ReadItemAsync<Order>(
                orderId,
                It.Is<PartitionKey>(pk => pk.Equals(new PartitionKey(orderId))),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CosmosException("Not found", HttpStatusCode.NotFound, 0, "", 1.0));

        // Act
        var result = await _sut.GetAsync(orderId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_WithNullOrderId_ThrowsArgumentException()
    {
        // Act
        var act = () => _sut.GetAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetAsync_WithEmptyOrderId_ThrowsArgumentException()
    {
        // Act
        var act = () => _sut.GetAsync("   ");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetByCustomerAsync_QueriesWithCorrectPartitionKey()
    {
        // Arrange
        var customerId = "cust-query";
        var orders = new List<Order>
        {
            new() { Id = "o1", CustomerId = customerId, Status = OrderStatus.Completed },
            new() { Id = "o2", CustomerId = customerId, Status = OrderStatus.Pending }
        };

        var mockFeedResponse = new Mock<FeedResponse<Order>>();
        mockFeedResponse.Setup(r => r.Count).Returns(orders.Count);
        mockFeedResponse.Setup(r => r.RequestCharge).Returns(3.5);
        mockFeedResponse.Setup(r => r.GetEnumerator()).Returns(orders.GetEnumerator());

        var mockIterator = new Mock<FeedIterator<Order>>();
        mockIterator.SetupSequence(i => i.HasMoreResults)
            .Returns(true)
            .Returns(false);
        mockIterator
            .Setup(i => i.ReadNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockFeedResponse.Object);

        _mockContainer
            .Setup(c => c.GetItemQueryIterator<Order>(
                It.Is<QueryDefinition>(q => q.QueryText.Contains("customerId")),
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>()))
            .Returns(mockIterator.Object);

        // Act
        var results = await _sut.GetByCustomerAsync(customerId);

        // Assert
        results.Should().HaveCount(2);
        results.Should().AllSatisfy(o => o.CustomerId.Should().Be(customerId));
    }

    [Fact]
    public async Task GetByCustomerAsync_WithNullCustomerId_ThrowsArgumentException()
    {
        // Act
        var act = () => _sut.GetByCustomerAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UpsertAsync_WithNullOrder_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _sut.UpsertAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullContainer_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new CosmosDbOrderRepository(null!, _mockLogger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("container");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new CosmosDbOrderRepository(_mockContainer.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }
}
