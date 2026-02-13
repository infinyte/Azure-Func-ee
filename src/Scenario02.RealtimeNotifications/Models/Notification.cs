using Azure;
using Azure.Data.Tables;

namespace Scenario02.RealtimeNotifications.Models;

/// <summary>
/// Represents a notification stored in Azure Table Storage.
/// Uses the user ID as the partition key and notification ID as the row key.
/// </summary>
public sealed class Notification : ITableEntity
{
    /// <inheritdoc />
    public string PartitionKey { get; set; } = string.Empty;

    /// <inheritdoc />
    public string RowKey { get; set; } = string.Empty;

    /// <inheritdoc />
    public DateTimeOffset? Timestamp { get; set; }

    /// <inheritdoc />
    public ETag ETag { get; set; }

    /// <summary>
    /// The unique identifier for this notification.
    /// </summary>
    public string NotificationId { get; set; } = string.Empty;

    /// <summary>
    /// The user ID this notification is addressed to.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// The notification title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The notification body/message.
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// The delivery channel for this notification.
    /// </summary>
    public int Channel { get; set; } = (int)NotificationChannel.InApp;

    /// <summary>
    /// The current delivery status.
    /// </summary>
    public int Status { get; set; } = (int)NotificationStatus.Pending;

    /// <summary>
    /// The date and time the notification was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// The date and time the notification was delivered, if applicable.
    /// </summary>
    public DateTimeOffset? DeliveredAt { get; set; }

    /// <summary>
    /// The date and time the notification was read, if applicable.
    /// </summary>
    public DateTimeOffset? ReadAt { get; set; }

    /// <summary>
    /// The notification category (e.g., "System", "Order", "Alert").
    /// </summary>
    public string Category { get; set; } = "General";
}
