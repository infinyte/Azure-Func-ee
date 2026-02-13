using Microsoft.Extensions.Logging;
using Scenario02.RealtimeNotifications.Models;
using Scenario02.RealtimeNotifications.Models.Dtos;
using Scenario02.RealtimeNotifications.Repositories;

namespace Scenario02.RealtimeNotifications.Services;

/// <summary>
/// Default implementation of <see cref="INotificationService"/>.
/// </summary>
public sealed class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<NotificationService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="NotificationService"/>.
    /// </summary>
    /// <param name="notificationRepository">The notification repository.</param>
    /// <param name="logger">The logger instance.</param>
    public NotificationService(
        INotificationRepository notificationRepository,
        ILogger<NotificationService> logger)
    {
        _notificationRepository = notificationRepository ?? throw new ArgumentNullException(nameof(notificationRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Notification> CreateAsync(SendNotificationRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var notificationId = Guid.NewGuid().ToString();
        var channel = Enum.TryParse<NotificationChannel>(request.Channel, ignoreCase: true, out var parsed)
            ? parsed
            : NotificationChannel.InApp;

        var notification = new Notification
        {
            PartitionKey = request.UserId,
            RowKey = notificationId,
            NotificationId = notificationId,
            UserId = request.UserId,
            Title = request.Title,
            Body = request.Body,
            Channel = (int)channel,
            Status = (int)NotificationStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            Category = request.Category
        };

        await _notificationRepository.UpsertAsync(notification, ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Created notification {NotificationId} for user {UserId} via {Channel}",
            notificationId, request.UserId, channel);

        return notification;
    }

    /// <inheritdoc />
    public async Task UpdateStatusAsync(string userId, string notificationId, NotificationStatus status, CancellationToken ct)
    {
        var notification = await _notificationRepository.GetAsync(userId, notificationId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Notification '{notificationId}' not found for user '{userId}'.");

        notification.Status = (int)status;

        if (status == NotificationStatus.Delivered)
        {
            notification.DeliveredAt = DateTimeOffset.UtcNow;
        }

        await _notificationRepository.UpsertAsync(notification, ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Updated notification {NotificationId} status to {Status}",
            notificationId, status);
    }

    /// <inheritdoc />
    public async Task MarkAsReadAsync(string userId, string notificationId, CancellationToken ct)
    {
        var notification = await _notificationRepository.GetAsync(userId, notificationId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Notification '{notificationId}' not found for user '{userId}'.");

        notification.Status = (int)NotificationStatus.Read;
        notification.ReadAt = DateTimeOffset.UtcNow;

        await _notificationRepository.UpsertAsync(notification, ct).ConfigureAwait(false);

        _logger.LogInformation("Marked notification {NotificationId} as read", notificationId);
    }

    /// <inheritdoc />
    public async Task<DigestSummary> GenerateDigestAsync(string userId, CancellationToken ct)
    {
        var unread = await _notificationRepository.GetUnreadByUserAsync(userId, ct).ConfigureAwait(false);

        var categories = unread
            .GroupBy(n => n.Category)
            .ToDictionary(g => g.Key, g => g.Count());

        _logger.LogInformation(
            "Generated digest for user {UserId}: {Count} unread notifications",
            userId, unread.Count);

        return new DigestSummary(
            userId,
            unread.Count,
            categories,
            DateTimeOffset.UtcNow);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Notification>> GetUnreadAsync(string userId, CancellationToken ct)
    {
        return await _notificationRepository.GetUnreadByUserAsync(userId, ct).ConfigureAwait(false);
    }
}
