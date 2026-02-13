namespace Scenario05.ScheduledEtlPipeline.Models;

/// <summary>
/// Represents the current status of an ETL pipeline run.
/// </summary>
public enum PipelineStatus
{
    /// <summary>Pipeline run has been created but not yet started.</summary>
    Pending = 0,

    /// <summary>Pipeline is extracting data from source systems.</summary>
    Extracting = 1,

    /// <summary>Pipeline is validating extracted data.</summary>
    Validating = 2,

    /// <summary>Pipeline is transforming validated data.</summary>
    Transforming = 3,

    /// <summary>Pipeline is loading transformed data to the output store.</summary>
    Loading = 4,

    /// <summary>Pipeline run completed successfully.</summary>
    Completed = 5,

    /// <summary>Pipeline run failed.</summary>
    Failed = 6
}
