using Scenario05.ScheduledEtlPipeline.Models;

namespace Scenario05.ScheduledEtlPipeline.Services;

/// <summary>
/// Client for extracting data from an external API source.
/// </summary>
public interface IExternalApiClient
{
    /// <summary>
    /// Extracts data records from the external API.
    /// </summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A list of extracted data records.</returns>
    Task<IReadOnlyList<DataRecord>> ExtractAsync(CancellationToken ct = default);
}
