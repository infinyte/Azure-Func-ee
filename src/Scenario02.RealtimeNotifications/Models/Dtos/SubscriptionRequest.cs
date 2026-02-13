namespace Scenario02.RealtimeNotifications.Models.Dtos;

/// <summary>
/// Request DTO for updating a user's notification channel subscription.
/// </summary>
/// <param name="Channel">The notification channel name.</param>
/// <param name="IsEnabled">Whether the channel should be enabled.</param>
/// <param name="IncludeInDigest">Whether to include this channel in daily digests.</param>
public sealed record SubscriptionRequest(
    string Channel,
    bool IsEnabled,
    bool IncludeInDigest = true);
