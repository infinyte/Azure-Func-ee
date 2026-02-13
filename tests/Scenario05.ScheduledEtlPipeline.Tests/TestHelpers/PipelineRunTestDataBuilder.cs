using Scenario05.ScheduledEtlPipeline.Models;

namespace Scenario05.ScheduledEtlPipeline.Tests.TestHelpers;

/// <summary>
/// Test data builder for creating <see cref="PipelineRun"/> instances in tests.
/// </summary>
internal sealed class PipelineRunTestDataBuilder
{
    private string _runId = Guid.NewGuid().ToString();
    private PipelineStatus _status = PipelineStatus.Pending;
    private string _triggerSource = "Test";
    private int _totalExtracted;
    private int _validCount;
    private int _invalidCount;
    private int _loadedCount;
    private string? _errorMessage;

    public PipelineRunTestDataBuilder WithRunId(string runId)
    {
        _runId = runId;
        return this;
    }

    public PipelineRunTestDataBuilder WithStatus(PipelineStatus status)
    {
        _status = status;
        return this;
    }

    public PipelineRunTestDataBuilder WithTriggerSource(string source)
    {
        _triggerSource = source;
        return this;
    }

    public PipelineRunTestDataBuilder WithCounts(int extracted, int valid, int invalid, int loaded)
    {
        _totalExtracted = extracted;
        _validCount = valid;
        _invalidCount = invalid;
        _loadedCount = loaded;
        return this;
    }

    public PipelineRunTestDataBuilder WithError(string errorMessage)
    {
        _errorMessage = errorMessage;
        return this;
    }

    public PipelineRun Build() => new()
    {
        PartitionKey = PipelineRun.DefaultPartitionKey,
        RowKey = _runId,
        RunId = _runId,
        Status = (int)_status,
        StartedAt = DateTimeOffset.UtcNow,
        TriggerSource = _triggerSource,
        TotalRecordsExtracted = _totalExtracted,
        ValidRecordCount = _validCount,
        InvalidRecordCount = _invalidCount,
        RecordsLoaded = _loadedCount,
        ErrorMessage = _errorMessage
    };
}
