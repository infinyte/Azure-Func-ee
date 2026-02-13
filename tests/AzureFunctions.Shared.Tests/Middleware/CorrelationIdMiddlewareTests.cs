using AzureFunctions.Shared.Middleware;
using AzureFunctions.Shared.Tests.Helpers;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Moq;
using Xunit;

namespace AzureFunctions.Shared.Tests.Middleware;

/// <summary>
/// Tests for <see cref="CorrelationIdMiddleware"/>.
/// Since HttpRequestData is abstract and tightly coupled to the runtime,
/// we test the non-HTTP path (which always generates a new correlation ID)
/// and verify constant values.
/// </summary>
public class CorrelationIdMiddlewareTests
{
    private readonly CorrelationIdMiddleware _sut = new();

    [Fact]
    public void CorrelationIdHeader_HasExpectedValue()
    {
        CorrelationIdMiddleware.CorrelationIdHeader.Should().Be("X-Correlation-ID");
    }

    [Fact]
    public void CorrelationIdKey_HasExpectedValue()
    {
        CorrelationIdMiddleware.CorrelationIdKey.Should().Be("CorrelationId");
    }

    [Fact]
    public async Task Invoke_WhenNoHttpRequest_GeneratesNewCorrelationIdAndStoresInItems()
    {
        // Arrange
        var mockContext = FunctionContextMockFactory.Create();
        var nextCalled = false;

        Task Next(FunctionContext context)
        {
            nextCalled = true;
            return Task.CompletedTask;
        }

        // Act
        await _sut.Invoke(mockContext.Object, Next);

        // Assert
        nextCalled.Should().BeTrue("the next delegate should always be called");
        mockContext.Object.Items.Should().ContainKey(CorrelationIdMiddleware.CorrelationIdKey);

        var correlationId = mockContext.Object.Items[CorrelationIdMiddleware.CorrelationIdKey] as string;
        correlationId.Should().NotBeNullOrWhiteSpace();

        // Verify it is a valid GUID in "D" format (xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx).
        Guid.TryParse(correlationId, out _).Should().BeTrue(
            "the generated correlation ID should be a valid GUID");
    }

    [Fact]
    public async Task Invoke_GeneratesUniqueCorrelationIds_AcrossMultipleInvocations()
    {
        // Arrange
        var correlationIds = new List<string>();

        for (var i = 0; i < 10; i++)
        {
            var mockContext = FunctionContextMockFactory.Create();

            Task Next(FunctionContext context) => Task.CompletedTask;

            // Act
            await _sut.Invoke(mockContext.Object, Next);

            var correlationId = mockContext.Object.Items[CorrelationIdMiddleware.CorrelationIdKey] as string;
            correlationIds.Add(correlationId!);
        }

        // Assert — all generated IDs should be unique.
        correlationIds.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task Invoke_AlwaysCallsNextDelegate()
    {
        // Arrange
        var mockContext = FunctionContextMockFactory.Create();
        var nextCallCount = 0;

        Task Next(FunctionContext context)
        {
            nextCallCount++;
            return Task.CompletedTask;
        }

        // Act
        await _sut.Invoke(mockContext.Object, Next);

        // Assert
        nextCallCount.Should().Be(1);
    }
}
