using AzureFunctions.Shared.Telemetry;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Scenario01.DocumentProcessing.Services;

namespace Scenario01.DocumentProcessing.Functions;

/// <summary>
/// Timer-triggered function that generates a daily processing report at 2:00 AM UTC.
/// Aggregates document processing statistics for the previous day.
/// </summary>
public sealed class GenerateProcessingReportFunction
{
    private readonly IDocumentProcessingService _processingService;
    private readonly ITelemetryService _telemetry;
    private readonly ILogger<GenerateProcessingReportFunction> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="GenerateProcessingReportFunction"/>.
    /// </summary>
    public GenerateProcessingReportFunction(
        IDocumentProcessingService processingService,
        ITelemetryService telemetry,
        ILogger<GenerateProcessingReportFunction> logger)
    {
        _processingService = processingService ?? throw new ArgumentNullException(nameof(processingService));
        _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes daily at 2:00 AM UTC. Generates a processing report for the previous day
    /// and tracks summary metrics in Application Insights.
    /// </summary>
    /// <param name="timerInfo">Timer trigger metadata including schedule status.</param>
    /// <param name="context">The function execution context.</param>
    [Function("GenerateProcessingReport")]
    public async Task RunAsync(
        [TimerTrigger("0 0 2 * * *")] TimerInfo timerInfo,
        FunctionContext context)
    {
        var previousDay = DateTimeOffset.UtcNow.AddDays(-1);

        _logger.LogInformation(
            "Generating daily processing report for {Date}. Timer past due: {IsPastDue}",
            previousDay.Date.ToString("yyyy-MM-dd"),
            timerInfo.IsPastDue);

        var report = await _processingService
            .GenerateReportAsync(previousDay, context.CancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "Daily report generated - Total: {Total}, Success: {Success}, Failed: {Failed}, Avg Time: {AvgMs}ms",
            report.TotalProcessed,
            report.SuccessCount,
            report.FailureCount,
            report.AverageProcessingTimeMs);

        // Track key metrics in Application Insights for dashboard visibility.
        _telemetry.TrackMetric("DailyDocumentsProcessed", report.TotalProcessed, new Dictionary<string, string>
        {
            ["ReportDate"] = report.ReportDate.Date.ToString("yyyy-MM-dd")
        });

        _telemetry.TrackMetric("DailyProcessingSuccessCount", report.SuccessCount);
        _telemetry.TrackMetric("DailyProcessingFailureCount", report.FailureCount);
        _telemetry.TrackMetric("DailyAverageProcessingTimeMs", report.AverageProcessingTimeMs);

        // Track classification breakdown as individual metrics.
        foreach (var (classification, count) in report.ClassificationBreakdown)
        {
            _telemetry.TrackMetric($"DailyClassification_{classification}", count, new Dictionary<string, string>
            {
                ["ReportDate"] = report.ReportDate.Date.ToString("yyyy-MM-dd"),
                ["Classification"] = classification
            });
        }

        _telemetry.TrackEvent("DailyReportGenerated", new Dictionary<string, string>
        {
            ["ReportDate"] = report.ReportDate.Date.ToString("yyyy-MM-dd"),
            ["TotalProcessed"] = report.TotalProcessed.ToString(),
            ["SuccessCount"] = report.SuccessCount.ToString(),
            ["FailureCount"] = report.FailureCount.ToString()
        });
    }
}
