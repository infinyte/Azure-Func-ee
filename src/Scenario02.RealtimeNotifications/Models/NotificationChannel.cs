namespace Scenario02.RealtimeNotifications.Models;

/// <summary>
/// The delivery channel for a notification.
/// </summary>
public enum NotificationChannel
{
    /// <summary>In-app real-time notification via SignalR.</summary>
    InApp = 0,

    /// <summary>Email notification.</summary>
    Email = 1,

    /// <summary>Push notification.</summary>
    Push = 2
}
