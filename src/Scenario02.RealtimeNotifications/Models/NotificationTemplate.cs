namespace Scenario02.RealtimeNotifications.Models;

/// <summary>
/// A notification template with parameter placeholders.
/// </summary>
/// <param name="Name">The template name/identifier.</param>
/// <param name="TitleTemplate">The title template with {parameter} placeholders.</param>
/// <param name="BodyTemplate">The body template with {parameter} placeholders.</param>
public sealed record NotificationTemplate(
    string Name,
    string TitleTemplate,
    string BodyTemplate);
