namespace Scenario05.ScheduledEtlPipeline.Models;

/// <summary>
/// Defines a field transformation to apply during the transform stage.
/// </summary>
/// <param name="SourceField">The name of the source field.</param>
/// <param name="TargetField">The name of the target field in the output record.</param>
/// <param name="TransformType">The type of transformation to apply.</param>
/// <param name="DefaultValue">A default value to use when the source field is missing.</param>
public sealed record TransformationMapping(
    string SourceField,
    string TargetField,
    TransformationType TransformType,
    string? DefaultValue = null);

/// <summary>
/// The type of transformation to apply to a field value.
/// </summary>
public enum TransformationType
{
    /// <summary>Copy the value as-is (optionally renaming the field).</summary>
    Rename,

    /// <summary>Convert the value to uppercase.</summary>
    Uppercase,

    /// <summary>Convert the value to lowercase.</summary>
    Lowercase,

    /// <summary>Use a default value if the source field is missing or empty.</summary>
    Default
}
