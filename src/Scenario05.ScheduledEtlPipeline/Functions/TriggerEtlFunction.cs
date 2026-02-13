using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Scenario05.ScheduledEtlPipeline.Models.Dtos;
using Scenario05.ScheduledEtlPipeline.Services;

namespace Scenario05.ScheduledEtlPipeline.Functions;

/// <summary>
/// HTTP-triggered function for on-demand ETL pipeline execution.
/// </summary>
public sealed class TriggerEtlFunction
{
    private readonly IPipelineService _pipelineService;
    private readonly ILogger<TriggerEtlFunction> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    /// <summary>
    /// Initializes a new instance of <see cref="TriggerEtlFunction"/>.
    /// </summary>
    /// <param name="pipelineService">The pipeline service.</param>
    /// <param name="logger">The logger instance.</param>
    public TriggerEtlFunction(IPipelineService pipelineService, ILogger<TriggerEtlFunction> logger)
    {
        _pipelineService = pipelineService ?? throw new ArgumentNullException(nameof(pipelineService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Triggers an on-demand ETL pipeline run.
    /// </summary>
    /// <param name="req">The HTTP request.</param>
    /// <param name="durableClient">The durable task client.</param>
    /// <returns>202 Accepted with the pipeline run ID.</returns>
    [Function("TriggerEtl")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "etl/trigger")] HttpRequestData req,
        [DurableClient] DurableTaskClient durableClient)
    {
        _logger.LogInformation("POST /api/etl/trigger");

        var run = await _pipelineService.CreateRunAsync("Manual").ConfigureAwait(false);

        var instanceId = await durableClient.ScheduleNewOrchestrationInstanceAsync(
            "EtlOrchestrator",
            run.RunId).ConfigureAwait(false);

        _logger.LogInformation(
            "Started on-demand ETL orchestration {InstanceId} for run {RunId}",
            instanceId, run.RunId);

        var response = req.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(PipelineRunResponse.FromPipelineRun(run)).ConfigureAwait(false);
        return response;
    }
}
