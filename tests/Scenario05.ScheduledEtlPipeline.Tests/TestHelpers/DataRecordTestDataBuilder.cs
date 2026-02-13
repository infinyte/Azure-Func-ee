using Scenario05.ScheduledEtlPipeline.Models;

namespace Scenario05.ScheduledEtlPipeline.Tests.TestHelpers;

/// <summary>
/// Test data builder for creating <see cref="DataRecord"/> instances in tests.
/// </summary>
internal sealed class DataRecordTestDataBuilder
{
    private string _sourceName = "TestSource";
    private readonly Dictionary<string, string> _fields = new();

    public DataRecordTestDataBuilder WithSource(string source)
    {
        _sourceName = source;
        return this;
    }

    public DataRecordTestDataBuilder WithField(string key, string value)
    {
        _fields[key] = value;
        return this;
    }

    public DataRecordTestDataBuilder WithStandardFields()
    {
        _fields["name"] = "Test Record";
        _fields["email"] = "test@example.com";
        _fields["amount"] = "100.00";
        _fields["category"] = "Standard";
        return this;
    }

    public DataRecord Build() => new()
    {
        SourceName = _sourceName,
        Fields = new Dictionary<string, string>(_fields)
    };

    public static IReadOnlyList<DataRecord> BuildMany(int count)
    {
        var records = new List<DataRecord>();
        for (var i = 0; i < count; i++)
        {
            records.Add(new DataRecordTestDataBuilder()
                .WithField("name", $"Record {i}")
                .WithField("email", $"user{i}@example.com")
                .WithField("amount", (i * 10.0).ToString("F2"))
                .Build());
        }
        return records.AsReadOnly();
    }
}
