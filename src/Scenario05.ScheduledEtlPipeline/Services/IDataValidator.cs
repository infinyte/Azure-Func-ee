using Scenario05.ScheduledEtlPipeline.Models;

namespace Scenario05.ScheduledEtlPipeline.Services;

/// <summary>
/// Validates data records against a set of rules.
/// </summary>
public interface IDataValidator
{
    /// <summary>
    /// Validates a collection of data records against the configured validation rules.
    /// </summary>
    /// <param name="records">The records to validate.</param>
    /// <param name="rules">The validation rules to apply.</param>
    /// <returns>A validation result partitioning records into valid and invalid sets.</returns>
    ValidationResult Validate(IReadOnlyList<DataRecord> records, IReadOnlyList<ValidationRule> rules);
}
