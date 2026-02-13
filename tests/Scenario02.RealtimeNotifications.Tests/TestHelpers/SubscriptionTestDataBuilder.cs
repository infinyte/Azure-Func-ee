using Scenario02.RealtimeNotifications.Models;

namespace Scenario02.RealtimeNotifications.Tests.TestHelpers;

/// <summary>
/// Test data builder for creating <see cref="UserSubscription"/> instances in tests.
/// </summary>
internal sealed class SubscriptionTestDataBuilder
{
    private string _userId = "user-001";
    private NotificationChannel _channel = NotificationChannel.InApp;
    private bool _isEnabled = true;
    private bool _includeInDigest = true;

    public SubscriptionTestDataBuilder WithUserId(string userId)
    {
        _userId = userId;
        return this;
    }

    public SubscriptionTestDataBuilder WithChannel(NotificationChannel channel)
    {
        _channel = channel;
        return this;
    }

    public SubscriptionTestDataBuilder WithEnabled(bool enabled)
    {
        _isEnabled = enabled;
        return this;
    }

    public SubscriptionTestDataBuilder WithDigest(bool includeInDigest)
    {
        _includeInDigest = includeInDigest;
        return this;
    }

    public UserSubscription Build() => new()
    {
        PartitionKey = _userId,
        RowKey = _channel.ToString(),
        UserId = _userId,
        Channel = (int)_channel,
        IsEnabled = _isEnabled,
        IncludeInDigest = _includeInDigest
    };
}
