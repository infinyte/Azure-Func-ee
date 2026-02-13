using System.Net;
using System.Text.Json;
using AzureFunctions.Shared.Middleware;
using AzureFunctions.Shared.Models;
using AzureFunctions.Shared.Tests.Helpers;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Moq;
using Xunit;

namespace AzureFunctions.Shared.Tests.Middleware;

/// <summary>
/// Tests for <see cref="ExceptionHandlingMiddleware"/>.
/// Since FunctionContext, HttpRequestData, and HttpResponseData are abstract and tightly coupled
/// to the Azure Functions runtime, these tests verify the MapException logic indirectly
/// by testing the middleware's public contract through its exception-to-status-code mapping.
/// Direct invocation of the middleware with real HTTP request/response objects is not feasible
/// in a pure unit test; those paths are better served by integration tests.
/// </summary>
public class ExceptionHandlingMiddlewareTests
{
    private readonly ExceptionHandlingMiddleware _sut = new();

    [Theory]
    [InlineData(typeof(ArgumentException), HttpStatusCode.BadRequest, "INVALID_ARGUMENT")]
    [InlineData(typeof(ArgumentNullException), HttpStatusCode.BadRequest, "ARGUMENT_NULL")]
    [InlineData(typeof(ArgumentOutOfRangeException), HttpStatusCode.BadRequest, "ARGUMENT_OUT_OF_RANGE")]
    [InlineData(typeof(KeyNotFoundException), HttpStatusCode.NotFound, "RESOURCE_NOT_FOUND")]
    [InlineData(typeof(FileNotFoundException), HttpStatusCode.NotFound, "FILE_NOT_FOUND")]
    [InlineData(typeof(UnauthorizedAccessException), HttpStatusCode.Forbidden, "ACCESS_DENIED")]
    [InlineData(typeof(InvalidOperationException), HttpStatusCode.Conflict, "INVALID_OPERATION")]
    [InlineData(typeof(NotImplementedException), HttpStatusCode.NotImplemented, "NOT_IMPLEMENTED")]
    [InlineData(typeof(NotSupportedException), HttpStatusCode.BadRequest, "NOT_SUPPORTED")]
    [InlineData(typeof(TimeoutException), HttpStatusCode.GatewayTimeout, "TIMEOUT")]
    [InlineData(typeof(FormatException), HttpStatusCode.BadRequest, "INVALID_FORMAT")]
    public void MapException_MapsKnownExceptions_ToExpectedStatusCodeAndErrorCode(
        Type exceptionType, HttpStatusCode expectedStatusCode, string expectedErrorCode)
    {
        // The MapException method is private static, so we test it through reflection
        // to verify the mapping table without needing a full HTTP pipeline.
        var method = typeof(ExceptionHandlingMiddleware)
            .GetMethod("MapException", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        method.Should().NotBeNull("MapException should exist as a private static method");

        var exception = (Exception)Activator.CreateInstance(exceptionType, "Test message")!;
        var result = method!.Invoke(null, [exception]);

        var valueTuple = ((HttpStatusCode StatusCode, string ErrorCode))result!;
        valueTuple.StatusCode.Should().Be(expectedStatusCode);
        valueTuple.ErrorCode.Should().Be(expectedErrorCode);
    }

    [Fact]
    public void MapException_UnknownException_ReturnsInternalServerError()
    {
        var method = typeof(ExceptionHandlingMiddleware)
            .GetMethod("MapException", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        method.Should().NotBeNull();

        var exception = new ApplicationException("Unknown error");
        var result = method!.Invoke(null, [exception]);

        var valueTuple = ((HttpStatusCode StatusCode, string ErrorCode))result!;
        valueTuple.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        valueTuple.ErrorCode.Should().Be("INTERNAL_ERROR");
    }

    [Fact]
    public void MapException_OperationCancelledException_ReturnsServiceUnavailable()
    {
        var method = typeof(ExceptionHandlingMiddleware)
            .GetMethod("MapException", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        method.Should().NotBeNull();

        var exception = new OperationCanceledException("Cancelled");
        var result = method!.Invoke(null, [exception]);

        var valueTuple = ((HttpStatusCode StatusCode, string ErrorCode))result!;
        valueTuple.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        valueTuple.ErrorCode.Should().Be("OPERATION_CANCELLED");
    }

    [Fact]
    public void ErrorResponse_CanIncludeCorrelationId()
    {
        // Verify the ErrorResponse model supports correlation ID.
        var errorResponse = new ErrorResponse
        {
            Message = "Test error",
            ErrorCode = "TEST",
            CorrelationId = "corr-12345"
        };

        errorResponse.CorrelationId.Should().Be("corr-12345");
    }

    [Fact]
    public void ErrorResponse_SerializesToExpectedJson()
    {
        // Verify error response JSON serialization matches what the middleware produces.
        var errorResponse = new ErrorResponse
        {
            Message = "Not found",
            ErrorCode = "RESOURCE_NOT_FOUND",
            CorrelationId = "abc-123",
            Detail = "Item was deleted."
        };

        var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        json.Should().Contain("\"message\":\"Not found\"");
        json.Should().Contain("\"errorCode\":\"RESOURCE_NOT_FOUND\"");
        json.Should().Contain("\"correlationId\":\"abc-123\"");
        json.Should().Contain("\"detail\":\"Item was deleted.\"");
    }

    [Fact]
    public void ErrorResponse_HasTimestamp()
    {
        var before = DateTimeOffset.UtcNow;
        var errorResponse = new ErrorResponse
        {
            Message = "Error",
            ErrorCode = "ERR"
        };
        var after = DateTimeOffset.UtcNow;

        errorResponse.Timestamp.Should().BeOnOrAfter(before);
        errorResponse.Timestamp.Should().BeOnOrBefore(after);
    }
}
