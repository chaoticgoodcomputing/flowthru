using Flowthru.Data.Schema;

namespace Flowthru.Extensions.Python.SourceGenerators.Tests;

/// <summary>
/// FT2008 closes the loop on CONTRIBUTING.md's three-error-phase model
/// for Python step schemas: a property whose CLR type the Arrow
/// marshaller cannot handle should be a build error, not a delayed
/// runtime <see cref="System.NotSupportedException"/> wrapped behind
/// a useless reflection-invocation envelope.
/// </summary>
[TestFixture]
public class Ft2008PythonSchemaMarshallabilityTests
{
  [Test]
  public void Schema_With_IntPtr_Property_Used_By_Python_Step_FiresFt2008()
  {
    // IntPtr has no Arrow encoding: it isn't in the shared
    // marshallable-leaf set and doesn't match any recursive shape
    // (nullable, enum, array, IEnumerable<>).
    var py = new GeneratorTestHarness.InMemoryAdditionalText(
      path: "Flows/Sales/aggregate.py",
      text:
        "@step(inputs=[SaleSchema], outputs=[SaleSchema])\n" +
        "def aggregate(rows):\n    return rows\n"
    );

    var consumerSource = """
      using System;
      using Flowthru.Data.Schema;
      namespace Sample;

      [FlowthruSchema]
      public partial record SaleSchema
      {
        public required int Id { get; init; }
        public required IntPtr Handle { get; init; }
      }
      """;

    var result = GeneratorTestHarness.Run(
      new PythonStepFactoryGenerator(),
      source: consumerSource,
      additionalFiles: new[] { py },
      assemblyName: "Sample",
      extraReferences: typeof(FlowthruSchemaAttribute).Assembly
    );

    var ft2008 = result.Diagnostics.Where("FT2008").ToList();
    Assert.That(ft2008, Is.Not.Empty,
      "FT2008 must fire when a Python-step schema declares a property the marshaller can't handle.");
    Assert.That(ft2008[0].GetMessage(), Does.Contain("Handle"),
      "Diagnostic must name the offending property.");
    // Roslyn's default ToDisplayString renders IntPtr as the C# alias
    // `nint`; either rendering is acceptable as a fix-path signal.
    var typeFragment = ft2008[0].GetMessage();
    Assert.That(typeFragment, Does.Contain("nint").IgnoreCase.Or.Contain("IntPtr").IgnoreCase,
      "Diagnostic must name the offending type so the fix path is obvious.");
  }

  [Test]
  public void Schema_With_StringArray_Property_DoesNotFireFt2008()
  {
    // The bug-report's exact case: string[] keywords. Before list
    // support shipped this would have been the right place to warn —
    // now it's a supported type and FT2008 must stay silent.
    var py = new GeneratorTestHarness.InMemoryAdditionalText(
      path: "Flows/Clustering/labels.py",
      text:
        "@step(inputs=[ClusterLabel], outputs=[ClusterLabel])\n" +
        "def label(rows):\n    return rows\n"
    );

    var consumerSource = """
      using Flowthru.Data.Schema;
      namespace Sample;

      [FlowthruSchema]
      public partial record ClusterLabel
      {
        public required int ClusterId { get; init; }
        public required string Label { get; init; }
        public required string[] Keywords { get; init; }
        public required int Size { get; init; }
      }
      """;

    var result = GeneratorTestHarness.Run(
      new PythonStepFactoryGenerator(),
      source: consumerSource,
      additionalFiles: new[] { py },
      assemblyName: "Sample",
      extraReferences: typeof(FlowthruSchemaAttribute).Assembly
    );

    Assert.That(result.Diagnostics.Where("FT2008").ToList(), Is.Empty,
      "string[] must round-trip through ListArray<String> without raising FT2008.");
  }

  [Test]
  public void Schema_With_NestedList_DoesNotFireFt2008()
  {
    // Recursive list element check: List<List<int>> walks two levels
    // and both must satisfy the marshallable set.
    var py = new GeneratorTestHarness.InMemoryAdditionalText(
      path: "Flows/Matrix/build.py",
      text:
        "@step(inputs=[MatrixRow], outputs=[MatrixRow])\n" +
        "def build(rows):\n    return rows\n"
    );

    var consumerSource = """
      using Flowthru.Data.Schema;
      using System.Collections.Generic;
      namespace Sample;

      [FlowthruSchema]
      public partial record MatrixRow
      {
        public required int Id { get; init; }
        public required List<List<int>> Cells { get; init; }
      }
      """;

    var result = GeneratorTestHarness.Run(
      new PythonStepFactoryGenerator(),
      source: consumerSource,
      additionalFiles: new[] { py },
      assemblyName: "Sample",
      extraReferences: typeof(FlowthruSchemaAttribute).Assembly
    );

    Assert.That(result.Diagnostics.Where("FT2008").ToList(), Is.Empty,
      "Nested lists of supported scalar elements must satisfy FT2008's check.");
  }
}
