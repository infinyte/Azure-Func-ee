using System.Text.Json;
using Azure.Messaging.EventGrid;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Scenario02.RealtimeNotifications.Models.Dtos;
using Scenario02.RealtimeNotifications.Services;

namespace Scenario02.RealtimeNotifications.Functions;

/// <summary>
/// Event Grid-triggered function that converts system events into notifications.
/// </summary>
public sealed class HandleSystemEventFunction
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<HandleSystemEventFunction> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="HandleSystemEventFunction"/>.
    /// </summary>
    /// <param name="notificationService">The notification service.</param>
    /// <param name="logger">The logger instance.</param>
    public HandleSystemEventFunction(
        INotificationService notificationService,
        ILogger<HandleSystemEventFunction> logger)
    {
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles an Event Grid event by creating a notification for the affected user.
    /// </summary>
    /// <param name="eventGridEvent">The Event Grid event.</param>
    [Function("HandleSystemEvent")]
    public async Task RunAsync(
        [EventGridTrigger] EventGridEvent eventGridEvent)
    {
        _logger.LogInformation(
            "Received Event Grid event: {EventType} Subject: {Subject}",
            eventGridEvent.EventType, eventGridEvent.Subject);

        // Extract user ID from the event subject (convention: "users/{userId}/...")
        var userId = ExtractUserId(eventGridEvent.Subject);

        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("Could not extract user ID from event subject: {Subject}", eventGridEvent.Subject);
            return;
        }

        var request = new SendNotificationRequest(
            UserId: userId,
            Title: $"System Event: {eventGridEvent.EventType}",
            Body: $"A system event occurred: {eventGridEvent.EventType}",
            Channel: "InApp",
            Category: "System");

        await _notificationService.CreateAsync(request).ConfigureAwait(false);

        _logger.LogInformation(
            "Created system notification for user {UserId} from event {EventType}",
            userId, eventGridEvent.EventType);
    }

    /// <summary>
    /// Extracts the user ID from an event subject using the "users/{userId}/..." convention.
    /// </summary>
    internal static string? ExtractUserId(string subject)
    {
        if (string.IsNullOrWhiteSpace(subject))
        {
            return null;
        }

        var segments = subject.Split('/', StringSplitOptions.RemoveEmptyEntries);

        for (var i = 0; i < segments.Length - 1; i++)
        {
            if (string.Equals(segments[i], "users", StringComparison.OrdinalIgnoreCase))
            {
                return segments[i + 1];
            }
        }

        return null;
    }
}
