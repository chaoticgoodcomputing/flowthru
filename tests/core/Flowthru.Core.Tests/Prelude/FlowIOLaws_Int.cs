using Flowthru.Tests.Kits.Prelude;

namespace Flowthru.Core.Tests.Prelude;

/// <summary>
/// Exercises the FlowIO law kit against <see cref="int"/>. Representative
/// integer samples cover the basic monad/functor laws and the
/// failure-as-value invariants the kit codifies.
/// </summary>
[TestFixture]
public class FlowIOLaws_Int : FlowIOLaws<int>
{
  protected override IEnumerable<int> SampleValues =>
    new[] { 0, 1, -1, 42, int.MaxValue };
}
