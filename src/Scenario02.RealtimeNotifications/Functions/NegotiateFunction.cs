using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Scenario02.RealtimeNotifications.Functions;

/// <summary>
/// HTTP-triggered function that provides SignalR connection negotiation.
/// Returns the SignalR Service connection info for the calling user.
/// </summary>
public sealed class NegotiateFunction
{
    private readonly ILogger<NegotiateFunction> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="NegotiateFunction"/>.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public NegotiateFunction(ILogger<NegotiateFunction> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Negotiates a SignalR connection for the requesting user.
    /// The user ID is extracted from the x-ms-client-principal-id header.
    /// </summary>
    /// <param name="req">The HTTP request.</param>
    /// <param name="connectionInfo">The SignalR connection info from the input binding.</param>
    /// <returns>The SignalR connection info response.</returns>
    [Function("Negotiate")]
    public HttpResponseData Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "negotiate")] HttpRequestData req,
        [SignalRConnectionInfoInput(HubName = "notifications", UserId = "{headers.x-ms-client-principal-id}")] string connectionInfo)
    {
        _logger.LogInformation("SignalR negotiate requested");

        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");
        response.WriteString(connectionInfo);
        return response;
    }
}
