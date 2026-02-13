using Scenario02.RealtimeNotifications.Models;
using Scenario02.RealtimeNotifications.Models.Dtos;

namespace Scenario02.RealtimeNotifications.Services;

/// <summary>
/// Service for managing notification lifecycle including creation, delivery, and digest generation.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Creates a new notification and persists it.
    /// </summary>
    /// <param name="request">The send notification request.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The created notification.</returns>
    Task<Notification> CreateAsync(SendNotificationRequest request, CancellationToken ct = default);

    /// <summary>
    /// Updates the delivery status of a notification.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="notificationId">The notification ID.</param>
    /// <param name="status">The new status.</param>
    /// <param name="ct">A cancellation token.</param>
    Task UpdateStatusAsync(string userId, string notificationId, NotificationStatus status, CancellationToken ct = default);

    /// <summary>
    /// Marks a notification as read.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="notificationId">The notification ID.</param>
    /// <param name="ct">A cancellation token.</param>
    Task MarkAsReadAsync(string userId, string notificationId, CancellationToken ct = default);

    /// <summary>
    /// Generates a daily digest summary for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The digest summary.</returns>
    Task<DigestSummary> GenerateDigestAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Gets all unread notifications for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A read-only list of unread notifications.</returns>
    Task<IReadOnlyList<Notification>> GetUnreadAsync(string userId, CancellationToken ct = default);
}
