using Flowthru.Data.Schema;
using Flowthru.Data.Storage;
using Flowthru.Tests.Kits.Storage;

namespace Flowthru.Core.Tests.Storage;

/// <summary>
/// Exercises <see cref="IFormatSerializerLaws{TRow}"/> against
/// <see cref="JsonFormatSerializer{TRow}"/> over a flat schema.
/// </summary>
[TestFixture]
public class JsonFormatSerializerLaws_TestRow : IFormatSerializerLaws<TestRow>
{
  protected override IFormatSerializer<TestRow> CreateSerializer() =>
    new JsonFormatSerializer<TestRow>();

  protected override IEnumerable<TestRow> SampleRows =>
    new[]
    {
      new TestRow { Id = 1, Name = "alpha" },
      new TestRow { Id = 2, Name = "beta" },
      new TestRow { Id = 3, Name = "gamma" },
    };
}
