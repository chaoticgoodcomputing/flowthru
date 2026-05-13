using Flowthru.Data.Schema;
using Flowthru.Flow;

namespace Flowthru.Extensions.Python.SourceGenerators.Tests;

/// <summary>
/// FT2009 closes the C# escape-hatch leg of the Python-step error
/// surface: a <c>builder.AddPythonStep&lt;TIn, TOut&gt;(...)</c>
/// call bypasses the <c>@step</c>-decorator codepath that FT2008
/// audits, so an unmarshallable property reachable from <c>TIn</c>
/// or <c>TOut</c> would otherwise only surface as a wrapped
/// runtime <see cref="System.NotSupportedException"/>. Each test
/// here pins one shape of unwrap the analyzer must understand —
/// <c>IEnumerable&lt;T&gt;</c>, value-tuple packing,
/// <c>DirectoryOf&lt;T&gt;</c>, nested combinations — so the
/// "if it compiles, Arrow can encode it" promise holds across
/// every authoring style.
/// </summary>
[TestFixture]
public class Ft2009AddPythonStepShapeAnalyzerTests
{
  // ── Happy path ────────────────────────────────────────────────────────

  [Test]
  public void Marshallable_TIn_And_TOut_FiresNothing()
  {
    var source = """
      using Flowthru.Data.Catalog;
      using Flowthru.Data.Schema;
      using Flowthru.Flow;
      using Flowthru.Step.Python;
      namespace Sample;

      [FlowthruSchema]
      public partial record GoodIn { public required int Id { get; init; } }

      [FlowthruSchema]
      public partial record GoodOut { public required string Label { get; init; } }

      public static class Pipeline
      {
        public static void Configure(FlowBuilder builder, IItem<GoodIn> input, IItem<GoodOut> output, IPythonExecutor exec)
        {
          builder.AddPythonStep<GoodIn, GoodOut>("step", "mod", "fn", input, output, exec);
        }
      }
      """;

    var result = GeneratorTestHarness.Run(
      new PythonStepFactoryGenerator(),
      source: source,
      assemblyName: "Sample",
      extraReferences: new[]
      {
        typeof(FlowthruSchemaAttribute).Assembly,
        typeof(FlowBuilder).Assembly,
        typeof(global::Flowthru.Flow.PythonStepFactory).Assembly,
      }
    );

    Assert.That(result.Diagnostics.Where("FT2009").ToList(), Is.Empty,
      "Both type arguments only carry marshallable properties; FT2009 must stay silent.");
  }

  // ── TIn unmarshallable ────────────────────────────────────────────────

  [Test]
  public void TIn_With_Unmarshallable_Property_FiresFt2009()
  {
    var source = """
      using System;
      using Flowthru.Data.Catalog;
      using Flowthru.Data.Schema;
      using Flowthru.Flow;
      using Flowthru.Step.Python;
      namespace Sample;

      [FlowthruSchema]
      public partial record BadIn { public required IntPtr Handle { get; init; } }

      [FlowthruSchema]
      public partial record GoodOut { public required string Label { get; init; } }

      public static class Pipeline
      {
        public static void Configure(FlowBuilder builder, IItem<BadIn> input, IItem<GoodOut> output, IPythonExecutor exec)
        {
          builder.AddPythonStep<BadIn, GoodOut>("step", "mod", "fn", input, output, exec);
        }
      }
      """;

    var result = GeneratorTestHarness.Run(
      new PythonStepFactoryGenerator(),
      source: source,
      assemblyName: "Sample",
      extraReferences: new[]
      {
        typeof(FlowthruSchemaAttribute).Assembly,
        typeof(FlowBuilder).Assembly,
        typeof(global::Flowthru.Flow.PythonStepFactory).Assembly,
      }
    );

    var ft2009 = result.Diagnostics.Where("FT2009").ToList();
    Assert.That(ft2009, Is.Not.Empty,
      "FT2009 must fire when a TIn slot reaches an unmarshallable property.");
    Assert.That(ft2009[0].GetMessage(), Does.Contain("Handle"),
      "Diagnostic must name the offending property.");
  }

  // ── TOut unmarshallable ───────────────────────────────────────────────

  [Test]
  public void TOut_With_Unmarshallable_Property_FiresFt2009()
  {
    var source = """
      using System;
      using Flowthru.Data.Catalog;
      using Flowthru.Data.Schema;
      using Flowthru.Flow;
      using Flowthru.Step.Python;
      namespace Sample;

      [FlowthruSchema]
      public partial record GoodIn { public required int Id { get; init; } }

      [FlowthruSchema]
      public partial record BadOut { public required IntPtr Ptr { get; init; } }

      public static class Pipeline
      {
        public static void Configure(FlowBuilder builder, IItem<GoodIn> input, IItem<BadOut> output, IPythonExecutor exec)
        {
          builder.AddPythonStep<GoodIn, BadOut>("step", "mod", "fn", input, output, exec);
        }
      }
      """;

    var result = GeneratorTestHarness.Run(
      new PythonStepFactoryGenerator(),
      source: source,
      assemblyName: "Sample",
      extraReferences: new[]
      {
        typeof(FlowthruSchemaAttribute).Assembly,
        typeof(FlowBuilder).Assembly,
        typeof(global::Flowthru.Flow.PythonStepFactory).Assembly,
      }
    );

    var ft2009 = result.Diagnostics.Where("FT2009").ToList();
    Assert.That(ft2009, Is.Not.Empty,
      "FT2009 must fire when a TOut slot reaches an unmarshallable property.");
    Assert.That(ft2009[0].GetMessage(), Does.Contain("Ptr"));
  }

  // ── Tabular input via IEnumerable<T> ──────────────────────────────────

  [Test]
  public void TIn_As_IEnumerable_Of_BadSchema_FiresFt2009()
  {
    var source = """
      using System;
      using System.Collections.Generic;
      using Flowthru.Data.Catalog;
      using Flowthru.Data.Schema;
      using Flowthru.Flow;
      using Flowthru.Step.Python;
      namespace Sample;

      [FlowthruSchema]
      public partial record BadRow { public required IntPtr Handle { get; init; } }

      [FlowthruSchema]
      public partial record GoodOut { public required int N { get; init; } }

      public static class Pipeline
      {
        public static void Configure(FlowBuilder builder, IItem<IEnumerable<BadRow>> input, IItem<GoodOut> output, IPythonExecutor exec)
        {
          builder.AddPythonStep<IEnumerable<BadRow>, GoodOut>("step", "mod", "fn", input, output, exec);
        }
      }
      """;

    var result = GeneratorTestHarness.Run(
      new PythonStepFactoryGenerator(),
      source: source,
      assemblyName: "Sample",
      extraReferences: new[]
      {
        typeof(FlowthruSchemaAttribute).Assembly,
        typeof(FlowBuilder).Assembly,
        typeof(global::Flowthru.Flow.PythonStepFactory).Assembly,
      }
    );

    Assert.That(result.Diagnostics.Where("FT2009").ToList(), Is.Not.Empty,
      "FT2009 must unwrap IEnumerable<T> and still flag unmarshallable schema properties.");
  }

  // ── ValueTuple TIn ────────────────────────────────────────────────────

  [Test]
  public void TIn_As_ValueTuple_With_Bad_Position_FiresFt2009()
  {
    var source = """
      using System;
      using Flowthru.Data.Catalog;
      using Flowthru.Data.Schema;
      using Flowthru.Flow;
      using Flowthru.Step.Python;
      namespace Sample;

      [FlowthruSchema]
      public partial record GoodA { public required int X { get; init; } }

      [FlowthruSchema]
      public partial record BadB { public required IntPtr P { get; init; } }

      [FlowthruSchema]
      public partial record GoodOut { public required int N { get; init; } }

      public static class Pipeline
      {
        public static void Configure(FlowBuilder builder, IItem<GoodA> a, IItem<BadB> b, IItem<GoodOut> output, IPythonExecutor exec)
        {
          builder.AddPythonStep<GoodA, BadB, GoodOut>("step", "mod", "fn", (a, b), output, exec);
        }
      }
      """;

    var result = GeneratorTestHarness.Run(
      new PythonStepFactoryGenerator(),
      source: source,
      assemblyName: "Sample",
      extraReferences: new[]
      {
        typeof(FlowthruSchemaAttribute).Assembly,
        typeof(FlowBuilder).Assembly,
        typeof(global::Flowthru.Flow.PythonStepFactory).Assembly,
      }
    );

    var ft2009 = result.Diagnostics.Where("FT2009").ToList();
    Assert.That(ft2009, Is.Not.Empty,
      "FT2009 must flag the bad slot when a value-tuple TIn mixes good and bad schemas.");
    Assert.That(ft2009[0].GetMessage(), Does.Contain("P"),
      "Diagnostic must point at the unmarshallable property in the bad slot.");
  }

  // ── DirectoryOf<T> input ──────────────────────────────────────────────

  [Test]
  public void TIn_As_DirectoryOf_BadSchema_FiresFt2009()
  {
    var source = """
      using System;
      using Flowthru.Data.Catalog;
      using Flowthru.Data.Schema;
      using Flowthru.Data.Storage;
      using Flowthru.Flow;
      using Flowthru.Step.Python;
      namespace Sample;

      [FlowthruSchema]
      public partial record BadDir { public required IntPtr P { get; init; } }

      [FlowthruSchema]
      public partial record GoodOut { public required int N { get; init; } }

      public static class Pipeline
      {
        public static void Configure(FlowBuilder builder, IItem<DirectoryOf<BadDir>> input, IItem<GoodOut> output, IPythonExecutor exec)
        {
          builder.AddPythonStep<DirectoryOf<BadDir>, GoodOut>("step", "mod", "fn", input, output, exec);
        }
      }
      """;

    var result = GeneratorTestHarness.Run(
      new PythonStepFactoryGenerator(),
      source: source,
      assemblyName: "Sample",
      extraReferences: new[]
      {
        typeof(FlowthruSchemaAttribute).Assembly,
        typeof(FlowBuilder).Assembly,
        typeof(global::Flowthru.Flow.PythonStepFactory).Assembly,
      }
    );

    Assert.That(result.Diagnostics.Where("FT2009").ToList(), Is.Not.Empty,
      "FT2009 must unwrap DirectoryOf<T> to its schema and flag unmarshallable properties on it.");
  }

  // ── Nested IEnumerable<(Good, Bad)> ───────────────────────────────────

  [Test]
  public void TIn_As_IEnumerable_Of_ValueTuple_With_BadSlot_FiresFt2009()
  {
    var source = """
      using System;
      using System.Collections.Generic;
      using Flowthru.Data.Catalog;
      using Flowthru.Data.Schema;
      using Flowthru.Flow;
      using Flowthru.Step.Python;
      namespace Sample;

      [FlowthruSchema]
      public partial record GoodA { public required int X { get; init; } }

      [FlowthruSchema]
      public partial record BadB { public required IntPtr P { get; init; } }

      [FlowthruSchema]
      public partial record GoodOut { public required int N { get; init; } }

      public static class Pipeline
      {
        public static void Configure(FlowBuilder builder, IItem<IEnumerable<(GoodA, BadB)>> input, IItem<GoodOut> output, IPythonExecutor exec)
        {
          builder.AddPythonStep<IEnumerable<(GoodA, BadB)>, GoodOut>("step", "mod", "fn", input, output, exec);
        }
      }
      """;

    var result = GeneratorTestHarness.Run(
      new PythonStepFactoryGenerator(),
      source: source,
      assemblyName: "Sample",
      extraReferences: new[]
      {
        typeof(FlowthruSchemaAttribute).Assembly,
        typeof(FlowBuilder).Assembly,
        typeof(global::Flowthru.Flow.PythonStepFactory).Assembly,
      }
    );

    Assert.That(result.Diagnostics.Where("FT2009").ToList(), Is.Not.Empty,
      "FT2009 must walk IEnumerable<(A, B)> down into B and flag its unmarshallable property.");
  }

  // ── FT2008 + FT2009 coexistence ───────────────────────────────────────

  [Test]
  public void Schema_Used_By_Both_Decorator_And_AddPythonStep_FiresBoth()
  {
    // FT2008 fires from the @step decorator pass; FT2009 fires from the
    // AddPythonStep call-site pass. Pin the actual behaviour — both
    // diagnostics surface, one per authoring entry point, so the
    // developer sees both lanes lit up if both paths reference the same
    // broken schema.
    var py = new GeneratorTestHarness.InMemoryAdditionalText(
      path: "Flows/Demo/run.py",
      text:
        "@step(inputs=[BadSchema], outputs=[BadSchema])\n" +
        "def run(rows):\n    return rows\n"
    );

    var source = """
      using System;
      using Flowthru.Data.Catalog;
      using Flowthru.Data.Schema;
      using Flowthru.Flow;
      using Flowthru.Step.Python;
      namespace Sample;

      [FlowthruSchema]
      public partial record BadSchema { public required IntPtr Handle { get; init; } }

      public static class Pipeline
      {
        public static void Configure(FlowBuilder builder, IItem<BadSchema> input, IItem<BadSchema> output, IPythonExecutor exec)
        {
          builder.AddPythonStep<BadSchema, BadSchema>("step", "mod", "fn", input, output, exec);
        }
      }
      """;

    var result = GeneratorTestHarness.Run(
      new PythonStepFactoryGenerator(),
      source: source,
      additionalFiles: new[] { py },
      assemblyName: "Sample",
      extraReferences: new[]
      {
        typeof(FlowthruSchemaAttribute).Assembly,
        typeof(FlowBuilder).Assembly,
        typeof(global::Flowthru.Flow.PythonStepFactory).Assembly,
      }
    );

    Assert.That(result.Diagnostics.Where("FT2008").ToList(), Is.Not.Empty,
      "@step decorator path still fires FT2008.");
    Assert.That(result.Diagnostics.Where("FT2009").ToList(), Is.Not.Empty,
      "AddPythonStep call-site path additionally fires FT2009.");
  }
}
