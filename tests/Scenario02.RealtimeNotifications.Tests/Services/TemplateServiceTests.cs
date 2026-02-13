using FluentAssertions;
using Scenario02.RealtimeNotifications.Models;
using Scenario02.RealtimeNotifications.Services;
using Xunit;

namespace Scenario02.RealtimeNotifications.Tests.Services;

public class TemplateServiceTests
{
    private readonly TemplateService _service = new();

    [Fact]
    public void Render_WithParameters_SubstitutesPlaceholders()
    {
        // Arrange
        var template = new NotificationTemplate(
            "OrderConfirmation",
            "Order {orderId} Confirmed",
            "Hi {userName}, your order {orderId} has been confirmed.");

        var parameters = new Dictionary<string, string>
        {
            ["orderId"] = "ORD-123",
            ["userName"] = "Alice"
        };

        // Act
        var (title, body) = _service.Render(template, parameters);

        // Assert
        title.Should().Be("Order ORD-123 Confirmed");
        body.Should().Be("Hi Alice, your order ORD-123 has been confirmed.");
    }

    [Fact]
    public void Render_WithNoMatchingParameters_LeavesPlaceholders()
    {
        // Arrange
        var template = new NotificationTemplate(
            "Test",
            "Hello {name}",
            "Your {item} is ready.");

        var parameters = new Dictionary<string, string>();

        // Act
        var (title, body) = _service.Render(template, parameters);

        // Assert
        title.Should().Be("Hello {name}");
        body.Should().Be("Your {item} is ready.");
    }

    [Fact]
    public void Render_CaseInsensitivePlaceholders_Substitutes()
    {
        // Arrange
        var template = new NotificationTemplate(
            "Test",
            "Hi {UserName}",
            "Welcome {username}!");

        var parameters = new Dictionary<string, string>
        {
            ["userName"] = "Bob"
        };

        // Act
        var (title, body) = _service.Render(template, parameters);

        // Assert
        title.Should().Be("Hi Bob");
        body.Should().Be("Welcome Bob!");
    }
}
