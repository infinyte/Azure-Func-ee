using Scenario05.ScheduledEtlPipeline.Models;

namespace Scenario05.ScheduledEtlPipeline.Services;

/// <summary>
/// Transforms data records by applying field mapping and value transformations.
/// </summary>
public interface IDataTransformer
{
    /// <summary>
    /// Transforms a collection of data records according to the provided mappings.
    /// </summary>
    /// <param name="records">The records to transform.</param>
    /// <param name="mappings">The transformation mappings to apply.</param>
    /// <returns>The transformed data records.</returns>
    IReadOnlyList<DataRecord> Transform(IReadOnlyList<DataRecord> records, IReadOnlyList<TransformationMapping> mappings);
}
