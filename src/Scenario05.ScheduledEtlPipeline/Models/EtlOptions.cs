namespace Scenario05.ScheduledEtlPipeline.Models;

/// <summary>
/// Configuration options for the ETL pipeline.
/// Bound from the "EtlPipeline" configuration section.
/// </summary>
public sealed class EtlOptions
{
    /// <summary>
    /// The configuration section name used to bind these options.
    /// </summary>
    public const string SectionName = "EtlPipeline";

    /// <summary>
    /// The connection name for the Azure Storage account used by the pipeline.
    /// </summary>
    public string StorageConnectionName { get; set; } = "AzureWebJobsStorage";

    /// <summary>
    /// The blob container for raw extracted data.
    /// </summary>
    public string RawContainer { get; set; } = "etl-raw";

    /// <summary>
    /// The blob container for validated data.
    /// </summary>
    public string ValidatedContainer { get; set; } = "etl-validated";

    /// <summary>
    /// The blob container for transformed data.
    /// </summary>
    public string TransformedContainer { get; set; } = "etl-transformed";

    /// <summary>
    /// The blob container for final output data.
    /// </summary>
    public string OutputContainer { get; set; } = "etl-output";

    /// <summary>
    /// The Azure Table Storage table name for pipeline run tracking.
    /// </summary>
    public string PipelineRunsTable { get; set; } = "pipelineruns";

    /// <summary>
    /// The CSV blob container/path for the CSV source extraction.
    /// </summary>
    public string CsvSourceContainer { get; set; } = "etl-raw";

    /// <summary>
    /// The blob name for the CSV source file.
    /// </summary>
    public string CsvSourceBlobName { get; set; } = "source-data.csv";

    /// <summary>
    /// The base URL for the external API source.
    /// </summary>
    public string ExternalApiBaseUrl { get; set; } = "https://api.external-data.internal";

    /// <summary>
    /// The maximum number of records to extract per source.
    /// </summary>
    public int MaxRecordsPerSource { get; set; } = 10000;

    /// <summary>
    /// The CRON expression for the scheduled ETL trigger.
    /// </summary>
    public string CronSchedule { get; set; } = "0 0 1 * * *";
}
