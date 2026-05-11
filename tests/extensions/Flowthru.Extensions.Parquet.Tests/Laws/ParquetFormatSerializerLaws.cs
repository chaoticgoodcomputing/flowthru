using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Parquet;
using Flowthru.Extensions.Parquet.Tests.Fixtures;
using Flowthru.Tests.Kits.Storage;

namespace Flowthru.Extensions.Parquet.Tests.Laws;

/// <summary>
/// <see cref="IFormatSerializerLaws{TRow}"/> binding for
/// <see cref="ParquetFormatSerializer{TRow}"/> over <see cref="FlatRow"/>.
/// Inherits round-trip, empty-stream, and trait/marker drift laws.
/// </summary>
[TestFixture]
[Category("Parquet")]
public class ParquetFormatSerializerLaws_FlatRow : IFormatSerializerLaws<FlatRow>
{
  protected override IFormatSerializer<FlatRow> CreateSerializer() =>
    new ParquetFormatSerializer<FlatRow>();

  protected override IEnumerable<FlatRow> SampleRows => new[]
  {
    new FlatRow { Id = 1, Name = "Alice", Value = 1.5 },
    new FlatRow { Id = 2, Name = "Bob",   Value = 2.5 },
    new FlatRow { Id = 3, Name = "Carol", Value = 3.5 },
  };
}

/// <summary>
/// <see cref="IFormatSerializerLaws{TRow}"/> binding for the
/// <c>[SerializedLabel]</c> path — verifies external column names
/// round-trip verbatim through Parquet's schema metadata.
/// </summary>
[TestFixture]
[Category("Parquet")]
public class ParquetFormatSerializerLaws_LabeledRow : IFormatSerializerLaws<LabeledRow>
{
  protected override IFormatSerializer<LabeledRow> CreateSerializer() =>
    new ParquetFormatSerializer<LabeledRow>();

  protected override IEnumerable<LabeledRow> SampleRows => new[]
  {
    new LabeledRow { CompanyId = 42, CompanyName = "Acme" },
    new LabeledRow { CompanyId = 99, CompanyName = "TestCo" },
  };
}
