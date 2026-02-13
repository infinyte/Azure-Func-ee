using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Scenario02.RealtimeNotifications.Models.Dtos;
using Scenario02.RealtimeNotifications.Services;

namespace Scenario02.RealtimeNotifications.Functions;

/// <summary>
/// HTTP-triggered function for sending notifications.
/// Validates the request, persists the notification, and queues it for delivery.
/// </summary>
public sealed class SendNotificationFunction
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<SendNotificationFunction> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    /// <summary>
    /// Initializes a new instance of <see cref="SendNotificationFunction"/>.
    /// </summary>
    /// <param name="notificationService">The notification service.</param>
    /// <param name="logger">The logger instance.</param>
    public SendNotificationFunction(
        INotificationService notificationService,
        ILogger<SendNotificationFunction> logger)
    {
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Sends a notification by persisting it and queuing a delivery message.
    /// </summary>
    /// <param name="req">The HTTP request containing the notification payload.</param>
    /// <returns>A response containing the queue output binding message.</returns>
    [Function("SendNotification")]
    public async Task<SendNotificationOutput> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "notifications")] HttpRequestData req)
    {
        _logger.LogInformation("POST /api/notifications");

        var body = await req.ReadAsStringAsync().ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(body))
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(new { message = "Request body is required." }).ConfigureAwait(false);
            return new SendNotificationOutput { HttpResponse = badResponse };
        }

        var request = JsonSerializer.Deserialize<SendNotificationRequest>(body, JsonOptions);

        if (request is null || string.IsNullOrWhiteSpace(request.UserId) || string.IsNullOrWhiteSpace(request.Title))
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(new { message = "UserId and Title are required." }).ConfigureAwait(false);
            return new SendNotificationOutput { HttpResponse = badResponse };
        }

        var notification = await _notificationService.CreateAsync(request).ConfigureAwait(false);

        _logger.LogInformation(
            "Notification {NotificationId} created for user {UserId}, queuing for delivery",
            notification.NotificationId, notification.UserId);

        var response = req.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(NotificationResponse.FromNotification(notification)).ConfigureAwait(false);

        // Queue the notification for delivery processing.
        var queueMessage = JsonSerializer.Serialize(new
        {
            notification.NotificationId,
            notification.UserId,
            Channel = notification.Channel,
            notification.Title,
            notification.Body
        }, JsonOptions);

        return new SendNotificationOutput
        {
            HttpResponse = response,
            QueueMessage = queueMessage
        };
    }
}

/// <summary>
/// Multi-output binding for SendNotification: HTTP response + queue message.
/// </summary>
public sealed class SendNotificationOutput
{
    /// <summary>
    /// The HTTP response.
    /// </summary>
    [HttpResult]
    public HttpResponseData HttpResponse { get; set; } = null!;

    /// <summary>
    /// The queue message for delivery processing.
    /// </summary>
    [QueueOutput("notification-delivery", Connection = "AzureWebJobsStorage")]
    public string? QueueMessage { get; set; }
}
