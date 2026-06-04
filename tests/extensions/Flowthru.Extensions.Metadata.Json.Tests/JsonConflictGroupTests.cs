using System.Text.Json;
using Flowthru.Data.Catalog;
using Flowthru.Diagnostics;
using Flowthru.Diagnostics.Json;
using Flowthru.Flow;
using Flowthru.Prelude;
using Flowthru.Step;
using Flowthru.Validation.Runtime;
using SysIO = System.IO;

namespace Flowthru.Extensions.Metadata.Json.Tests;

/// <summary>
/// Prototype coverage for the conflict-group signal (ADR-0019, #100 s7) in
/// the JSON DAG manifest: two independent steps sharing a capacity-1
/// resource surface as one conflict group that serializes; a flow with no
/// constrained resource surfaces none.
/// </summary>
[TestFixture]
[Category("Metadata.Json")]
public class JsonConflictGroupTests
{
  private string _root = null!;

  [SetUp]
  public void SetUp()
  {
    _root = SysIO.Path.Combine(SysIO.Path.GetTempPath(), $"flowthru-json-conflict-{Guid.NewGuid():N}");
    SysIO.Directory.CreateDirectory(_root);
  }

  [TearDown]
  public void TearDown()
  {
    if (SysIO.Directory.Exists(_root))
      try { SysIO.Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
  }

  /// <summary>A fictional serial resource two steps both depend on.</summary>
  private interface ISerialResource { }

  /// <summary>Capacity 1 for <see cref="ISerialResource"/>; unbounded otherwise.</summary>
  private sealed class SerialResourceProvider : IServiceProfileProvider
  {
    private static readonly string SerialId = ServiceDependency.Of<ISerialResource>().DagId;
    public ServiceProfile Resolve(ServiceDependency dependency) =>
      dependency.DagId == SerialId ? new ServiceProfile { Capacity = 1 } : ServiceProfile.Unbounded;
  }

  private static BuiltFlow TwoStepsSharingSerialResource()
  {
    var root = ItemFactory.Singleton.Memory<int>("cg-root");
    var outA = ItemFactory.Singleton.Memory<int>("cg-a");
    var outB = ItemFactory.Singleton.Memory<int>("cg-b");
    var deps = new[] { ServiceDependency.Of<ISerialResource>() };

    IStepNode Step(string label, IItem<int> output) =>
      new Step<int, int>(
        label, x => FlowIO.Pure(x), new IItem[] { root }, new IItem[] { output },
        loadInputs: () => root.Load(), saveOutputs: v => output.Save(v),
        serviceDependencies: deps);

    return FlowBuilder.CreateFlow("serial-demo", b =>
    {
      b.Add(Step("step-a", outA));
      b.Add(Step("step-b", outB));
    });
  }

  private async Task<JsonElement> EmitAndReadAsync(FlowMetadataContext ctx)
  {
    var provider = new JsonMetadataProviderBuilder().WithOutputDirectory(_root).Build();
    var result = await ((IMetadataProvider)provider).Emit(ctx).Run();
    Assert.That(result, Is.InstanceOf<EffResult<FlowUnit>.Success>());
    var json = SysIO.File.ReadAllText(SysIO.Directory.GetFiles(_root, "*.json").Single());
    return JsonDocument.Parse(json).RootElement.Clone();
  }

  [Test]
  public async Task SharedCapacityOneResource_SurfacesOneSerializingConflictGroup()
  {
    var ctx = FlowMetadataContext.Unsliced(TwoStepsSharingSerialResource())
      with { ServiceProfiles = new SerialResourceProvider() };

    var root = await EmitAndReadAsync(ctx);
    var groups = root.GetProperty("conflictGroups");

    Assert.That(groups.GetArrayLength(), Is.EqualTo(1), "Both steps share one capacity-1 resource.");
    var g = groups[0];
    Assert.Multiple(() =>
    {
      Assert.That(g.GetProperty("op").GetString(), Is.EqualTo("Use"),
        "An injected service dependency contends under the Use op.");
      Assert.That(g.GetProperty("capacity").GetInt32(), Is.EqualTo(1));
      Assert.That(g.GetProperty("serializes").GetBoolean(), Is.True,
        "Two steps share a capacity-1 resource — they serialize.");
      var steps = g.GetProperty("steps").EnumerateArray().Select(e => e.GetString()).ToList();
      Assert.That(steps, Is.EquivalentTo(new[] { "step-a", "step-b" }));
    });

    // Surface the actual signal in test output for prototype inspection.
    TestContext.Out.WriteLine("conflictGroups:");
    TestContext.Out.WriteLine(JsonSerializer.Serialize(
      groups, new JsonSerializerOptions { WriteIndented = true }));
  }

  [Test]
  public async Task NoProvider_SurfacesNoConflictGroups()
  {
    // Unsliced leaves ServiceProfiles null → permissive default → nothing
    // is constrained → no groups, even though the steps declare the dep.
    var root = await EmitAndReadAsync(FlowMetadataContext.Unsliced(TwoStepsSharingSerialResource()));
    Assert.That(root.GetProperty("conflictGroups").GetArrayLength(), Is.EqualTo(0),
      "Without a provider declaring a finite capacity, there are no conflict groups.");
  }
}
