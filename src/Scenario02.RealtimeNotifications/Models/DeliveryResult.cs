namespace Scenario02.RealtimeNotifications.Models;

/// <summary>
/// The result of delivering a notification through a channel.
/// </summary>
/// <param name="IsSuccess">Whether the delivery was successful.</param>
/// <param name="Channel">The delivery channel used.</param>
/// <param name="ErrorMessage">An error message if delivery failed.</param>
public sealed record DeliveryResult(
    bool IsSuccess,
    NotificationChannel Channel,
    string? ErrorMessage = null);
