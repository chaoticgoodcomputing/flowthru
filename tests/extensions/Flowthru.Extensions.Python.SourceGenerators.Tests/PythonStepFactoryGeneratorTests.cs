using Flowthru.Data.Schema;

namespace Flowthru.Extensions.Python.SourceGenerators.Tests;

/// <summary>
/// Behavioural tests for <see cref="PythonStepFactoryGenerator"/>.
/// The generator discovers <c>.py</c> files via <c>AdditionalFiles</c>,
/// parses every <c>@step(...)</c> decorator, resolves the referenced
/// schemas against <c>[FlowthruSchema]</c> types in the consuming
/// compilation, and emits a strongly-typed factory per discovered
/// step. These tests assert on the emitted source text and on the
/// <see cref="Microsoft.CodeAnalysis.Diagnostic"/> stream the generator
/// produces — the same observable surface every consuming project
/// sees at build time.
/// </summary>
[TestFixture]
public class PythonStepFactoryGeneratorTests
{
  // ── No Python AdditionalFiles ──────────────────────────────────────────

  [Test]
  public void NoPythonAdditionalFiles_EmitsNothing()
  {
    // Without any .py file, the generator's RegisterSourceOutput
    // callback early-returns; consumers shouldn't see a stray empty
    // PythonSteps class polluting their namespace.
    var result = GeneratorTestHarness.Run(
      new PythonStepFactoryGenerator(),
      source: "namespace Sample { public class Empty {} }"
    );

    Assert.That(result.GeneratedSources, Is.Empty,
      "Generator should emit no source when no .py AdditionalFiles are present.");
    Assert.That(result.Diagnostics, Is.Empty);
  }

  [Test]
  public void PythonFileWithoutStepDecorator_EmitsNothing()
  {
    // A .py file that lacks the @step(...) decorator yields a null
    // PythonStepInfo and is filtered out — no factory, no diagnostic.
    var py = new GeneratorTestHarness.InMemoryAdditionalText(
      path: "Flows/Demo/no_step.py",
      text: "def plain_function(x):\n    return x\n"
    );

    var result = GeneratorTestHarness.Run(
      new PythonStepFactoryGenerator(),
      source: "namespace Sample { public class Empty {} }",
      additionalFiles: new[] { py }
    );

    Assert.That(result.GeneratedSources, Is.Empty,
      "A decorator-less .py file must not trigger emission.");
  }

  [Test]
  public void NonPythonAdditionalFile_IsIgnored()
  {
    // Sanity: AdditionalFiles can be anything (.md, .json, etc.).
    // Only the .py suffix is supposed to trigger the parser.
    var notPython = new GeneratorTestHarness.InMemoryAdditionalText(
      path: "Flows/Demo/notes.md",
      text: "@step(inputs=[Foo], outputs=[Bar])\ndef should_not_parse(): pass\n"
    );

    var result = GeneratorTestHarness.Run(
      new PythonStepFactoryGenerator(),
      source: "namespace Sample { public class Empty {} }",
      additionalFiles: new[] { notPython }
    );

    Assert.That(result.GeneratedSources, Is.Empty,
      "Files whose path doesn't end in .py must be ignored.");
  }

  // ── FT2007: unknown schema ────────────────────────────────────────────

  [Test]
  public void UnknownSchemaReferenced_FiresFt2007()
  {
    // The decorator names DefinitelyNotARealSchema, and the consuming
    // compilation has no [FlowthruSchema] type by that name — the
    // generator should raise FT2007 and skip emission of the factory.
    var py = new GeneratorTestHarness.InMemoryAdditionalText(
      path: "Flows/Train/train_model.py",
      text:
        "@step(inputs=[DefinitelyNotARealSchema], outputs=[AlsoNotReal])\n" +
        "def train_model(x):\n    return x\n"
    );

    var result = GeneratorTestHarness.Run(
      new PythonStepFactoryGenerator(),
      source: "namespace Sample { public class Empty {} }",
      additionalFiles: new[] { py }
    );

    Assert.That(result.Diagnostics.Where("FT2007").ToList(), Is.Not.Empty,
      "FT2007 must fire when a decorator references an unknown schema.");
  }

  [Test]
  public void UnknownSchemaReferenced_SkipsFactoryEmission()
  {
    // The contract is "diagnostics > generation": if any schema fails
    // to resolve, the factory itself is skipped (otherwise the user
    // gets a cascading CS error on a method that references an
    // unresolved type, drowning the FT2007 they care about).
    var py = new GeneratorTestHarness.InMemoryAdditionalText(
      path: "Flows/Train/train_model.py",
      text:
        "@step(inputs=[StillMissing], outputs=[AlsoMissing])\n" +
        "def train_model(x):\n    return x\n"
    );

    var result = GeneratorTestHarness.Run(
      new PythonStepFactoryGenerator(),
      source: "namespace Sample { public class Empty {} }",
      additionalFiles: new[] { py }
    );

    if (result.GeneratedSources.TryGetValue("PythonSteps.g.cs", out var emitted))
    {
      Assert.That(emitted, Does.Not.Contain("TrainModel"),
        "Factory must not be emitted when its schemas fail to resolve.");
    }
    // If nothing emits at all, that's also acceptable — the diagnostic
    // is the contract, not the empty PythonSteps class shell.
  }

  // ── Wire-format primitives ────────────────────────────────────────────

  [Test]
  public void WireFormatPrimitives_ResolveWithoutDiagnostic()
  {
    // bytes/str/int/float/bool/object are first-class authoring
    // shortcuts for one-shot Python steps that don't need a schema
    // record. They must NOT trip FT2007.
    var py = new GeneratorTestHarness.InMemoryAdditionalText(
      path: "Flows/Misc/transform.py",
      text:
        "@step(inputs=[bytes], outputs=[str])\n" +
        "def transform(x):\n    return x.decode()\n"
    );

    var result = GeneratorTestHarness.Run(
      new PythonStepFactoryGenerator(),
      source: "namespace Sample { public class Empty {} }",
      additionalFiles: new[] { py }
    );

    Assert.That(result.Diagnostics.Where("FT2007").ToList(), Is.Empty,
      "Wire-format primitives must not raise FT2007.");
    Assert.That(result.GeneratedSources, Does.ContainKey("PythonSteps.g.cs"));
    var emitted = result.GeneratedSources["PythonSteps.g.cs"];
    Assert.That(emitted, Does.Contain("PythonStep<byte[], string>"),
      "bytes→byte[] and str→string mapping should be reflected in the factory signature.");
  }

  [Test]
  public void WireFormatPrimitive_int_MapsToInt()
  {
    var py = new GeneratorTestHarness.InMemoryAdditionalText(
      path: "Flows/Misc/count.py",
      text:
        "@step(inputs=[int], outputs=[int])\n" +
        "def count(x):\n    return x + 1\n"
    );

    var result = GeneratorTestHarness.Run(
      new PythonStepFactoryGenerator(),
      source: "namespace Sample { public class Empty {} }",
      additionalFiles: new[] { py }
    );

    var emitted = result.GeneratedSources["PythonSteps.g.cs"];
    Assert.That(emitted, Does.Contain("PythonStep<int, int>"));
  }

  [Test]
  public void WireFormatPrimitive_float_MapsToDouble()
  {
    // Python `float` is IEEE-754 double precision, so the generator
    // pins it to C# `double` — not `float`.
    var py = new GeneratorTestHarness.InMemoryAdditionalText(
      path: "Flows/Misc/scale.py",
      text:
        "@step(inputs=[float], outputs=[float])\n" +
        "def scale(x):\n    return x * 2.0\n"
    );

    var result = GeneratorTestHarness.Run(
      new PythonStepFactoryGenerator(),
      source: "namespace Sample { public class Empty {} }",
      additionalFiles: new[] { py }
    );

    var emitted = result.GeneratedSources["PythonSteps.g.cs"];
    Assert.That(emitted, Does.Contain("PythonStep<double, double>"),
      "Python `float` must map to C# `double`, not `float`.");
  }

  // ── Valid schema discovery ────────────────────────────────────────────

  [Test]
  public void ValidSchemaInCompilation_EmitsFactoryWithFullyQualifiedReference()
  {
    var py = new GeneratorTestHarness.InMemoryAdditionalText(
      path: "Flows/Train/train_model.py",
      text:
        "@step(inputs=[FeatureVectorSchema], outputs=[ModelWeightsSchema])\n" +
        "def train_model(features):\n    return features\n"
    );

    var consumerSource = """
      using Flowthru.Data.Schema;
      namespace Sample;

      [FlowthruSchema]
      public partial record FeatureVectorSchema
      {
        public required int X { get; init; }
      }

      [FlowthruSchema]
      public partial record ModelWeightsSchema
      {
        public required double W { get; init; }
      }
      """;

    var result = GeneratorTestHarness.Run(
      new PythonStepFactoryGenerator(),
      source: consumerSource,
      additionalFiles: new[] { py },
      assemblyName: "Sample",
      extraReferences: typeof(FlowthruSchemaAttribute).Assembly
    );

    Assert.That(result.Diagnostics.Where("FT2007").ToList(), Is.Empty,
      "Properly-declared schemas must resolve without FT2007.");
    Assert.That(result.GeneratedSources, Does.ContainKey("PythonSteps.g.cs"));
    var emitted = result.GeneratedSources["PythonSteps.g.cs"];
    Assert.That(emitted, Does.Contain("TrainModel"),
      "Factory should be emitted for the snake_case `train_model`, PascalCased to `TrainModel`.");
    Assert.That(emitted, Does.Contain("global::Sample.FeatureVectorSchema"),
      "Resolved schema type should appear fully-qualified.");
    Assert.That(emitted, Does.Contain("IEnumerable<global::Sample.FeatureVectorSchema>"),
      "Schema types are wrapped in IEnumerable<...> in factory signatures.");
  }

  [Test]
  public void QuotedSchemaList_ResolvesSameAsBareIdentifiers()
  {
    // Authors who lift the decorator from Kedro tend to write the
    // schema names as strings; the generator must accept that too.
    var py = new GeneratorTestHarness.InMemoryAdditionalText(
      path: "Flows/Train/train_model.py",
      text:
        "@step(inputs=[\"FeatureVectorSchema\"], outputs=[\"ModelWeightsSchema\"])\n" +
        "def train_model(features):\n    return features\n"
    );

    var consumerSource = """
      using Flowthru.Data.Schema;
      namespace Sample;

      [FlowthruSchema]
      public partial record FeatureVectorSchema
      {
        public required int X { get; init; }
      }

      [FlowthruSchema]
      public partial record ModelWeightsSchema
      {
        public required double W { get; init; }
      }
      """;

    var result = GeneratorTestHarness.Run(
      new PythonStepFactoryGenerator(),
      source: consumerSource,
      additionalFiles: new[] { py },
      assemblyName: "Sample",
      extraReferences: typeof(FlowthruSchemaAttribute).Assembly
    );

    Assert.That(result.Diagnostics.Where("FT2007").ToList(), Is.Empty);
    Assert.That(result.GeneratedSources["PythonSteps.g.cs"],
      Does.Contain("global::Sample.FeatureVectorSchema"));
  }

  [Test]
  public void DottedSchemaName_TakesRightmostSegment()
  {
    // Author writes `Module.FeatureVectorSchema` — the generator
    // should take the rightmost segment for the lookup, matching the
    // Python __qualname__ semantics described in the source's
    // ParseSchemaList comment.
    var py = new GeneratorTestHarness.InMemoryAdditionalText(
      path: "Flows/Train/train_model.py",
      text:
        "@step(inputs=[some.module.FeatureVectorSchema], outputs=[ModelWeightsSchema])\n" +
        "def train_model(features):\n    return features\n"
    );

    var consumerSource = """
      using Flowthru.Data.Schema;
      namespace Sample;

      [FlowthruSchema]
      public partial record FeatureVectorSchema
      {
        public required int X { get; init; }
      }

      [FlowthruSchema]
      public partial record ModelWeightsSchema
      {
        public required double W { get; init; }
      }
      """;

    var result = GeneratorTestHarness.Run(
      new PythonStepFactoryGenerator(),
      source: consumerSource,
      additionalFiles: new[] { py },
      assemblyName: "Sample",
      extraReferences: typeof(FlowthruSchemaAttribute).Assembly
    );

    Assert.That(result.Diagnostics.Where("FT2007").ToList(), Is.Empty,
      "Dotted schema name should be resolved by its rightmost segment.");
  }

  // ── None outputs ──────────────────────────────────────────────────────

  [Test]
  public void NoneOutputs_EmitsObjectReturnType()
  {
    // `outputs=None` is a no-output step; JoinAsTupleOrSingle returns
    // "object" for an empty list, so the emitted PythonStep<TIn, TOut>
    // pins TOut to object.
    var py = new GeneratorTestHarness.InMemoryAdditionalText(
      path: "Flows/Side/sink.py",
      text:
        "@step(inputs=[bytes], outputs=None)\n" +
        "def sink(x):\n    pass\n"
    );

    var result = GeneratorTestHarness.Run(
      new PythonStepFactoryGenerator(),
      source: "namespace Sample { public class Empty {} }",
      additionalFiles: new[] { py }
    );

    Assert.That(result.Diagnostics.Where("FT2007").ToList(), Is.Empty);
    var emitted = result.GeneratedSources["PythonSteps.g.cs"];
    Assert.That(emitted, Does.Contain("PythonStep<byte[], object>"),
      "outputs=None should produce a PythonStep with TOut=object.");
  }

  // ── Module path derivation ────────────────────────────────────────────

  [Test]
  public void ModulePathDerivation_StartsAtFlowsAnchor()
  {
    // The .py file lives at Foo/Bar/Flows/Train/Inner/train.py.
    // DeriveModulePath should anchor at "Flows" and yield
    // Flows.Train.Inner.train (note: trailing .py is stripped).
    var py = new GeneratorTestHarness.InMemoryAdditionalText(
      path: "Foo/Bar/Flows/Train/Inner/train.py",
      text:
        "@step(inputs=[bytes], outputs=[str])\n" +
        "def train(x):\n    return x\n"
    );

    var result = GeneratorTestHarness.Run(
      new PythonStepFactoryGenerator(),
      source: "namespace Sample { public class Empty {} }",
      additionalFiles: new[] { py }
    );

    var emitted = result.GeneratedSources["PythonSteps.g.cs"];
    Assert.That(emitted, Does.Contain("Flows.Train.Inner.train"),
      "Module path should be the dotted suffix from the Flows anchor downward.");
  }

  // ── Snake → Pascal method naming ──────────────────────────────────────

  [Test]
  public void SnakeCaseFunctionName_BecomesPascalCaseMethod()
  {
    // train_model_v2 → TrainModelV2.
    var py = new GeneratorTestHarness.InMemoryAdditionalText(
      path: "Flows/Train/runner.py",
      text:
        "@step(inputs=[bytes], outputs=[str])\n" +
        "def train_model_v2(x):\n    return x\n"
    );

    var result = GeneratorTestHarness.Run(
      new PythonStepFactoryGenerator(),
      source: "namespace Sample { public class Empty {} }",
      additionalFiles: new[] { py }
    );

    var emitted = result.GeneratedSources["PythonSteps.g.cs"];
    Assert.That(emitted, Does.Contain("TrainModelV2"),
      "snake_case function name should be PascalCased for the factory method.");
    Assert.That(emitted, Does.Not.Contain("train_model_v2("),
      "Original snake_case should not leak into the method-name slot.");
  }

  // ── Multi-arity ───────────────────────────────────────────────────────

  [Test]
  public void MultipleInputsAndOutputs_EmitTupleSignature()
  {
    var py = new GeneratorTestHarness.InMemoryAdditionalText(
      path: "Flows/Train/combine.py",
      text:
        "@step(inputs=[bytes, str], outputs=[int, float])\n" +
        "def combine(a, b):\n    return (1, 2.0)\n"
    );

    var result = GeneratorTestHarness.Run(
      new PythonStepFactoryGenerator(),
      source: "namespace Sample { public class Empty {} }",
      additionalFiles: new[] { py }
    );

    var emitted = result.GeneratedSources["PythonSteps.g.cs"];
    Assert.That(emitted, Does.Contain("PythonStep<(byte[], string), (int, double)>"),
      "Multi-input/multi-output steps should use value-tuple signatures.");
    Assert.That(emitted, Does.Contain("input1"));
    Assert.That(emitted, Does.Contain("input2"));
    Assert.That(emitted, Does.Contain("output1"));
    Assert.That(emitted, Does.Contain("output2"));
  }

  [Test]
  public void DecoratorWithServices_StillParsesInputsAndOutputs()
  {
    // The optional `services=[...]` slot must not break the parser.
    var py = new GeneratorTestHarness.InMemoryAdditionalText(
      path: "Flows/Train/run.py",
      text:
        "@step(inputs=[bytes], outputs=[str], services=[\"db\"])\n" +
        "def run(x):\n    return x.decode()\n"
    );

    var result = GeneratorTestHarness.Run(
      new PythonStepFactoryGenerator(),
      source: "namespace Sample { public class Empty {} }",
      additionalFiles: new[] { py }
    );

    Assert.That(result.GeneratedSources, Does.ContainKey("PythonSteps.g.cs"),
      "Generator should still emit when the decorator carries a services= slot.");
    Assert.That(result.GeneratedSources["PythonSteps.g.cs"], Does.Contain("Run"));
  }

  // ── Header sanity ─────────────────────────────────────────────────────

  [Test]
  public void EmittedSource_HasAutoGeneratedHeaderAndNullableEnable()
  {
    var py = new GeneratorTestHarness.InMemoryAdditionalText(
      path: "Flows/Demo/echo.py",
      text:
        "@step(inputs=[bytes], outputs=[str])\n" +
        "def echo(x):\n    return x.decode()\n"
    );

    var result = GeneratorTestHarness.Run(
      new PythonStepFactoryGenerator(),
      source: "namespace Sample { public class Empty {} }",
      additionalFiles: new[] { py }
    );

    var emitted = result.GeneratedSources["PythonSteps.g.cs"];
    Assert.That(emitted, Does.Contain("// <auto-generated/>"),
      "Generated file must carry the <auto-generated/> sentinel so style/lint tools skip it.");
    Assert.That(emitted, Does.Contain("#nullable enable"));
    Assert.That(emitted, Does.Contain("public static class PythonSteps"));
    Assert.That(emitted, Does.Contain("namespace Flowthru.Extensions.Python.Generated"));
  }
}
