namespace Scenario02.RealtimeNotifications.Models.Dtos;

/// <summary>
/// Response DTO representing a user's channel subscription.
/// </summary>
public sealed record SubscriptionResponse
{
    /// <summary>
    /// The user ID.
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// The notification channel.
    /// </summary>
    public string Channel { get; init; } = string.Empty;

    /// <summary>
    /// Whether the channel is enabled.
    /// </summary>
    public bool IsEnabled { get; init; }

    /// <summary>
    /// Whether the channel is included in daily digests.
    /// </summary>
    public bool IncludeInDigest { get; init; }

    /// <summary>
    /// Creates a response from a <see cref="UserSubscription"/> entity.
    /// </summary>
    /// <param name="subscription">The subscription entity.</param>
    /// <returns>A new response DTO.</returns>
    public static SubscriptionResponse FromSubscription(UserSubscription subscription) => new()
    {
        UserId = subscription.UserId,
        Channel = ((NotificationChannel)subscription.Channel).ToString(),
        IsEnabled = subscription.IsEnabled,
        IncludeInDigest = subscription.IncludeInDigest
    };
}
