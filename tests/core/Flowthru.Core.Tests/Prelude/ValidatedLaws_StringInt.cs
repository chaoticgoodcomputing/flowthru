using Flowthru.Tests.Kits.Prelude;

namespace Flowthru.Core.Tests.Prelude;

/// <summary>
/// Exercises the Validated law kit with <see cref="string"/> errors and
/// <see cref="int"/> values. Representative samples cover applicative-zip
/// accumulation, monadic-bind short-circuit, functor identity, and the
/// n-ary <c>ZipAll</c> behaviour.
/// </summary>
[TestFixture]
public class ValidatedLaws_StringInt : ValidatedLaws<string, int>
{
  protected override IEnumerable<int> SampleValues =>
    new[] { 0, 1, -1, 42, int.MaxValue };

  protected override IEnumerable<string> SampleErrors =>
    new[] { "missing-input", "schema-drift", "duplicate-producer", "circular-dependency" };
}
