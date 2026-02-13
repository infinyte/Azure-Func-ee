namespace Scenario05.ScheduledEtlPipeline.Models;

/// <summary>
/// The result of validating extracted data records.
/// </summary>
/// <param name="ValidRecords">Records that passed all validation rules.</param>
/// <param name="InvalidRecords">Records that failed one or more validation rules.</param>
/// <param name="Errors">Validation error messages keyed by record ID.</param>
public sealed record ValidationResult(
    IReadOnlyList<DataRecord> ValidRecords,
    IReadOnlyList<DataRecord> InvalidRecords,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Errors);
