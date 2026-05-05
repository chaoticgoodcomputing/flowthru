using Flowthru.Core.Effects;
using Flowthru.Core.Steps;

namespace Flowthru.Core.Tests.Execution;

/// <summary>
/// Tests for the runtime metadata resolver that locates source-generated sibling
/// <c>{StepClassName}_Metadata</c> static classes. The fixtures below mimic the shape
/// the generator emits, so the resolver's lookup logic can be tested in isolation
/// without depending on the generator running.
/// </summary>
[TestFixture]
[Category("Execution")]
public class StepMetadataResolverTests
{
  // ─────────────────────────────────────────────────────────────────────────
  // Method group from a class with manually-shaped sibling metadata
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void GetServiceDependencies_MethodGroupWithMetadata_ReturnsServiceTypes()
  {
    Action transform = FakeStepWithMetadata.Run;

    var deps = StepMetadataResolver.GetServiceDependencies(transform);

    Assert.That(
      deps,
      Is.EquivalentTo(
        new ServiceRef[]
        {
          ServiceRef.Of<IFakeService>(),
          ServiceRef.Of<IOtherService>(),
        }
      )
    );
  }

  [Test]
  public void GetTraits_MethodGroupWithMetadata_ReturnsDeclaredTraits()
  {
    Action transform = FakeStepWithMetadata.Run;

    var traits = StepMetadataResolver.GetTraits(transform);

    Assert.Multiple(() =>
    {
      Assert.That(traits.IsIdempotent, Is.True);
      Assert.That(traits.HasSideEffects, Is.True);
    });
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Method group from a class without sibling metadata
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void GetServiceDependencies_MethodGroupWithoutMetadata_ReturnsEmpty()
  {
    Action transform = FakeStepWithoutMetadata.Run;

    var deps = StepMetadataResolver.GetServiceDependencies(transform);

    Assert.That(deps, Is.Empty);
  }

  [Test]
  public void GetTraits_MethodGroupWithoutMetadata_ReturnsDefault()
  {
    Action transform = FakeStepWithoutMetadata.Run;

    var traits = StepMetadataResolver.GetTraits(transform);

    Assert.Multiple(() =>
    {
      Assert.That(traits.IsIdempotent, Is.False);
      Assert.That(traits.HasSideEffects, Is.False);
    });
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Inline lambda — synthesized closure type has no _Metadata sibling
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void GetServiceDependencies_InlineLambda_ReturnsEmpty()
  {
    Action transform = () => { };

    var deps = StepMetadataResolver.GetServiceDependencies(transform);

    Assert.That(deps, Is.Empty);
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Test fixtures: shaped to mimic generator emit
  // ─────────────────────────────────────────────────────────────────────────

  public interface IFakeService { }
  public interface IOtherService { }

  /// <summary>Step factory; sibling _Metadata class lives below.</summary>
  public static class FakeStepWithMetadata
  {
    public static void Run() { }
  }

  /// <summary>Hand-rolled to mimic source-gen output.</summary>
  // ReSharper disable once InconsistentNaming
  public static class FakeStepWithMetadata_Metadata
  {
    public static readonly StepTraits Traits = new(IsIdempotent: true, HasSideEffects: true);

    public static readonly IReadOnlyList<ServiceRef> ServiceDependencies = new ServiceRef[]
    {
      new ServiceRef.CSharp(typeof(IFakeService)),
      new ServiceRef.CSharp(typeof(IOtherService)),
    };
  }

  public static class FakeStepWithoutMetadata
  {
    public static void Run() { }
  }
}
