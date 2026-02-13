using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Scenario02.RealtimeNotifications.Functions;
using Scenario02.RealtimeNotifications.Models;
using Scenario02.RealtimeNotifications.Services;
using Xunit;

namespace Scenario02.RealtimeNotifications.Tests.Functions;

public class ProcessNotificationFunctionTests
{
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly ProcessNotificationFunction _function;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ProcessNotificationFunctionTests()
    {
        _mockNotificationService = new Mock<INotificationService>();
        _mockEmailService = new Mock<IEmailService>();
        var mockLogger = new Mock<ILogger<ProcessNotificationFunction>>();
        _function = new ProcessNotificationFunction(
            _mockNotificationService.Object,
            _mockEmailService.Object,
            mockLogger.Object);
    }

    [Fact]
    public async Task RunAsync_InAppChannel_RoutesToSignalRBroadcastQueue()
    {
        // Arrange
        var payload = new { notificationId = "notif-001", userId = "user-001", channel = 0, title = "Test", body = "Hello" };
        var message = JsonSerializer.Serialize(payload, JsonOptions);

        // Act
        var output = await _function.RunAsync(message);

        // Assert
        output.SignalRBroadcastMessage.Should().NotBeNullOrWhiteSpace();
        _mockEmailService.Verify(
            s => s.SendAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RunAsync_EmailChannel_SendsEmail()
    {
        // Arrange
        _mockEmailService
            .Setup(s => s.SendAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeliveryResult(true, NotificationChannel.Email));

        var payload = new { notificationId = "notif-002", userId = "user-001", channel = 1, title = "Test", body = "Hello" };
        var message = JsonSerializer.Serialize(payload, JsonOptions);

        // Act
        var output = await _function.RunAsync(message);

        // Assert
        output.SignalRBroadcastMessage.Should().BeNull();
        _mockEmailService.Verify(
            s => s.SendAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _mockNotificationService.Verify(
            s => s.UpdateStatusAsync("user-001", "notif-002", NotificationStatus.Delivered, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_InvalidPayload_ReturnsEmptyOutput()
    {
        // Act
        var output = await _function.RunAsync("invalid json {{{");

        // Assert
        output.SignalRBroadcastMessage.Should().BeNull();
    }
}
