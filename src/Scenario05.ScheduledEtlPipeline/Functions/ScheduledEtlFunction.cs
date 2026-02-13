using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Scenario05.ScheduledEtlPipeline.Services;

namespace Scenario05.ScheduledEtlPipeline.Functions;

/// <summary>
/// Timer-triggered function that initiates the scheduled ETL pipeline.
/// Runs daily at 1:00 AM UTC (configurable via EtlPipeline:CronSchedule).
/// </summary>
public sealed class ScheduledEtlFunction
{
    private readonly IPipelineService _pipelineService;
    private readonly ILogger<ScheduledEtlFunction> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="ScheduledEtlFunction"/>.
    /// </summary>
    /// <param name="pipelineService">The pipeline service.</param>
    /// <param name="logger">The logger instance.</param>
    public ScheduledEtlFunction(IPipelineService pipelineService, ILogger<ScheduledEtlFunction> logger)
    {
        _pipelineService = pipelineService ?? throw new ArgumentNullException(nameof(pipelineService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes the scheduled ETL trigger, creating a pipeline run and starting the orchestration.
    /// </summary>
    /// <param name="timerInfo">The timer trigger information.</param>
    /// <param name="durableClient">The durable task client for starting orchestrations.</param>
    [Function("ScheduledEtl")]
    public async Task RunAsync(
        [TimerTrigger("0 0 1 * * *")] TimerInfo timerInfo,
        [DurableClient] DurableTaskClient durableClient)
    {
        _logger.LogInformation("Scheduled ETL trigger fired at {Time}", DateTimeOffset.UtcNow);

        var run = await _pipelineService.CreateRunAsync("Scheduled").ConfigureAwait(false);

        var instanceId = await durableClient.ScheduleNewOrchestrationInstanceAsync(
            "EtlOrchestrator",
            run.RunId).ConfigureAwait(false);

        _logger.LogInformation(
            "Started ETL orchestration {InstanceId} for pipeline run {RunId}",
            instanceId, run.RunId);
    }
}
