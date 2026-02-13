using System.Globalization;
using System.Text.RegularExpressions;
using Scenario05.ScheduledEtlPipeline.Models;

namespace Scenario05.ScheduledEtlPipeline.Services;

/// <summary>
/// Rule-based implementation of <see cref="IDataValidator"/> supporting Required, Regex, and Range rules.
/// </summary>
public sealed class RuleBasedDataValidator : IDataValidator
{
    /// <inheritdoc />
    public ValidationResult Validate(IReadOnlyList<DataRecord> records, IReadOnlyList<ValidationRule> rules)
    {
        ArgumentNullException.ThrowIfNull(records);
        ArgumentNullException.ThrowIfNull(rules);

        var validRecords = new List<DataRecord>();
        var invalidRecords = new List<DataRecord>();
        var errors = new Dictionary<string, IReadOnlyList<string>>();

        foreach (var record in records)
        {
            var recordErrors = new List<string>();

            foreach (var rule in rules)
            {
                var fieldValue = record.Fields.GetValueOrDefault(rule.FieldName);
                var isValid = rule.RuleType switch
                {
                    ValidationRuleType.Required => !string.IsNullOrWhiteSpace(fieldValue),
                    ValidationRuleType.Regex => ValidateRegex(fieldValue, rule.RuleValue),
                    ValidationRuleType.Range => ValidateRange(fieldValue, rule.RuleValue),
                    _ => true
                };

                if (!isValid)
                {
                    recordErrors.Add(rule.ErrorMessage);
                }
            }

            if (recordErrors.Count == 0)
            {
                validRecords.Add(record);
            }
            else
            {
                invalidRecords.Add(record);
                errors[record.Id] = recordErrors.AsReadOnly();
            }
        }

        return new ValidationResult(
            validRecords.AsReadOnly(),
            invalidRecords.AsReadOnly(),
            errors);
    }

    private static bool ValidateRegex(string? value, string? pattern)
    {
        if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(pattern))
        {
            return false;
        }

        return Regex.IsMatch(value, pattern, RegexOptions.None, TimeSpan.FromSeconds(1));
    }

    private static bool ValidateRange(string? value, string? rangeSpec)
    {
        if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(rangeSpec))
        {
            return false;
        }

        var parts = rangeSpec.Split('-', 2);
        if (parts.Length != 2)
        {
            return false;
        }

        if (!double.TryParse(value, CultureInfo.InvariantCulture, out var numericValue) ||
            !double.TryParse(parts[0], CultureInfo.InvariantCulture, out var min) ||
            !double.TryParse(parts[1], CultureInfo.InvariantCulture, out var max))
        {
            return false;
        }

        return numericValue >= min && numericValue <= max;
    }
}
