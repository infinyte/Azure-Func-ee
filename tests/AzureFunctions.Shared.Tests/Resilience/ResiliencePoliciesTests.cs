using AzureFunctions.Shared.Resilience;
using FluentAssertions;
using Polly;
using Xunit;

namespace AzureFunctions.Shared.Tests.Resilience;

public class ResiliencePoliciesTests
{
    [Fact]
    public void GetDefaultRetryOptions_HasThreeRetryAttempts()
    {
        // Act
        var options = ResiliencePolicies.GetDefaultRetryOptions();

        // Assert
        options.MaxRetryAttempts.Should().Be(3);
    }

    [Fact]
    public void GetDefaultRetryOptions_HasTwoSecondBaseDelay()
    {
        // Act
        var options = ResiliencePolicies.GetDefaultRetryOptions();

        // Assert
        options.Delay.Should().Be(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void GetDefaultRetryOptions_UsesExponentialBackoff()
    {
        // Act
        var options = ResiliencePolicies.GetDefaultRetryOptions();

        // Assert
        options.BackoffType.Should().Be(DelayBackoffType.Exponential);
    }

    [Fact]
    public void GetDefaultRetryOptions_HasJitterEnabled()
    {
        // Act
        var options = ResiliencePolicies.GetDefaultRetryOptions();

        // Assert
        options.UseJitter.Should().BeTrue();
    }

    [Fact]
    public void GetDefaultCircuitBreakerOptions_HasFiftyPercentFailureRatio()
    {
        // Act
        var options = ResiliencePolicies.GetDefaultCircuitBreakerOptions();

        // Assert
        options.FailureRatio.Should().Be(0.5);
    }

    [Fact]
    public void GetDefaultCircuitBreakerOptions_HasMinimumThroughputOfTen()
    {
        // Act
        var options = ResiliencePolicies.GetDefaultCircuitBreakerOptions();

        // Assert
        options.MinimumThroughput.Should().Be(10);
    }

    [Fact]
    public void GetDefaultCircuitBreakerOptions_HasThirtySecondSamplingDuration()
    {
        // Act
        var options = ResiliencePolicies.GetDefaultCircuitBreakerOptions();

        // Assert
        options.SamplingDuration.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void GetDefaultCircuitBreakerOptions_HasThirtySecondBreakDuration()
    {
        // Act
        var options = ResiliencePolicies.GetDefaultCircuitBreakerOptions();

        // Assert
        options.BreakDuration.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void GetDefaultTimeoutOptions_HasThirtySecondTimeout()
    {
        // Act
        var options = ResiliencePolicies.GetDefaultTimeoutOptions();

        // Assert
        options.Timeout.Should().Be(TimeSpan.FromSeconds(30));
    }
}
