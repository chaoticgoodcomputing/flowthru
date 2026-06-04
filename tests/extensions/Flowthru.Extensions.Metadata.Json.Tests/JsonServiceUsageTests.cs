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
/// Coverage for the <c>services</c> signal (ADR-0019, #100 s7) in the JSON
/// DAG manifest: every service used in the flow is listed with its profile
/// (capacity, cacheability) and the steps that use it; a finite capacity
/// exceeded by its users reports <c>serializes: true</c> — the conflict-group
/// view. Services are listed regardless of capacity; only their profile
/// reflects gating.
/// </summary>
[TestFixture]
[Category("Metadata.Json")]
public class JsonServiceUsageTests
{
  private string _root = null!;

  [SetUp]
  public void SetUp()
  {
    _root = SysIO.Path.Combine(SysIO.Path.GetTempPath(), $"flowthru-json-services-{Guid.NewGuid():N}");
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
    var root = ItemFactory.Singleton.Memory<int>("su-root");
    var outA = ItemFactory.Singleton.Memory<int>("su-a");
    var outB = ItemFactory.Singleton.Memory<int>("su-b");
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
  public async Task SharedCapacityOneResource_ListsServiceThatSerializes()
  {
    var ctx = FlowMetadataContext.Unsliced(TwoStepsSharingSerialResource())
      with { ServiceProfiles = new SerialResourceProvider() };

    var root = await EmitAndReadAsync(ctx);
    var services = root.GetProperty("services");

    Assert.That(services.GetArrayLength(), Is.EqualTo(1));
    var s = services[0];
    Assert.Multiple(() =>
    {
      Assert.That(s.GetProperty("resource").GetString(), Is.EqualTo("ISerialResource"));
      Assert.That(s.GetProperty("ops").EnumerateArray().Select(e => e.GetString()),
        Is.EquivalentTo(new[] { "Use" }), "An injected service is touched under the Use op.");
      Assert.That(s.GetProperty("writeCapacity").GetInt32(), Is.EqualTo(1));
      Assert.That(s.GetProperty("readCapacity").GetInt32(), Is.EqualTo(int.MaxValue));
      Assert.That(s.GetProperty("serializes").GetBoolean(), Is.True,
        "Two steps share a capacity-1 service — it serializes them.");
      var usedBy = s.GetProperty("usedBy").EnumerateArray()
        .Select(m => m.GetProperty("step").GetString()).ToList();
      Assert.That(usedBy, Is.EquivalentTo(new[] { "step-a", "step-b" }));
    });

    TestContext.Out.WriteLine("services:");
    TestContext.Out.WriteLine(JsonSerializer.Serialize(
      services, new JsonSerializerOptions { WriteIndented = true }));
  }

  [Test]
  public async Task NoProvider_ListsServiceAsUnconstrained()
  {
    // Unsliced leaves ServiceProfiles null → permissive default. The service
    // is still listed (a complete legend), but unbounded and non-serializing.
    var root = await EmitAndReadAsync(FlowMetadataContext.Unsliced(TwoStepsSharingSerialResource()));
    var services = root.GetProperty("services");

    Assert.That(services.GetArrayLength(), Is.EqualTo(1), "The service is listed even when unconstrained.");
    var s = services[0];
    Assert.Multiple(() =>
    {
      Assert.That(s.GetProperty("writeCapacity").GetInt32(), Is.EqualTo(int.MaxValue));
      Assert.That(s.GetProperty("serializes").GetBoolean(), Is.False,
        "Without a finite capacity the service constrains nothing.");
    });
  }
}
