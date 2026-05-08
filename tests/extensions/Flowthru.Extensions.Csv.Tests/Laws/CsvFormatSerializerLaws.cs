using Flowthru.Extensions.Csv.Tests.Fixtures;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Csv;
using Flowthru.Tests.Kits.Storage;

namespace Flowthru.Extensions.Csv.Tests.Laws;

/// <summary>
/// <see cref="IFormatSerializerLaws{TRow}"/> binding for
/// <see cref="CsvFormatSerializer{TRow}"/> over <see cref="FlatRow"/>.
/// Inherits round-trip and empty-stream laws from the kit.
/// </summary>
[TestFixture]
[Category("Csv")]
public class CsvFormatSerializerLaws_FlatRow : IFormatSerializerLaws<FlatRow>
{
  protected override IFormatSerializer<FlatRow> CreateSerializer() =>
    new CsvFormatSerializer<FlatRow>();

  protected override IEnumerable<FlatRow> SampleRows =>
    new[]
    {
      new FlatRow { Id = 1, Name = "Alice", Value = 1.5 },
      new FlatRow { Id = 2, Name = "Bob", Value = 2.5 },
      new FlatRow { Id = 3, Name = "Charlie", Value = 3.5 },
    };
}

/// <summary>
/// <see cref="IFormatSerializerLaws{TRow}"/> binding for the
/// <c>[SerializedLabel]</c> path. Asserts the label-rewriter
/// round-trips header names verbatim.
/// </summary>
[TestFixture]
[Category("Csv")]
public class CsvFormatSerializerLaws_LabeledRow : IFormatSerializerLaws<LabeledRow>
{
  protected override IFormatSerializer<LabeledRow> CreateSerializer() =>
    new CsvFormatSerializer<LabeledRow>();

  protected override IEnumerable<LabeledRow> SampleRows =>
    new[]
    {
      new LabeledRow { CompanyId = 42, CompanyName = "Acme" },
      new LabeledRow { CompanyId = 99, CompanyName = "TestCo" },
    };
}

/// <summary>
/// <see cref="IFormatSerializerLaws{TRow}"/> binding exercising the
/// <c>[SerializedEnum]</c> chain end-to-end.
/// </summary>
[TestFixture]
[Category("Csv")]
public class CsvFormatSerializerLaws_CheckStatusRow : IFormatSerializerLaws<CheckStatusRow>
{
  protected override IFormatSerializer<CheckStatusRow> CreateSerializer() =>
    new CsvFormatSerializer<CheckStatusRow>();

  protected override IEnumerable<CheckStatusRow> SampleRows =>
    new[]
    {
      new CheckStatusRow { Id = 1, Status = CheckStatus.Complete },
      new CheckStatusRow { Id = 2, Status = CheckStatus.Incomplete },
    };
}
