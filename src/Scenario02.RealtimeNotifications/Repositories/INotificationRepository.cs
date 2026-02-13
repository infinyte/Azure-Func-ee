using Scenario02.RealtimeNotifications.Models;

namespace Scenario02.RealtimeNotifications.Repositories;

/// <summary>
/// Repository for persisting and retrieving notification records.
/// </summary>
public interface INotificationRepository
{
    /// <summary>
    /// Gets a notification by user ID and notification ID.
    /// </summary>
    /// <param name="userId">The user ID (partition key).</param>
    /// <param name="notificationId">The notification ID (row key).</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The notification, or null if not found.</returns>
    Task<Notification?> GetAsync(string userId, string notificationId, CancellationToken ct = default);

    /// <summary>
    /// Gets all notifications for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A read-only list of notifications.</returns>
    Task<IReadOnlyList<Notification>> GetByUserAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Gets unread notifications for a user (status is Pending or Delivered).
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A read-only list of unread notifications.</returns>
    Task<IReadOnlyList<Notification>> GetUnreadByUserAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Creates or updates a notification record.
    /// </summary>
    /// <param name="notification">The notification entity.</param>
    /// <param name="ct">A cancellation token.</param>
    Task UpsertAsync(Notification notification, CancellationToken ct = default);
}
