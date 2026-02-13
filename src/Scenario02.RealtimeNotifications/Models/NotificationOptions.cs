namespace Scenario02.RealtimeNotifications.Models;

/// <summary>
/// Configuration options for the notification system.
/// Bound from the "Notifications" configuration section.
/// </summary>
public sealed class NotificationOptions
{
    /// <summary>
    /// The configuration section name used to bind these options.
    /// </summary>
    public const string SectionName = "Notifications";

    /// <summary>
    /// The connection name for Azure Storage.
    /// </summary>
    public string StorageConnectionName { get; set; } = "AzureWebJobsStorage";

    /// <summary>
    /// The table name for notification records.
    /// </summary>
    public string NotificationsTable { get; set; } = "notifications";

    /// <summary>
    /// The table name for user subscription records.
    /// </summary>
    public string SubscriptionsTable { get; set; } = "subscriptions";

    /// <summary>
    /// The queue name for notification delivery processing.
    /// </summary>
    public string DeliveryQueue { get; set; } = "notification-delivery";

    /// <summary>
    /// The queue name for SignalR broadcast messages.
    /// </summary>
    public string SignalRBroadcastQueue { get; set; } = "signalr-broadcast";

    /// <summary>
    /// The SignalR hub name.
    /// </summary>
    public string SignalRHubName { get; set; } = "notifications";

    /// <summary>
    /// The digest email hour (UTC, 24-hour format).
    /// </summary>
    public int DigestHourUtc { get; set; } = 8;
}
