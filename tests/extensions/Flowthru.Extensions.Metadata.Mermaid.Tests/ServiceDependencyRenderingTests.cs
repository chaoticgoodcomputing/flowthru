using Flowthru.Core.Graph.Meta.Models;
using Flowthru.Meta;

namespace Flowthru.Extensions.Metadata.Mermaid.Tests;

/// <summary>
/// Tests for Phase 6 Mermaid rendering of step service dependencies. Each test
/// constructs a minimal <see cref="DagMetadata"/> fixture and asserts on the
/// generated Mermaid output.
/// </summary>
[TestFixture]
[Category("Metadata")]
[Category("Mermaid")]
[Category("ServiceRendering")]
public class ServiceDependencyRenderingTests
{
  // ─────────────────────────────────────────────────────────────────────────
  // 0 service deps → no service section emitted (existing diagrams unchanged)
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void ZeroServiceDeps_NoServiceSection()
  {
    var dag = BuildDag(
      ("MyStep", "MyFlow", new string[0])
    );

    var output = dag.ToMermaidDiagram();

    Assert.Multiple(() =>
    {
      Assert.That(output, Does.Not.Contain("Service Dependencies"));
      Assert.That(output, Does.Not.Contain("classDef service"));
      Assert.That(output, Does.Not.Contain("-.uses.->"));
    });
  }

  // ─────────────────────────────────────────────────────────────────────────
  // 1 step + 1 service → one node + one dashed edge
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void OneStepOneService_RendersNodeAndEdge()
  {
    var dag = BuildDag(
      ("ApplyDeltas", "Sync", new[] { "MyApp.IMailchimpClient" })
    );

    var output = dag.ToMermaidDiagram();

    Assert.Multiple(() =>
    {
      Assert.That(output, Does.Contain("svc_MyApp_IMailchimpClient[\"IMailchimpClient\"]"));
      Assert.That(output, Does.Contain("ApplyDeltas -.uses.-> svc_MyApp_IMailchimpClient"));
      Assert.That(output, Does.Contain("classDef service"));
      Assert.That(output, Does.Contain("class svc_MyApp_IMailchimpClient service"));
    });
  }

  // ─────────────────────────────────────────────────────────────────────────
  // 2 steps sharing 1 service → one node + two dashed edges (idempotency)
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void TwoStepsShareOneService_OneNodeTwoEdges()
  {
    var dag = BuildDag(
      ("StepA", "Flow1", new[] { "MyApp.ISharedService" }),
      ("StepB", "Flow1", new[] { "MyApp.ISharedService" })
    );

    var output = dag.ToMermaidDiagram();

    // One service node — count occurrences of the bracketed declaration line
    // (NOT the dashed edges which also reference the node ID).
    var declarationCount = CountOccurrences(output, "svc_MyApp_ISharedService[\"ISharedService\"]");
    Assert.Multiple(() =>
    {
      Assert.That(declarationCount, Is.EqualTo(1), "service node should be declared exactly once");
      Assert.That(output, Does.Contain("StepA -.uses.-> svc_MyApp_ISharedService"));
      Assert.That(output, Does.Contain("StepB -.uses.-> svc_MyApp_ISharedService"));
    });
  }

  // ─────────────────────────────────────────────────────────────────────────
  // 1 step with multiple services → one node per service, one edge each
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void OneStepMultipleServices_OneNodePerService()
  {
    var dag = BuildDag(
      ("MultiServiceStep", "Flow1", new[]
      {
        "MyApp.IMailchimpClient",
        "MyApp.ISlackClient",
        "MyApp.IInternalCrmClient",
      })
    );

    var output = dag.ToMermaidDiagram();

    Assert.Multiple(() =>
    {
      Assert.That(output, Does.Contain("svc_MyApp_IMailchimpClient[\"IMailchimpClient\"]"));
      Assert.That(output, Does.Contain("svc_MyApp_ISlackClient[\"ISlackClient\"]"));
      Assert.That(output, Does.Contain("svc_MyApp_IInternalCrmClient[\"IInternalCrmClient\"]"));

      Assert.That(output, Does.Contain("MultiServiceStep -.uses.-> svc_MyApp_IMailchimpClient"));
      Assert.That(output, Does.Contain("MultiServiceStep -.uses.-> svc_MyApp_ISlackClient"));
      Assert.That(output, Does.Contain("MultiServiceStep -.uses.-> svc_MyApp_IInternalCrmClient"));
    });
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Helpers
  // ─────────────────────────────────────────────────────────────────────────

  private static DagMetadata BuildDag(
    params (string StepLabel, string FlowName, string[] ServiceFullNames)[] steps
  )
  {
    var dag = new DagMetadata { FlowName = "TestDag" };
    foreach (var (label, flowName, services) in steps)
    {
      dag.Steps.Add(new StepMetadata
      {
        Id = label,
        Label = label,
        StepType = label,
        Layer = 0,
        FlowName = flowName,
        ServiceDependencies = services.ToList(),
      });
    }
    return dag;
  }

  private static int CountOccurrences(string source, string substring)
  {
    int count = 0;
    int idx = 0;
    while ((idx = source.IndexOf(substring, idx, System.StringComparison.Ordinal)) >= 0)
    {
      count++;
      idx += substring.Length;
    }
    return count;
  }
}
