using System.Net;
using System.Text.Json;
using AzureFunctions.Shared.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace AzureFunctions.Shared.Middleware;

/// <summary>
/// Middleware that provides centralized exception handling for Azure Functions.
/// For HTTP-triggered functions, catches unhandled exceptions and returns a consistent
/// JSON error response with an appropriate HTTP status code.
/// For non-HTTP triggers, logs the exception and re-throws to allow the Functions runtime
/// to handle retries and dead-lettering.
/// </summary>
public sealed class ExceptionHandlingMiddleware : IFunctionsWorkerMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <inheritdoc />
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var logger = context.GetLogger<ExceptionHandlingMiddleware>();
            logger.LogError(ex, "Unhandled exception in function {FunctionName}. {ExceptionMessage}",
                context.FunctionDefinition.Name, ex.Message);

            var httpReqData = await context.GetHttpRequestDataAsync();

            if (httpReqData is null)
            {
                // Non-HTTP trigger: re-throw so the runtime handles retries / dead-lettering.
                throw;
            }

            var (statusCode, errorCode) = MapException(ex);

            // Attempt to retrieve the correlation ID stored by CorrelationIdMiddleware.
            context.Items.TryGetValue("CorrelationId", out var correlationIdObj);
            var correlationId = correlationIdObj as string;

            var errorResponse = new ErrorResponse
            {
                Message = ex.Message,
                Detail = ex.InnerException?.Message,
                CorrelationId = correlationId,
                ErrorCode = errorCode
            };

            var response = httpReqData.CreateResponse(statusCode);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");

            await response.WriteStringAsync(JsonSerializer.Serialize(errorResponse, JsonOptions));

            context.GetInvocationResult().Value = response;
        }
    }

    /// <summary>
    /// Maps a caught exception to an appropriate HTTP status code and machine-readable error code.
    /// </summary>
    private static (HttpStatusCode StatusCode, string ErrorCode) MapException(Exception exception) =>
        exception switch
        {
            ArgumentNullException => (HttpStatusCode.BadRequest, "ARGUMENT_NULL"),
            ArgumentOutOfRangeException => (HttpStatusCode.BadRequest, "ARGUMENT_OUT_OF_RANGE"),
            ArgumentException => (HttpStatusCode.BadRequest, "INVALID_ARGUMENT"),
            KeyNotFoundException => (HttpStatusCode.NotFound, "RESOURCE_NOT_FOUND"),
            FileNotFoundException => (HttpStatusCode.NotFound, "FILE_NOT_FOUND"),
            UnauthorizedAccessException => (HttpStatusCode.Forbidden, "ACCESS_DENIED"),
            InvalidOperationException => (HttpStatusCode.Conflict, "INVALID_OPERATION"),
            NotImplementedException => (HttpStatusCode.NotImplemented, "NOT_IMPLEMENTED"),
            NotSupportedException => (HttpStatusCode.BadRequest, "NOT_SUPPORTED"),
            TimeoutException => (HttpStatusCode.GatewayTimeout, "TIMEOUT"),
            OperationCanceledException => (HttpStatusCode.ServiceUnavailable, "OPERATION_CANCELLED"),
            FormatException => (HttpStatusCode.BadRequest, "INVALID_FORMAT"),
            _ => (HttpStatusCode.InternalServerError, "INTERNAL_ERROR")
        };
}
