using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Scenario05.ScheduledEtlPipeline.Models;
using Scenario05.ScheduledEtlPipeline.Services;

namespace Scenario05.ScheduledEtlPipeline.Functions.Activities;

/// <summary>
/// Activity function that validates extracted data records against predefined rules.
/// </summary>
public sealed class ValidateDataActivity
{
    private readonly IDataValidator _validator;
    private readonly ILogger<ValidateDataActivity> _logger;

    /// <summary>
    /// The default validation rules applied to all data records.
    /// </summary>
    internal static readonly IReadOnlyList<ValidationRule> DefaultRules = new List<ValidationRule>
    {
        new("name", ValidationRuleType.Required, null, "Name is required."),
        new("email", ValidationRuleType.Regex, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", "Email must be a valid email address."),
        new("amount", ValidationRuleType.Range, "0-1000000", "Amount must be between 0 and 1,000,000.")
    }.AsReadOnly();

    /// <summary>
    /// Initializes a new instance of <see cref="ValidateDataActivity"/>.
    /// </summary>
    /// <param name="validator">The data validator.</param>
    /// <param name="logger">The logger instance.</param>
    public ValidateDataActivity(IDataValidator validator, ILogger<ValidateDataActivity> logger)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates the extracted data records.
    /// </summary>
    /// <param name="records">The records to validate.</param>
    /// <returns>The validation result with valid and invalid partitions.</returns>
    [Function("ValidateData")]
    public ValidationResult Run([ActivityTrigger] List<DataRecord> records)
    {
        _logger.LogInformation("Validating {Count} records", records.Count);

        var result = _validator.Validate(records, DefaultRules);

        _logger.LogInformation(
            "Validation complete. Valid: {Valid}, Invalid: {Invalid}",
            result.ValidRecords.Count, result.InvalidRecords.Count);

        return result;
    }
}
