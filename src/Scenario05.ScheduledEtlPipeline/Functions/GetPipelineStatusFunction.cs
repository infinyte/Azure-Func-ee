using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Scenario05.ScheduledEtlPipeline.Models.Dtos;
using Scenario05.ScheduledEtlPipeline.Services;

namespace Scenario05.ScheduledEtlPipeline.Functions;

/// <summary>
/// HTTP-triggered function for querying the status of an ETL pipeline run.
/// </summary>
public sealed class GetPipelineStatusFunction
{
    private readonly IPipelineService _pipelineService;
    private readonly ILogger<GetPipelineStatusFunction> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    /// <summary>
    /// Initializes a new instance of <see cref="GetPipelineStatusFunction"/>.
    /// </summary>
    /// <param name="pipelineService">The pipeline service.</param>
    /// <param name="logger">The logger instance.</param>
    public GetPipelineStatusFunction(IPipelineService pipelineService, ILogger<GetPipelineStatusFunction> logger)
    {
        _pipelineService = pipelineService ?? throw new ArgumentNullException(nameof(pipelineService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the status of a specific pipeline run.
    /// </summary>
    /// <param name="req">The HTTP request.</param>
    /// <param name="runId">The pipeline run identifier from the route.</param>
    /// <returns>200 OK with the run status, or 404 Not Found.</returns>
    [Function("GetPipelineStatus")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "etl/runs/{runId}")] HttpRequestData req,
        string runId)
    {
        _logger.LogInformation("GET /api/etl/runs/{RunId}", runId);

        var run = await _pipelineService.GetRunAsync(runId).ConfigureAwait(false);

        if (run is null)
        {
            _logger.LogInformation("Pipeline run {RunId} not found", runId);
            var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
            await notFoundResponse.WriteAsJsonAsync(new { message = $"Pipeline run '{runId}' not found." })
                .ConfigureAwait(false);
            return notFoundResponse;
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(PipelineRunResponse.FromPipelineRun(run)).ConfigureAwait(false);
        return response;
    }
}
