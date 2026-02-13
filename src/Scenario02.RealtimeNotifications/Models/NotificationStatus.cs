namespace Scenario02.RealtimeNotifications.Models;

/// <summary>
/// The delivery status of a notification.
/// </summary>
public enum NotificationStatus
{
    /// <summary>Notification has been created but not yet delivered.</summary>
    Pending = 0,

    /// <summary>Notification has been queued for delivery.</summary>
    Queued = 1,

    /// <summary>Notification has been delivered to the recipient.</summary>
    Delivered = 2,

    /// <summary>Notification has been read by the recipient.</summary>
    Read = 3,

    /// <summary>Notification delivery failed.</summary>
    Failed = 4
}
