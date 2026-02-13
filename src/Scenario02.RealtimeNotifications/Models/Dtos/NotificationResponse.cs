namespace Scenario02.RealtimeNotifications.Models.Dtos;

/// <summary>
/// Response DTO representing a notification.
/// </summary>
public sealed record NotificationResponse
{
    /// <summary>
    /// The notification identifier.
    /// </summary>
    public string NotificationId { get; init; } = string.Empty;

    /// <summary>
    /// The target user ID.
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// The notification title.
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// The notification body.
    /// </summary>
    public string Body { get; init; } = string.Empty;

    /// <summary>
    /// The delivery channel.
    /// </summary>
    public string Channel { get; init; } = string.Empty;

    /// <summary>
    /// The delivery status.
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// The creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// The notification category.
    /// </summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>
    /// Creates a response from a <see cref="Notification"/> entity.
    /// </summary>
    /// <param name="notification">The notification entity.</param>
    /// <returns>A new response DTO.</returns>
    public static NotificationResponse FromNotification(Notification notification) => new()
    {
        NotificationId = notification.NotificationId,
        UserId = notification.UserId,
        Title = notification.Title,
        Body = notification.Body,
        Channel = ((NotificationChannel)notification.Channel).ToString(),
        Status = ((NotificationStatus)notification.Status).ToString(),
        CreatedAt = notification.CreatedAt,
        Category = notification.Category
    };
}
