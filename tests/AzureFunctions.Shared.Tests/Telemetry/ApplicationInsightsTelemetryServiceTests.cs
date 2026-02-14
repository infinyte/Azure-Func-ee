using AzureFunctions.Shared.Telemetry;
using FluentAssertions;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Xunit;

namespace AzureFunctions.Shared.Tests.Telemetry;

public class ApplicationInsightsTelemetryServiceTests : IDisposable
{
    private readonly TelemetryConfiguration _config;
    private readonly TelemetryClient _telemetryClient;
    private readonly ApplicationInsightsTelemetryService _sut;

    public ApplicationInsightsTelemetryServiceTests()
    {
        _config = new TelemetryConfiguration
        {
            DisableTelemetry = true,
            ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000"
        };

        _telemetryClient = new TelemetryClient(_config);
        _sut = new ApplicationInsightsTelemetryService(_telemetryClient);
    }

    public void Dispose()
    {
        _config.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Constructor_WithNullTelemetryClient_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ApplicationInsightsTelemetryService(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("telemetryClient");
    }

    [Fact]
    public void TrackEvent_DoesNotThrow()
    {
        // Act & Assert
        var act = () => _sut.TrackEvent("TestEvent");
        act.Should().NotThrow();
    }

    [Fact]
    public void TrackEvent_WithProperties_DoesNotThrow()
    {
        // Arrange
        var properties = new Dictionary<string, string>
        {
            ["Key1"] = "Value1",
            ["Key2"] = "Value2"
        };

        // Act & Assert
        var act = () => _sut.TrackEvent("TestEvent", properties);
        act.Should().NotThrow();
    }

    [Fact]
    public void TrackException_DoesNotThrow()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");

        // Act & Assert
        var act = () => _sut.TrackException(exception);
        act.Should().NotThrow();
    }

    [Fact]
    public void TrackException_WithProperties_DoesNotThrow()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var properties = new Dictionary<string, string> { ["DocumentId"] = "doc-123" };

        // Act & Assert
        var act = () => _sut.TrackException(exception, properties);
        act.Should().NotThrow();
    }

    [Fact]
    public void TrackMetric_DoesNotThrow()
    {
        // Act & Assert
        var act = () => _sut.TrackMetric("TestMetric", 42.5);
        act.Should().NotThrow();
    }

    [Fact]
    public void TrackMetric_WithProperties_DoesNotThrow()
    {
        // Arrange
        var properties = new Dictionary<string, string> { ["Region"] = "EastUS" };

        // Act & Assert
        var act = () => _sut.TrackMetric("TestMetric", 10.0, properties);
        act.Should().NotThrow();
    }

    [Fact]
    public void TrackDependency_DoesNotThrow()
    {
        // Arrange
        var startTime = DateTimeOffset.UtcNow;
        var duration = TimeSpan.FromMilliseconds(150);

        // Act & Assert
        var act = () => _sut.TrackDependency("HTTP", "ExternalApi", "GET /api/data", startTime, duration, true);
        act.Should().NotThrow();
    }

    [Fact]
    public void TrackDependency_WithFailure_DoesNotThrow()
    {
        // Arrange
        var startTime = DateTimeOffset.UtcNow;
        var duration = TimeSpan.FromMilliseconds(500);

        // Act & Assert
        var act = () => _sut.TrackDependency("SQL", "Database", "SELECT * FROM orders", startTime, duration, false);
        act.Should().NotThrow();
    }
}
