using Flowthru.Step;

namespace Flowthru.Core.Tests.Step;

/// <summary>
/// Verifies the <c>StepMetadataGenerator</c> emits a
/// <c>{StepClassName}_Metadata</c> companion next to every
/// <see cref="FlowthruStepAttribute"/>-decorated class, and that the
/// emitted constants reflect the attribute arguments.
/// </summary>
[TestFixture]
public class StepMetadataGeneratorTests
{
  [Test]
  public void DefaultStep_HasGeneratedCompanion_WithDefaultTraits()
  {
    Assert.That(BareStep_Metadata.ClassName, Is.EqualTo("BareStep"));
    Assert.That(BareStep_Metadata.Label, Is.EqualTo("BareStep"),
      "Label defaults to the step class name when not overridden.");
    Assert.That(BareStep_Metadata.Traits.IsIdempotent, Is.False);
    Assert.That(BareStep_Metadata.Traits.HasSideEffects, Is.False);
  }

  [Test]
  public void StepWithAttributeArguments_HasGeneratedCompanionReflectingThem()
  {
    Assert.That(ConfiguredStep_Metadata.ClassName, Is.EqualTo("ConfiguredStep"));
    Assert.That(ConfiguredStep_Metadata.Label, Is.EqualTo("custom-label"),
      "Explicit Label = should flow into the companion.");
    Assert.That(ConfiguredStep_Metadata.Traits.IsIdempotent, Is.True);
    Assert.That(ConfiguredStep_Metadata.Traits.HasSideEffects, Is.True);
  }
}

[FlowthruStep]
public static class BareStep
{
  public static Func<int, int> Create() => x => x;
}

[FlowthruStep(Label = "custom-label", IsIdempotent = true, HasSideEffects = true)]
public static class ConfiguredStep
{
  public static Func<int, int> Create() => x => x;
}
