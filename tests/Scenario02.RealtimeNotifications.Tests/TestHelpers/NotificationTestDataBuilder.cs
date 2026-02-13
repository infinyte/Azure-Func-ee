using Scenario02.RealtimeNotifications.Models;

namespace Scenario02.RealtimeNotifications.Tests.TestHelpers;

/// <summary>
/// Test data builder for creating <see cref="Notification"/> instances in tests.
/// </summary>
internal sealed class NotificationTestDataBuilder
{
    private string _userId = "user-001";
    private string _notificationId = Guid.NewGuid().ToString();
    private string _title = "Test Notification";
    private string _body = "This is a test notification body.";
    private NotificationChannel _channel = NotificationChannel.InApp;
    private NotificationStatus _status = NotificationStatus.Pending;
    private string _category = "General";

    public NotificationTestDataBuilder WithUserId(string userId)
    {
        _userId = userId;
        return this;
    }

    public NotificationTestDataBuilder WithNotificationId(string notificationId)
    {
        _notificationId = notificationId;
        return this;
    }

    public NotificationTestDataBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public NotificationTestDataBuilder WithBody(string body)
    {
        _body = body;
        return this;
    }

    public NotificationTestDataBuilder WithChannel(NotificationChannel channel)
    {
        _channel = channel;
        return this;
    }

    public NotificationTestDataBuilder WithStatus(NotificationStatus status)
    {
        _status = status;
        return this;
    }

    public NotificationTestDataBuilder WithCategory(string category)
    {
        _category = category;
        return this;
    }

    public Notification Build() => new()
    {
        PartitionKey = _userId,
        RowKey = _notificationId,
        NotificationId = _notificationId,
        UserId = _userId,
        Title = _title,
        Body = _body,
        Channel = (int)_channel,
        Status = (int)_status,
        CreatedAt = DateTimeOffset.UtcNow,
        Category = _category
    };
}
