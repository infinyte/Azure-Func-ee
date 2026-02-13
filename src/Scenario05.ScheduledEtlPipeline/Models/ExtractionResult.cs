namespace Scenario05.ScheduledEtlPipeline.Models;

/// <summary>
/// The result of extracting data from a single source system.
/// </summary>
/// <param name="SourceName">The name of the source system.</param>
/// <param name="Records">The extracted data records.</param>
/// <param name="IsSuccess">Whether the extraction completed successfully.</param>
/// <param name="ErrorMessage">An error message if the extraction failed.</param>
public sealed record ExtractionResult(
    string SourceName,
    IReadOnlyList<DataRecord> Records,
    bool IsSuccess,
    string? ErrorMessage = null);
