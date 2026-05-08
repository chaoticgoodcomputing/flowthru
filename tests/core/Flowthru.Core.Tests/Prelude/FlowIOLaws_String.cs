using Flowthru.Tests.Kits.Prelude;

namespace Flowthru.Core.Tests.Prelude;

/// <summary>
/// Exercises the FlowIO law kit against <see cref="string"/>. Representative
/// string samples cover the basic monad/functor laws and the failure-as-value
/// invariants the kit codifies — over a reference type with non-trivial
/// equality semantics.
/// </summary>
[TestFixture]
public class FlowIOLaws_String : FlowIOLaws<string>
{
  protected override IEnumerable<string> SampleValues =>
    new[] { "", "abc", "with spaces", "unicode: 漢字" };

  protected override bool AreEqual(string a, string b) =>
    string.Equals(a, b, StringComparison.Ordinal);
}
