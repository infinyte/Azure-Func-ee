using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Scenario05.ScheduledEtlPipeline.Models;
using Scenario05.ScheduledEtlPipeline.Services;

namespace Scenario05.ScheduledEtlPipeline.Functions.Activities;

/// <summary>
/// Activity function that transforms validated data records by applying field mappings.
/// </summary>
public sealed class TransformDataActivity
{
    private readonly IDataTransformer _transformer;
    private readonly ILogger<TransformDataActivity> _logger;

    /// <summary>
    /// The default transformation mappings applied to data records.
    /// </summary>
    internal static readonly IReadOnlyList<TransformationMapping> DefaultMappings = new List<TransformationMapping>
    {
        new("name", "fullName", TransformationType.Uppercase),
        new("email", "emailAddress", TransformationType.Lowercase),
        new("amount", "totalAmount", TransformationType.Rename),
        new("category", "tier", TransformationType.Default, "Standard")
    }.AsReadOnly();

    /// <summary>
    /// Initializes a new instance of <see cref="TransformDataActivity"/>.
    /// </summary>
    /// <param name="transformer">The data transformer.</param>
    /// <param name="logger">The logger instance.</param>
    public TransformDataActivity(IDataTransformer transformer, ILogger<TransformDataActivity> logger)
    {
        _transformer = transformer ?? throw new ArgumentNullException(nameof(transformer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Transforms validated data records according to the default mappings.
    /// </summary>
    /// <param name="records">The records to transform.</param>
    /// <returns>The transformed records.</returns>
    [Function("TransformData")]
    public IReadOnlyList<DataRecord> Run([ActivityTrigger] List<DataRecord> records)
    {
        _logger.LogInformation("Transforming {Count} records", records.Count);

        var result = _transformer.Transform(records, DefaultMappings);

        _logger.LogInformation("Transformation complete. Output: {Count} records", result.Count);

        return result;
    }
}
