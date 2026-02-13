using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Scenario02.RealtimeNotifications.Models;
using Scenario02.RealtimeNotifications.Services;

namespace Scenario02.RealtimeNotifications.Functions;

/// <summary>
/// Queue-triggered function that processes notification delivery.
/// Routes notifications to the appropriate channel: InApp to the SignalR broadcast queue,
/// Email to the email service.
/// </summary>
public sealed class ProcessNotificationFunction
{
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;
    private readonly ILogger<ProcessNotificationFunction> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Initializes a new instance of <see cref="ProcessNotificationFunction"/>.
    /// </summary>
    /// <param name="notificationService">The notification service.</param>
    /// <param name="emailService">The email service.</param>
    /// <param name="logger">The logger instance.</param>
    public ProcessNotificationFunction(
        INotificationService notificationService,
        IEmailService emailService,
        ILogger<ProcessNotificationFunction> logger)
    {
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Processes a notification delivery message by routing to the appropriate channel.
    /// </summary>
    /// <param name="message">The queue message containing notification details.</param>
    /// <returns>The output binding containing an optional SignalR broadcast queue message.</returns>
    [Function("ProcessNotification")]
    public async Task<ProcessNotificationOutput> RunAsync(
        [QueueTrigger("notification-delivery", Connection = "AzureWebJobsStorage")] string message)
    {
        _logger.LogInformation("Processing notification delivery message");

        NotificationPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<NotificationPayload>(message, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize notification payload");
            return new ProcessNotificationOutput();
        }

        if (payload is null)
        {
            _logger.LogWarning("Failed to deserialize notification payload");
            return new ProcessNotificationOutput();
        }

        var channel = (NotificationChannel)payload.Channel;

        _logger.LogInformation(
            "Routing notification {NotificationId} for user {UserId} via {Channel}",
            payload.NotificationId, payload.UserId, channel);

        switch (channel)
        {
            case NotificationChannel.InApp:
                // Route to SignalR broadcast queue.
                var signalRMessage = JsonSerializer.Serialize(payload, JsonOptions);
                return new ProcessNotificationOutput { SignalRBroadcastMessage = signalRMessage };

            case NotificationChannel.Email:
                var notification = new Notification
                {
                    NotificationId = payload.NotificationId,
                    UserId = payload.UserId,
                    Title = payload.Title,
                    Body = payload.Body,
                    Channel = payload.Channel
                };

                var result = await _emailService.SendAsync(notification).ConfigureAwait(false);

                if (result.IsSuccess)
                {
                    await _notificationService.UpdateStatusAsync(
                        payload.UserId, payload.NotificationId, NotificationStatus.Delivered).ConfigureAwait(false);
                }

                return new ProcessNotificationOutput();

            default:
                _logger.LogWarning("Unsupported channel {Channel} for notification {NotificationId}",
                    channel, payload.NotificationId);
                return new ProcessNotificationOutput();
        }
    }
}

/// <summary>
/// Multi-output binding for ProcessNotification.
/// </summary>
public sealed class ProcessNotificationOutput
{
    /// <summary>
    /// Optional queue message to forward to the SignalR broadcast queue.
    /// </summary>
    [QueueOutput("signalr-broadcast", Connection = "AzureWebJobsStorage")]
    public string? SignalRBroadcastMessage { get; set; }
}

/// <summary>
/// Internal DTO for notification delivery payload.
/// </summary>
internal sealed record NotificationPayload(
    string NotificationId,
    string UserId,
    int Channel,
    string Title,
    string Body);
