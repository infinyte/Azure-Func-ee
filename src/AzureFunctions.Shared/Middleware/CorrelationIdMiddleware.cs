using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace AzureFunctions.Shared.Middleware;

/// <summary>
/// Middleware that ensures every HTTP request has a correlation ID for distributed tracing.
/// If the incoming request contains an <c>X-Correlation-ID</c> header, that value is used;
/// otherwise a new GUID is generated. The correlation ID is stored in
/// <see cref="FunctionContext.Items"/> and added to the outgoing response headers.
/// </summary>
public sealed class CorrelationIdMiddleware : IFunctionsWorkerMiddleware
{
    /// <summary>
    /// The HTTP header name used to propagate the correlation ID.
    /// </summary>
    public const string CorrelationIdHeader = "X-Correlation-ID";

    /// <summary>
    /// The key used to store the correlation ID in <see cref="FunctionContext.Items"/>.
    /// </summary>
    public const string CorrelationIdKey = "CorrelationId";

    /// <inheritdoc />
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var httpReqData = await context.GetHttpRequestDataAsync();
        string correlationId;

        if (httpReqData is not null
            && httpReqData.Headers.TryGetValues(CorrelationIdHeader, out var headerValues))
        {
            correlationId = headerValues.First();
        }
        else
        {
            correlationId = Guid.NewGuid().ToString("D");
        }

        // Store in context items so other middleware and the function itself can access it.
        context.Items[CorrelationIdKey] = correlationId;

        var logger = context.GetLogger<CorrelationIdMiddleware>();
        logger.LogDebug("Correlation ID for invocation {InvocationId}: {CorrelationId}",
            context.InvocationId, correlationId);

        await next(context);

        // Add the correlation ID to the outgoing HTTP response if this is an HTTP trigger.
        if (httpReqData is not null)
        {
            var response = context.GetHttpResponseData();

            if (response is not null)
            {
                response.Headers.TryAddWithoutValidation(CorrelationIdHeader, correlationId);
            }
        }
    }
}
