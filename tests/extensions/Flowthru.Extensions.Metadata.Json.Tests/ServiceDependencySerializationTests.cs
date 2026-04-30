using Flowthru.Core.Graph.Meta.Models;
using Flowthru.Meta;

namespace Flowthru.Extensions.Metadata.Json.Tests;

/// <summary>
/// Tests for Phase 6 JSON serialization of step service dependencies. The
/// <see cref="StepMetadata.ServiceDependencies"/> field is serialized via
/// <c>System.Text.Json</c>. Empty lists serialize as <c>[]</c>, matching the
/// existing behavior of <see cref="StepMetadata.Inputs"/> and
/// <see cref="StepMetadata.Outputs"/>.
/// </summary>
[TestFixture]
[Category("Metadata")]
[Category("Json")]
[Category("ServiceSerialization")]
public class ServiceDependencySerializationTests
{
  // ─────────────────────────────────────────────────────────────────────────
  // Step with empty ServiceDependencies → empty array (consistent with Inputs/Outputs)
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void StepWithEmptyServiceDependencies_SerializesAsEmptyArray()
  {
    var dag = BuildDag(("PureStep", services: new string[0]));

    var json = dag.ToJson();

    Assert.That(json, Does.Contain("\"serviceDependencies\": []"));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Step with service deps → field present + service names embedded
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void StepWithServiceDependencies_FieldPresentInJson()
  {
    var dag = BuildDag(
      ("ServiceStep", services: new[]
      {
        "MyApp.IMailchimpClient",
        "MyApp.ISlackClient",
      })
    );

    var json = dag.ToJson();

    Assert.Multiple(() =>
    {
      Assert.That(json, Does.Contain("\"serviceDependencies\""));
      Assert.That(json, Does.Contain("\"MyApp.IMailchimpClient\""));
      Assert.That(json, Does.Contain("\"MyApp.ISlackClient\""));
    });
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Round-trip: serialized → deserialized preserves ServiceDependencies
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void RoundTrip_PreservesServiceDependencies()
  {
    var dag = BuildDag(
      ("Step1", services: new[] { "MyApp.IClient" })
    );
    var json = dag.ToJson();

    var restored = MetadataJsonExtensions.FromJson(json);

    Assert.That(
      restored.Steps[0].ServiceDependencies,
      Is.EquivalentTo(new[] { "MyApp.IClient" })
    );
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Helpers
  // ─────────────────────────────────────────────────────────────────────────

  private static DagMetadata BuildDag(params (string Label, string[] Services)[] steps)
  {
    var dag = new DagMetadata
    {
      FlowName = "TestFlow",
      Steps = new(),
      CatalogItems = new(),
      Edges = new(),
    };
    foreach (var (label, services) in steps)
    {
      dag.Steps.Add(new StepMetadata
      {
        Id = label,
        Label = label,
        StepType = label,
        Layer = 0,
        FlowName = "TestFlow",
        ServiceDependencies = services.ToList(),
      });
    }
    return dag;
  }
}
