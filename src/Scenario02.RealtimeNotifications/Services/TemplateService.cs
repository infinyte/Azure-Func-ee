using Scenario02.RealtimeNotifications.Models;

namespace Scenario02.RealtimeNotifications.Services;

/// <summary>
/// Simple template rendering service with {parameter} placeholder substitution.
/// </summary>
public sealed class TemplateService : ITemplateService
{
    /// <inheritdoc />
    public (string Title, string Body) Render(
        NotificationTemplate template,
        IReadOnlyDictionary<string, string> parameters)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(parameters);

        var title = SubstitutePlaceholders(template.TitleTemplate, parameters);
        var body = SubstitutePlaceholders(template.BodyTemplate, parameters);

        return (title, body);
    }

    private static string SubstitutePlaceholders(string template, IReadOnlyDictionary<string, string> parameters)
    {
        var result = template;

        foreach (var (key, value) in parameters)
        {
            result = result.Replace($"{{{key}}}", value, StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }
}
