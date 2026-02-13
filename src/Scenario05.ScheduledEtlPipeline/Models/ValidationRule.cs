namespace Scenario05.ScheduledEtlPipeline.Models;

/// <summary>
/// Defines a validation rule to apply to data records during the validation stage.
/// </summary>
/// <param name="FieldName">The name of the field to validate.</param>
/// <param name="RuleType">The type of validation (Required, Regex, Range).</param>
/// <param name="RuleValue">The rule parameter (regex pattern, min-max range, etc.).</param>
/// <param name="ErrorMessage">A human-readable message when validation fails.</param>
public sealed record ValidationRule(
    string FieldName,
    ValidationRuleType RuleType,
    string? RuleValue,
    string ErrorMessage);

/// <summary>
/// The type of validation to apply to a field.
/// </summary>
public enum ValidationRuleType
{
    /// <summary>The field must be present and non-empty.</summary>
    Required,

    /// <summary>The field value must match a regular expression pattern.</summary>
    Regex,

    /// <summary>The field value must be a number within a specified range (format: "min-max").</summary>
    Range
}
