using Microsoft.Extensions.Logging;
using Scenario02.RealtimeNotifications.Models;

namespace Scenario02.RealtimeNotifications.Services;

/// <summary>
/// Simulated email service that logs delivery instead of sending actual emails.
/// In production, this would integrate with SendGrid, Azure Communication Services, or similar.
/// </summary>
public sealed class SimulatedEmailService : IEmailService
{
    private readonly ILogger<SimulatedEmailService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="SimulatedEmailService"/>.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public SimulatedEmailService(ILogger<SimulatedEmailService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task<DeliveryResult> SendAsync(Notification notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);

        _logger.LogInformation(
            "Simulated email sent to user {UserId}: [{Title}] {Body}",
            notification.UserId, notification.Title, notification.Body);

        return Task.FromResult(new DeliveryResult(true, NotificationChannel.Email));
    }
}
