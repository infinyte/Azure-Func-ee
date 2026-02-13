using Azure.Messaging.EventGrid;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Scenario02.RealtimeNotifications.Functions;
using Scenario02.RealtimeNotifications.Models.Dtos;
using Scenario02.RealtimeNotifications.Services;
using Scenario02.RealtimeNotifications.Tests.TestHelpers;
using Xunit;

namespace Scenario02.RealtimeNotifications.Tests.Functions;

public class HandleSystemEventFunctionTests
{
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly HandleSystemEventFunction _function;

    public HandleSystemEventFunctionTests()
    {
        _mockNotificationService = new Mock<INotificationService>();
        var mockLogger = new Mock<ILogger<HandleSystemEventFunction>>();
        _function = new HandleSystemEventFunction(_mockNotificationService.Object, mockLogger.Object);

        _mockNotificationService
            .Setup(s => s.CreateAsync(It.IsAny<SendNotificationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationTestDataBuilder().Build());
    }

    [Fact]
    public async Task RunAsync_ValidEventWithUserSubject_CreatesNotification()
    {
        // Arrange
        var eventGridEvent = new EventGridEvent(
            "users/user-001/orders/order-123",
            "OrderCreated",
            "1.0",
            new BinaryData("{}"));

        // Act
        await _function.RunAsync(eventGridEvent);

        // Assert
        _mockNotificationService.Verify(
            s => s.CreateAsync(
                It.Is<SendNotificationRequest>(r =>
                    r.UserId == "user-001" &&
                    r.Category == "System"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_EventWithoutUserSubject_DoesNotCreateNotification()
    {
        // Arrange
        var eventGridEvent = new EventGridEvent(
            "system/health/check",
            "HealthCheck",
            "1.0",
            new BinaryData("{}"));

        // Act
        await _function.RunAsync(eventGridEvent);

        // Assert
        _mockNotificationService.Verify(
            s => s.CreateAsync(It.IsAny<SendNotificationRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData("users/user-001/orders", "user-001")]
    [InlineData("/users/abc123/events/test", "abc123")]
    [InlineData("system/health", null)]
    [InlineData("", null)]
    public void ExtractUserId_VariousSubjects_ReturnsExpected(string subject, string? expected)
    {
        var result = HandleSystemEventFunction.ExtractUserId(subject);
        result.Should().Be(expected);
    }
}
