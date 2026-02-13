using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Scenario02.RealtimeNotifications.Services;

namespace Scenario02.RealtimeNotifications.Functions;

/// <summary>
/// Timer-triggered function that sends daily notification digests.
/// Runs at 8:00 AM UTC (configurable via cron expression).
/// </summary>
public sealed class SendDigestFunction
{
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;
    private readonly ILogger<SendDigestFunction> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="SendDigestFunction"/>.
    /// </summary>
    /// <param name="notificationService">The notification service.</param>
    /// <param name="emailService">The email service.</param>
    /// <param name="logger">The logger instance.</param>
    public SendDigestFunction(
        INotificationService notificationService,
        IEmailService emailService,
        ILogger<SendDigestFunction> logger)
    {
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generates and sends daily digest emails for users with unread notifications.
    /// This is a demonstration placeholder; in production it would iterate over all subscribed users.
    /// </summary>
    /// <param name="timerInfo">The timer trigger information.</param>
    [Function("SendDigest")]
    public Task RunAsync(
        [TimerTrigger("0 0 8 * * *")] TimerInfo timerInfo)
    {
        _logger.LogInformation("Daily digest timer fired at {Time}", DateTimeOffset.UtcNow);

        // In production, this would:
        // 1. Query all users with digest subscriptions enabled
        // 2. For each user, call _notificationService.GenerateDigestAsync
        // 3. Send the digest via _emailService

        _logger.LogInformation("Digest processing complete");

        return Task.CompletedTask;
    }
}
