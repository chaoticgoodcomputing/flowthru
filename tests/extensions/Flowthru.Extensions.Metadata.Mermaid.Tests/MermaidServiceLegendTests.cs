using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Diagnostics;
using Flowthru.Diagnostics.Mermaid;
using Flowthru.Diagnostics.Mermaid.Internal;
using Flowthru.Flow;
using Flowthru.Prelude;
using Flowthru.Step;
using Flowthru.Validation.Runtime;

namespace Flowthru.Extensions.Metadata.Mermaid.Tests;

/// <summary>
/// Coverage for the service-legend rendering (ADR-0019, #100 s7): services
/// annotate nodes inline (node-generic across steps and item cylinders) and
/// appear once in a distinct-coloured <c>services</c> legend subgraph with
/// capacity / cacheability metadata. The source-language parenthetical is
/// collapsed — a step's language is read from its executor service, not a tag.
/// </summary>
[TestFixture]
[Category("Metadata.Mermaid")]
public class MermaidServiceLegendTests
{
  private interface IExecutorMarker { }

  /// <summary>Capacity 1, cache-neutral for <see cref="IExecutorMarker"/>.</summary>
  private sealed class ExecutorProfileProvider : IServiceProfileProvider
  {
    private static readonly string Id = ServiceDependency.Of<IExecutorMarker>().DagId;
    public ServiceProfile Resolve(ServiceDependency dependency) =>
      dependency.DagId == Id
        ? new ServiceProfile { Capacity = 1, AffectsOutputs = false }
        : ServiceProfile.Unbounded;
  }

  [Test]
  public void RenderDag_StepWithService_EmitsLegendAndInline_NoLanguageTag()
  {
    var root = ItemFactory.Singleton.Memory<int>("leg-root");
    var output = ItemFactory.Singleton.Memory<int>("leg-out");
    var step = new PythonishStep("transform", root, output,
      new[] { ServiceDependency.Of<IExecutorMarker>() });

    var flow = FlowBuilder.CreateFlow("legend-demo", b => b.Add(step));
    var ctx = FlowMetadataContext.Unsliced(flow) with { ServiceProfiles = new ExecutorProfileProvider() };

    var diagram = MermaidDiagramRenderer.RenderDag(
      ctx, showFullDag: true,
      direction: MermaidFlowchartDirection.TopToBottom,
      theme: MermaidDiagramRenderer.Theme.Default);

    Assert.Multiple(() =>
    {
      // Inline compartment on the step (membership).
      Assert.That(diagram, Does.Contain("<br>──<br>IExecutorMarker"),
        "The step node keeps its inline service compartment.");
      // Legend subgraph (metadata key).
      Assert.That(diagram, Does.Contain("subgraph service_legend[\"services\"]"),
        "A services legend subgraph is emitted.");
      Assert.That(diagram, Does.Contain("• cap: 1"), "The legend node shows the capacity bullet.");
      Assert.That(diagram, Does.Contain("• cache: neutral"),
        "A cache-neutral service shows 'cache: neutral'.");
      // Configurable legend colour applied.
      Assert.That(diagram, Does.Contain("style service_legend fill:"),
        "The legend subgraph carries the configurable accent fill.");
      // No step→service edges (implicit name-join).
      Assert.That(diagram, Does.Not.Contain("-.uses.->"), "No step→service edges.");
      // Source-language parenthetical collapsed.
      Assert.That(diagram, Does.Not.Contain("(python)"),
        "The (python) source-language tag is collapsed — read from the executor service instead.");
    });

    TestContext.Out.WriteLine(diagram);
  }

  [Test]
  public void SanitizeId_StripsResourceDagIdPunctuation()
  {
    // Service DagIds carry ': | / +' (e.g. "efcore:Sqlite|/tmp/x.db/main") —
    // the legend keys nodes by DagId, so all of it must reduce to a valid id.
    var id = MermaidDiagramRenderer.SanitizeId("efcore:Microsoft.Sqlite|/tmp/x.db/main");
    Assert.That(id, Is.EqualTo("efcore_Microsoft_Sqlite__tmp_x_db_main"));
    Assert.That(System.Text.RegularExpressions.Regex.IsMatch(id, "^[A-Za-z0-9_]+$"), Is.True,
      "A sanitized id must be Mermaid-safe (alphanumerics and underscores only).");
  }

  [Test]
  public void ItemNodeSyntax_ItemWithServiceDependency_AnnotatesResourceNodeGenerically()
  {
    var item = new ResourceBackedItem("Catalog.Metrics",
      new ServiceDependency.External(new FakeResourceDependency("metrics.db")));

    var syntax = MermaidDiagramRenderer.ItemNodeSyntax("Catalog.Metrics", item);

    Assert.That(syntax, Does.Contain("<br>──<br>db:metrics.db"),
      "An item backed by a shared resource carries the same inline compartment a step does.");
    Assert.That(syntax, Does.Contain("[("), "It still renders with the data (cylinder) shape.");
  }

  // ── Stubs ────────────────────────────────────────────────────────────

  /// <summary>A step that reports a source language — to prove the tag is collapsed.</summary>
  private sealed class PythonishStep : IStepNode<int, int>
  {
    public PythonishStep(string label, IItem<int> input, IItem<int> output,
      IReadOnlyList<ServiceDependency> deps)
    {
      Label = label;
      Inputs = new IItem[] { input };
      Outputs = new IItem[] { output };
      ServiceDependencies = deps;
    }

    public string Label { get; }
    public NodeTraits Traits { get; } = new();
    public IReadOnlyList<ServiceDependency> ServiceDependencies { get; }
    public IReadOnlyList<IItem> Inputs { get; }
    public IReadOnlyList<IItem> Outputs { get; }
    public string? SourceLanguage => "python";
    public Func<int, FlowIO<int>> Transform => x => FlowIO.Pure(x);
    public FlowIO<ValidationResult> Validate() => FlowIO.Pure(ValidationResult.Success());
    public FlowIO<FlowUnit> Execute() => FlowIO.Pure(FlowUnit.Default);
  }

  private sealed record FakeResourceDependency(string Name) : IExtensionServiceDependency
  {
    public string DagId => $"fake:{Name}";
    public string DisplayName => $"db:{Name}";
    public string Category => "fake";
  }

  /// <summary>Minimal item that declares a backing-resource service dependency.</summary>
  private sealed class ResourceBackedItem : IItem<int>
  {
    private readonly IReadOnlyList<ServiceDependency> _deps;
    public ResourceBackedItem(string label, ServiceDependency dep) { Label = label; _deps = new[] { dep }; }
    public string Label { get; }
    public NodeTraits Traits { get; } = new();
    public IReadOnlyList<ServiceDependency> ServiceDependencies => _deps;
    public FlowIO<ValidationResult> Validate() => FlowIO.Pure(ValidationResult.Success());
    public FlowIO<int> Load() => FlowIO.Pure(0);
    public FlowIO<FlowUnit> Save(int data) => FlowIO.Pure(FlowUnit.Default);
    public FlowIO<bool> Exists() => FlowIO.Pure(true);
    public FlowIO<ValidationResult> InspectShallow(int sampleSize = 100) => FlowIO.Pure(ValidationResult.Success());
    public FlowIO<ValidationResult> InspectDeep() => FlowIO.Pure(ValidationResult.Success());
    public FlowIO<ValidationResult> InspectTarget() => FlowIO.Pure(ValidationResult.Success());
  }
}
