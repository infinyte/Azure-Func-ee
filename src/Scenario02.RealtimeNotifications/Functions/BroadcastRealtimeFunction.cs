using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Scenario02.RealtimeNotifications.Models;
using Scenario02.RealtimeNotifications.Services;

namespace Scenario02.RealtimeNotifications.Functions;

/// <summary>
/// Queue-triggered function that broadcasts real-time notifications via SignalR.
/// Reads from the signalr-broadcast queue and sends messages to the target user.
/// </summary>
public sealed class BroadcastRealtimeFunction
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<BroadcastRealtimeFunction> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Initializes a new instance of <see cref="BroadcastRealtimeFunction"/>.
    /// </summary>
    /// <param name="notificationService">The notification service.</param>
    /// <param name="logger">The logger instance.</param>
    public BroadcastRealtimeFunction(
        INotificationService notificationService,
        ILogger<BroadcastRealtimeFunction> logger)
    {
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Broadcasts a notification to a specific user via SignalR.
    /// </summary>
    /// <param name="message">The queue message containing notification details.</param>
    /// <returns>The SignalR message action for the output binding.</returns>
    [Function("BroadcastRealtime")]
    [SignalROutput(HubName = "notifications", ConnectionStringSetting = "AzureSignalRConnectionString")]
    public async Task<SignalRMessageAction> RunAsync(
        [QueueTrigger("signalr-broadcast", Connection = "AzureWebJobsStorage")] string message)
    {
        _logger.LogInformation("Broadcasting real-time notification via SignalR");

        var payload = JsonSerializer.Deserialize<BroadcastPayload>(message, JsonOptions);

        if (payload is null)
        {
            _logger.LogWarning("Failed to deserialize broadcast payload");
            return new SignalRMessageAction("notification") { Arguments = new object[] { "Invalid payload" } };
        }

        await _notificationService.UpdateStatusAsync(
            payload.UserId,
            payload.NotificationId,
            NotificationStatus.Delivered).ConfigureAwait(false);

        _logger.LogInformation(
            "Broadcasting notification {NotificationId} to user {UserId}",
            payload.NotificationId, payload.UserId);

        return new SignalRMessageAction("notification")
        {
            UserId = payload.UserId,
            Arguments = new object[]
            {
                new
                {
                    payload.NotificationId,
                    payload.Title,
                    payload.Body,
                    Timestamp = DateTimeOffset.UtcNow
                }
            }
        };
    }

    private sealed record BroadcastPayload(
        string NotificationId,
        string UserId,
        int Channel,
        string Title,
        string Body);
}
