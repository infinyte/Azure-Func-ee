using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace AzureFunctions.Shared.Resilience;

/// <summary>
/// Provides standard resilience strategy configurations (retry, circuit breaker, timeout)
/// for use across Azure Function HTTP clients and other outbound calls.
/// Built on Polly v8 (<c>Polly.Core</c>) and <c>Microsoft.Extensions.Resilience</c>.
/// </summary>
public static class ResiliencePolicies
{
    /// <summary>
    /// Returns default retry options with exponential backoff.
    /// 3 retries, starting at a 2-second base delay, with jitter.
    /// </summary>
    public static RetryStrategyOptions GetDefaultRetryOptions() =>
        new()
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(2),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>().Handle<TimeoutRejectedException>()
        };

    /// <summary>
    /// Returns default circuit breaker options.
    /// Opens the circuit after 5 failures within a sampling window when the failure ratio
    /// exceeds 50% with a minimum throughput of 10 requests. The circuit stays open for 30 seconds.
    /// </summary>
    public static CircuitBreakerStrategyOptions GetDefaultCircuitBreakerOptions() =>
        new()
        {
            FailureRatio = 0.5,
            MinimumThroughput = 10,
            SamplingDuration = TimeSpan.FromSeconds(30),
            BreakDuration = TimeSpan.FromSeconds(30),
            ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>().Handle<TimeoutRejectedException>()
        };

    /// <summary>
    /// Returns default timeout options with a 30-second timeout.
    /// </summary>
    public static TimeoutStrategyOptions GetDefaultTimeoutOptions() =>
        new()
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

    /// <summary>
    /// Adds standard resilience policies (retry, circuit breaker, and timeout) to an
    /// <see cref="IHttpClientBuilder"/> pipeline.
    /// </summary>
    /// <param name="builder">The HTTP client builder to configure.</param>
    /// <returns>The configured <see cref="IHttpClientBuilder"/> for chaining.</returns>
    public static IHttpClientBuilder AddStandardResilience(this IHttpClientBuilder builder)
    {
        builder.AddResilienceHandler("standard-resilience", pipelineBuilder =>
        {
            pipelineBuilder
                .AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromSeconds(2),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true
                })
                .AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                {
                    FailureRatio = 0.5,
                    MinimumThroughput = 10,
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    BreakDuration = TimeSpan.FromSeconds(30)
                })
                .AddTimeout(GetDefaultTimeoutOptions());
        });

        return builder;
    }
}
