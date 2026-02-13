namespace Scenario02.RealtimeNotifications.Models.Dtos;

/// <summary>
/// Request DTO for sending a notification.
/// </summary>
/// <param name="UserId">The target user ID.</param>
/// <param name="Title">The notification title.</param>
/// <param name="Body">The notification body.</param>
/// <param name="Channel">The delivery channel (InApp, Email, Push).</param>
/// <param name="Category">The notification category.</param>
public sealed record SendNotificationRequest(
    string UserId,
    string Title,
    string Body,
    string Channel = "InApp",
    string Category = "General");
