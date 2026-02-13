using Scenario05.ScheduledEtlPipeline.Models;

namespace Scenario05.ScheduledEtlPipeline.Services;

/// <summary>
/// Mapping-based implementation of <see cref="IDataTransformer"/> supporting
/// field rename, uppercase, lowercase, and default value transformations.
/// </summary>
public sealed class MappingDataTransformer : IDataTransformer
{
    /// <inheritdoc />
    public IReadOnlyList<DataRecord> Transform(
        IReadOnlyList<DataRecord> records,
        IReadOnlyList<TransformationMapping> mappings)
    {
        ArgumentNullException.ThrowIfNull(records);
        ArgumentNullException.ThrowIfNull(mappings);

        var results = new List<DataRecord>(records.Count);

        foreach (var record in records)
        {
            var transformed = new DataRecord
            {
                Id = record.Id,
                SourceName = record.SourceName,
                ExtractedAt = record.ExtractedAt
            };

            foreach (var mapping in mappings)
            {
                var sourceValue = record.Fields.GetValueOrDefault(mapping.SourceField);

                var transformedValue = mapping.TransformType switch
                {
                    TransformationType.Rename => sourceValue ?? string.Empty,
                    TransformationType.Uppercase => sourceValue?.ToUpperInvariant() ?? string.Empty,
                    TransformationType.Lowercase => sourceValue?.ToLowerInvariant() ?? string.Empty,
                    TransformationType.Default => string.IsNullOrWhiteSpace(sourceValue)
                        ? mapping.DefaultValue ?? string.Empty
                        : sourceValue,
                    _ => sourceValue ?? string.Empty
                };

                transformed.Fields[mapping.TargetField] = transformedValue;
            }

            results.Add(transformed);
        }

        return results.AsReadOnly();
    }
}
