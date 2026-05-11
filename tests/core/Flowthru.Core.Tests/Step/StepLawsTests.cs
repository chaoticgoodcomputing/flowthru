using Flowthru.Data.Catalog;
using Flowthru.Prelude;
using Flowthru.Step;
using Flowthru.Tests.Kits.Step;

namespace Flowthru.Core.Tests.Step;

/// <summary>
/// Concrete <see cref="IStepNodeLaws{TIn, TOut}"/> subclass that
/// binds the standard <see cref="Step{TIn, TOut}"/> implementation to
/// a trivial transform — proves the kit runs and the reference step
/// type satisfies the laws.
/// </summary>
[TestFixture]
public class StepStandardImplLawsTests : IStepNodeLaws<int, int>
{
  protected override int SampleInput => 5;

  protected override IStepNode<int, int> CreateStep()
  {
    var input = ItemFactory.Singleton.Memory<int>("law-input");
    var output = ItemFactory.Singleton.Memory<int>("law-output");
    return new Step<int, int>(
      label: "double",
      transform: x => FlowIO.Pure(x * 2),
      inputs: new IItem[] { input },
      outputs: new IItem[] { output },
      loadInputs: () => input.Load(),
      saveOutputs: result => output.Save(result)
    );
  }
}
