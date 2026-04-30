using Flowthru.Tests.Helpers;

namespace Flowthru.Core.SourceGenerators.Tests.Compilation.TypeSafety;

/// <summary>
/// Tests for the <c>StepMetadataGenerator</c>: scans <c>[FlowthruStep]</c>-attributed
/// classes and emits sibling <c>{StepClassName}_Metadata</c> static classes carrying
/// <c>StepTraits</c> and <c>ServiceDependencies</c>.
/// </summary>
[TestFixture]
[Category("Compilation")]
[Category("SourceGenerator")]
public class StepMetadataGeneratorTests
{
  // ─────────────────────────────────────────────────────────────────────────
  // Empty Create() → empty ServiceDependencies
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void StepWithEmptyCreate_EmitsEmptyServiceDependencies()
  {
    var source = """
      using Flowthru.Core.Steps;

      namespace TestProject;

      [FlowthruStep]
      public static class PureStep
      {
          public static System.Func<int, int> Create() => x => x;
      }
      """;

    var result = GeneratorTestHelper.RunStepMetadataGenerator(source);
    var generated = result.GetGeneratedSource("PureStep.StepMetadata.g.cs");

    Assert.That(generated, Is.Not.Null);
    Assert.That(generated, Does.Contain("class PureStep_Metadata"));
    Assert.That(generated, Does.Contain("Array.Empty<Type>()"));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Create(IService) → [typeof(IService)]
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void StepWithSingleServiceParam_EmitsServiceTypeInDependencies()
  {
    var source = """
      using Flowthru.Core.Steps;

      namespace TestProject;

      public interface IMyService { }

      [FlowthruStep]
      public static class ServiceStep
      {
          public static System.Func<int, int> Create(IMyService svc) => x => x;
      }
      """;

    var result = GeneratorTestHelper.RunStepMetadataGenerator(source);
    var generated = result.GetGeneratedSource("ServiceStep.StepMetadata.g.cs");

    Assert.That(generated, Is.Not.Null);
    Assert.That(generated, Does.Contain("typeof(global::TestProject.IMyService)"));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Create(ILogger<T>) → empty (logger is in the allow-list)
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void StepWithILoggerParam_AllowsListsLoggerOut()
  {
    var source = """
      using Flowthru.Core.Steps;
      using Microsoft.Extensions.Logging;

      namespace TestProject;

      [FlowthruStep]
      public static class LoggingStep
      {
          public static System.Func<int, int> Create(ILogger<LoggingStep> logger) => x => x;
      }
      """;

    var result = GeneratorTestHelper.RunStepMetadataGenerator(source);
    var generated = result.GetGeneratedSource("LoggingStep.StepMetadata.g.cs");

    Assert.That(generated, Is.Not.Null);
    Assert.That(generated, Does.Contain("Array.Empty<Type>()"));
    Assert.That(generated, Does.Not.Contain("typeof(Microsoft.Extensions.Logging.ILogger"));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Create(string id, IService svc) → only the service type
  // (string is a primitive, not an interface — naturally excluded)
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void StepWithMixedParams_EmitsOnlyInterfaceServiceTypes()
  {
    var source = """
      using Flowthru.Core.Steps;

      namespace TestProject;

      public interface IMyService { }

      [FlowthruStep]
      public static class MixedStep
      {
          public static System.Func<int, int> Create(string id, IMyService svc) => x => x;
      }
      """;

    var result = GeneratorTestHelper.RunStepMetadataGenerator(source);
    var generated = result.GetGeneratedSource("MixedStep.StepMetadata.g.cs");

    Assert.That(generated, Is.Not.Null);
    Assert.That(generated, Does.Contain("typeof(global::TestProject.IMyService)"));
    // 'string' (primitive) should be excluded; only the interface should appear in the
    // emitted Type[] payload.
    Assert.That(generated, Does.Not.Contain("typeof(string)"));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Attribute parameters propagate to Traits
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void StepWithDeclaredTraits_PropagatesToMetadata()
  {
    var source = """
      using Flowthru.Core.Steps;

      namespace TestProject;

      [FlowthruStep(IsIdempotent = true, HasSideEffects = true)]
      public static class TraitedStep
      {
          public static System.Func<int, int> Create() => x => x;
      }
      """;

    var result = GeneratorTestHelper.RunStepMetadataGenerator(source);
    var generated = result.GetGeneratedSource("TraitedStep.StepMetadata.g.cs");

    Assert.That(generated, Is.Not.Null);
    Assert.That(generated, Does.Contain("IsIdempotent: true"));
    Assert.That(generated, Does.Contain("HasSideEffects: true"));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Default attribute (no traits) emits both as false
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void StepWithoutDeclaredTraits_EmitsDefaultsAsFalse()
  {
    var source = """
      using Flowthru.Core.Steps;

      namespace TestProject;

      [FlowthruStep]
      public static class DefaultTraitsStep
      {
          public static System.Func<int, int> Create() => x => x;
      }
      """;

    var result = GeneratorTestHelper.RunStepMetadataGenerator(source);
    var generated = result.GetGeneratedSource("DefaultTraitsStep.StepMetadata.g.cs");

    Assert.That(generated, Is.Not.Null);
    Assert.That(generated, Does.Contain("IsIdempotent: false"));
    Assert.That(generated, Does.Contain("HasSideEffects: false"));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Tuple/record param types are NOT classified as services
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void StepWithTupleParam_ExcludesTupleFromServiceDependencies()
  {
    var source = """
      using Flowthru.Core.Steps;
      using System.Collections.Generic;

      namespace TestProject;

      public record Options(int Threshold);

      [FlowthruStep]
      public static class TupleParamStep
      {
          public static System.Func<int, int>
              Create((IEnumerable<int> Data, Options Opts) input) => x => x;
      }
      """;

    var result = GeneratorTestHelper.RunStepMetadataGenerator(source);
    var generated = result.GetGeneratedSource("TupleParamStep.StepMetadata.g.cs");

    Assert.That(generated, Is.Not.Null);
    // Tuples and records aren't interfaces — neither should appear.
    Assert.That(generated, Does.Contain("Array.Empty<Type>()"));
  }
}
