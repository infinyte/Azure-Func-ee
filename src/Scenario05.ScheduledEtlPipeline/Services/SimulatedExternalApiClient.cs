using Microsoft.Extensions.Logging;
using Scenario05.ScheduledEtlPipeline.Models;

namespace Scenario05.ScheduledEtlPipeline.Services;

/// <summary>
/// Simulated implementation of <see cref="IExternalApiClient"/> that generates mock data
/// for demonstration purposes. In production, this would use HttpClient with resilience policies.
/// </summary>
public sealed class SimulatedExternalApiClient : IExternalApiClient
{
    private readonly ILogger<SimulatedExternalApiClient> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="SimulatedExternalApiClient"/>.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public SimulatedExternalApiClient(ILogger<SimulatedExternalApiClient> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<DataRecord>> ExtractAsync(CancellationToken ct)
    {
        _logger.LogInformation("Extracting data from simulated external API");

        var records = new List<DataRecord>();

        for (var i = 1; i <= 5; i++)
        {
            records.Add(new DataRecord
            {
                SourceName = "ExternalApi",
                Fields = new Dictionary<string, string>
                {
                    ["name"] = $"API Record {i}",
                    ["email"] = $"user{i}@example.com",
                    ["amount"] = (i * 100.50).ToString("F2"),
                    ["category"] = i % 2 == 0 ? "Premium" : "Standard"
                }
            });
        }

        _logger.LogInformation("Extracted {Count} records from external API", records.Count);

        return Task.FromResult<IReadOnlyList<DataRecord>>(records.AsReadOnly());
    }
}
