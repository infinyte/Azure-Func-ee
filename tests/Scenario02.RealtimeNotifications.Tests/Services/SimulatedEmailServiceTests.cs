using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Scenario02.RealtimeNotifications.Models;
using Scenario02.RealtimeNotifications.Services;
using Scenario02.RealtimeNotifications.Tests.TestHelpers;
using Xunit;

namespace Scenario02.RealtimeNotifications.Tests.Services;

public class SimulatedEmailServiceTests
{
    private readonly SimulatedEmailService _service;

    public SimulatedEmailServiceTests()
    {
        var mockLogger = new Mock<ILogger<SimulatedEmailService>>();
        _service = new SimulatedEmailService(mockLogger.Object);
    }

    [Fact]
    public async Task SendAsync_ValidNotification_ReturnsSuccess()
    {
        // Arrange
        var notification = new NotificationTestDataBuilder()
            .WithChannel(NotificationChannel.Email)
            .Build();

        // Act
        var result = await _service.SendAsync(notification, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Channel.Should().Be(NotificationChannel.Email);
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task SendAsync_NullNotification_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _service.SendAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
