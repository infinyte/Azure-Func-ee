using Scenario02.RealtimeNotifications.Models;

namespace Scenario02.RealtimeNotifications.Services;

/// <summary>
/// Service for sending email notifications.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email notification.
    /// </summary>
    /// <param name="notification">The notification to send.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The delivery result.</returns>
    Task<DeliveryResult> SendAsync(Notification notification, CancellationToken ct = default);
}
