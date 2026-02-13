using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Scenario02.RealtimeNotifications.Models;
using Scenario02.RealtimeNotifications.Models.Dtos;
using Scenario02.RealtimeNotifications.Repositories;
using Scenario02.RealtimeNotifications.Services;
using Scenario02.RealtimeNotifications.Tests.TestHelpers;
using Xunit;

namespace Scenario02.RealtimeNotifications.Tests.Services;

public class NotificationServiceTests
{
    private readonly Mock<INotificationRepository> _mockRepo;
    private readonly NotificationService _service;

    public NotificationServiceTests()
    {
        _mockRepo = new Mock<INotificationRepository>();
        var mockLogger = new Mock<ILogger<NotificationService>>();
        _service = new NotificationService(_mockRepo.Object, mockLogger.Object);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesAndPersistsNotification()
    {
        // Arrange
        var request = new SendNotificationRequest("user-001", "Title", "Body", "InApp", "General");

        // Act
        var result = await _service.CreateAsync(request, CancellationToken.None);

        // Assert
        result.NotificationId.Should().NotBeNullOrWhiteSpace();
        result.UserId.Should().Be("user-001");
        result.Title.Should().Be("Title");
        result.Status.Should().Be((int)NotificationStatus.Pending);
        _mockRepo.Verify(r => r.UpsertAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_EmailChannel_SetsEmailChannel()
    {
        // Arrange
        var request = new SendNotificationRequest("user-001", "Title", "Body", "Email");

        // Act
        var result = await _service.CreateAsync(request, CancellationToken.None);

        // Assert
        result.Channel.Should().Be((int)NotificationChannel.Email);
    }

    [Fact]
    public async Task UpdateStatusAsync_ExistingNotification_UpdatesStatus()
    {
        // Arrange
        var notification = new NotificationTestDataBuilder()
            .WithUserId("user-001")
            .WithNotificationId("notif-001")
            .Build();

        _mockRepo.Setup(r => r.GetAsync("user-001", "notif-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);

        // Act
        await _service.UpdateStatusAsync("user-001", "notif-001", NotificationStatus.Delivered, CancellationToken.None);

        // Assert
        _mockRepo.Verify(r => r.UpsertAsync(
            It.Is<Notification>(n =>
                n.Status == (int)NotificationStatus.Delivered &&
                n.DeliveredAt != null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkAsReadAsync_ExistingNotification_SetsReadStatus()
    {
        // Arrange
        var notification = new NotificationTestDataBuilder()
            .WithUserId("user-001")
            .WithNotificationId("notif-001")
            .WithStatus(NotificationStatus.Delivered)
            .Build();

        _mockRepo.Setup(r => r.GetAsync("user-001", "notif-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);

        // Act
        await _service.MarkAsReadAsync("user-001", "notif-001", CancellationToken.None);

        // Assert
        _mockRepo.Verify(r => r.UpsertAsync(
            It.Is<Notification>(n =>
                n.Status == (int)NotificationStatus.Read &&
                n.ReadAt != null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateDigestAsync_WithUnreadNotifications_ReturnsSummary()
    {
        // Arrange
        var notifications = new List<Notification>
        {
            new NotificationTestDataBuilder().WithCategory("Orders").Build(),
            new NotificationTestDataBuilder().WithCategory("Orders").Build(),
            new NotificationTestDataBuilder().WithCategory("System").Build()
        };

        _mockRepo.Setup(r => r.GetUnreadByUserAsync("user-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(notifications.AsReadOnly());

        // Act
        var digest = await _service.GenerateDigestAsync("user-001", CancellationToken.None);

        // Assert
        digest.UserId.Should().Be("user-001");
        digest.NotificationCount.Should().Be(3);
        digest.Categories.Should().ContainKey("Orders").WhoseValue.Should().Be(2);
        digest.Categories.Should().ContainKey("System").WhoseValue.Should().Be(1);
    }

    [Fact]
    public async Task UpdateStatusAsync_NonExistentNotification_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetAsync("user-001", "missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Notification?)null);

        // Act
        var act = () => _service.UpdateStatusAsync("user-001", "missing", NotificationStatus.Delivered, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Notification 'missing' not found*");
    }
}
