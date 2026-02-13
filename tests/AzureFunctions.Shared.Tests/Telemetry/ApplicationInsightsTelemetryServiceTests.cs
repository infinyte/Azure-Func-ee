using AzureFunctions.Shared.Telemetry;
using FluentAssertions;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Moq;
using Xunit;

namespace AzureFunctions.Shared.Tests.Telemetry;

public class ApplicationInsightsTelemetryServiceTests : IDisposable
{
    private readonly Mock<ITelemetryChannel> _mockChannel;
    private readonly TelemetryClient _telemetryClient;
    private readonly ApplicationInsightsTelemetryService _sut;
    private readonly List<ITelemetry> _sentTelemetry = [];

    public ApplicationInsightsTelemetryServiceTests()
    {
        _mockChannel = new Mock<ITelemetryChannel>();
        _mockChannel
            .Setup(c => c.Send(It.IsAny<ITelemetry>()))
            .Callback<ITelemetry>(t => _sentTelemetry.Add(t));

        var config = new TelemetryConfiguration
        {
            TelemetryChannel = _mockChannel.Object,
            ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000"
        };

        _telemetryClient = new TelemetryClient(config);
        _sut = new ApplicationInsightsTelemetryService(_telemetryClient);
    }

    public void Dispose()
    {
        _telemetryClient.FlushAsync(CancellationToken.None).GetAwaiter().GetResult();
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
    public void TrackEvent_SendsEventTelemetry()
    {
        // Act
        _sut.TrackEvent("TestEvent");
        _telemetryClient.Flush();

        // Assert
        _sentTelemetry.Should().ContainSingle(t => t is EventTelemetry);
        var eventTelemetry = (EventTelemetry)_sentTelemetry.Single(t => t is EventTelemetry);
        eventTelemetry.Name.Should().Be("TestEvent");
    }

    [Fact]
    public void TrackEvent_WithProperties_IncludesPropertiesInTelemetry()
    {
        // Arrange
        var properties = new Dictionary<string, string>
        {
            ["Key1"] = "Value1",
            ["Key2"] = "Value2"
        };

        // Act
        _sut.TrackEvent("TestEvent", properties);
        _telemetryClient.Flush();

        // Assert
        var eventTelemetry = (EventTelemetry)_sentTelemetry.Single(t => t is EventTelemetry);
        eventTelemetry.Properties.Should().Contain("Key1", "Value1");
        eventTelemetry.Properties.Should().Contain("Key2", "Value2");
    }

    [Fact]
    public void TrackException_SendsExceptionTelemetry()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");

        // Act
        _sut.TrackException(exception);
        _telemetryClient.Flush();

        // Assert
        _sentTelemetry.Should().ContainSingle(t => t is ExceptionTelemetry);
        var exceptionTelemetry = (ExceptionTelemetry)_sentTelemetry.Single(t => t is ExceptionTelemetry);
        exceptionTelemetry.Exception.Should().BeSameAs(exception);
    }

    [Fact]
    public void TrackException_WithProperties_IncludesPropertiesInTelemetry()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var properties = new Dictionary<string, string> { ["DocumentId"] = "doc-123" };

        // Act
        _sut.TrackException(exception, properties);
        _telemetryClient.Flush();

        // Assert
        var exceptionTelemetry = (ExceptionTelemetry)_sentTelemetry.Single(t => t is ExceptionTelemetry);
        exceptionTelemetry.Properties.Should().Contain("DocumentId", "doc-123");
    }

    [Fact]
    public void TrackMetric_SendsMetricTelemetry()
    {
        // Act
        _sut.TrackMetric("TestMetric", 42.5);
        _telemetryClient.Flush();

        // Assert
        _sentTelemetry.Should().ContainSingle(t => t is MetricTelemetry);
        var metricTelemetry = (MetricTelemetry)_sentTelemetry.Single(t => t is MetricTelemetry);
        metricTelemetry.Name.Should().Be("TestMetric");
        metricTelemetry.Sum.Should().Be(42.5);
    }

    [Fact]
    public void TrackMetric_WithProperties_IncludesPropertiesInTelemetry()
    {
        // Arrange
        var properties = new Dictionary<string, string> { ["Region"] = "EastUS" };

        // Act
        _sut.TrackMetric("TestMetric", 10.0, properties);
        _telemetryClient.Flush();

        // Assert
        var metricTelemetry = (MetricTelemetry)_sentTelemetry.Single(t => t is MetricTelemetry);
        metricTelemetry.Properties.Should().Contain("Region", "EastUS");
    }

    [Fact]
    public void TrackDependency_SendsDependencyTelemetry()
    {
        // Arrange
        var startTime = DateTimeOffset.UtcNow;
        var duration = TimeSpan.FromMilliseconds(150);

        // Act
        _sut.TrackDependency("HTTP", "ExternalApi", "GET /api/data", startTime, duration, true);
        _telemetryClient.Flush();

        // Assert
        _sentTelemetry.Should().ContainSingle(t => t is DependencyTelemetry);
        var dependencyTelemetry = (DependencyTelemetry)_sentTelemetry.Single(t => t is DependencyTelemetry);
        dependencyTelemetry.Type.Should().Be("HTTP");
        dependencyTelemetry.Target.Should().Be("ExternalApi");
        dependencyTelemetry.Data.Should().Be("GET /api/data");
        dependencyTelemetry.Duration.Should().Be(duration);
        dependencyTelemetry.Success.Should().BeTrue();
    }

    [Fact]
    public void TrackDependency_WithFailure_RecordsSuccessAsFalse()
    {
        // Arrange
        var startTime = DateTimeOffset.UtcNow;
        var duration = TimeSpan.FromMilliseconds(500);

        // Act
        _sut.TrackDependency("SQL", "Database", "SELECT * FROM orders", startTime, duration, false);
        _telemetryClient.Flush();

        // Assert
        var dependencyTelemetry = (DependencyTelemetry)_sentTelemetry.Single(t => t is DependencyTelemetry);
        dependencyTelemetry.Success.Should().BeFalse();
    }
}
