namespace Scenario02.RealtimeNotifications.Models.Dtos;

/// <summary>
/// Summary of a daily notification digest for a user.
/// </summary>
/// <param name="UserId">The user ID.</param>
/// <param name="NotificationCount">Number of unread notifications.</param>
/// <param name="Categories">Notification counts by category.</param>
/// <param name="GeneratedAt">When the digest was generated.</param>
public sealed record DigestSummary(
    string UserId,
    int NotificationCount,
    IReadOnlyDictionary<string, int> Categories,
    DateTimeOffset GeneratedAt);
