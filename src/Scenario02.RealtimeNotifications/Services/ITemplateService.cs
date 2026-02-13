using Scenario02.RealtimeNotifications.Models;

namespace Scenario02.RealtimeNotifications.Services;

/// <summary>
/// Service for rendering notification templates with parameter substitution.
/// </summary>
public interface ITemplateService
{
    /// <summary>
    /// Renders a template by substituting parameter placeholders.
    /// </summary>
    /// <param name="template">The notification template.</param>
    /// <param name="parameters">The parameter values keyed by placeholder name.</param>
    /// <returns>A tuple of (renderedTitle, renderedBody).</returns>
    (string Title, string Body) Render(NotificationTemplate template, IReadOnlyDictionary<string, string> parameters);
}
