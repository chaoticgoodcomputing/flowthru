using Flowthru.Data.Schema;
using Flowthru.Data.Storage;
using Flowthru.Prelude;

namespace Flowthru.Core.Tests.Storage;

/// <summary>
/// Verifies that <see cref="JsonFormatSerializer{TRow}"/> consumes
/// <c>PropertyMappingPlanner</c> correctly: <see cref="SerializedLabelAttribute"/>
/// drives external field names, and <see cref="IScalar"/> NewType
/// wrappers round-trip as the backing primitive.
/// </summary>
[TestFixture]
public class PlannerDrivenJsonTests
{
  private string _tempDir = null!;

  [SetUp]
  public void SetUp()
  {
    _tempDir = Path.Combine(Path.GetTempPath(), $"flowthru-2B3-{Guid.NewGuid():N}");
    Directory.CreateDirectory(_tempDir);
  }

  [TearDown]
  public void TearDown()
  {
    if (Directory.Exists(_tempDir))
    {
      try
      {
        Directory.Delete(_tempDir, recursive: true);
      }
      catch
      {
        // Best-effort.
      }
    }
  }

  [Test]
  public async Task SerializedLabel_DrivesExternalFieldName()
  {
    var path = Path.Combine(_tempDir, "labelled.json");
    var adapter = new ComposedStorageAdapter<IEnumerable<LabelledRow>, LabelledRow>(
      new FileStorageMedium(path),
      new JsonFormatSerializer<LabelledRow>(),
      new EnumerableContainerAdapter<LabelledRow>()
    );

    var saveResult = await adapter.Save(new[]
    {
      new LabelledRow { CustomerId = 42, FullName = "Ada Lovelace" },
    }).Run();
    Assert.That(saveResult, Is.InstanceOf<EffResult<FlowUnit>.Success>());

    // Inspect the on-disk JSON to verify [SerializedLabel] honored.
    var json = await File.ReadAllTextAsync(path);
    Assert.That(json, Does.Contain("\"customer_id\""), "Expected snake_case label from [SerializedLabel].");
    Assert.That(json, Does.Contain("\"full_name\""));
    Assert.That(json, Does.Not.Contain("\"CustomerId\""), "C# property name should NOT appear when [SerializedLabel] is set.");

    // Round-trip back.
    var loadResult = await adapter.Load().Run();
    Assert.That(loadResult, Is.InstanceOf<EffResult<IEnumerable<LabelledRow>>.Success>());
    var loaded = ((EffResult<IEnumerable<LabelledRow>>.Success)loadResult).Value.ToList();
    Assert.That(loaded, Has.Count.EqualTo(1));
    Assert.That(loaded[0].CustomerId, Is.EqualTo(42));
    Assert.That(loaded[0].FullName, Is.EqualTo("Ada Lovelace"));
  }

  [Test]
  public async Task IScalarWrapper_RoundTripsAsBackingPrimitive()
  {
    var path = Path.Combine(_tempDir, "iscalar.json");
    var adapter = new ComposedStorageAdapter<IEnumerable<IScalarRow>, IScalarRow>(
      new FileStorageMedium(path),
      new JsonFormatSerializer<IScalarRow>(),
      new EnumerableContainerAdapter<IScalarRow>()
    );

    var saveResult = await adapter.Save(new[]
    {
      new IScalarRow { Id = new OrderRef("ORD-001"), Quantity = 7 },
    }).Run();
    Assert.That(saveResult, Is.InstanceOf<EffResult<FlowUnit>.Success>());

    // On-disk: the wrapper should serialize as the backing string, not as
    // a nested object.
    var json = await File.ReadAllTextAsync(path);
    Assert.That(json, Does.Contain("\"ORD-001\""), "IScalar wrapper should round-trip as backing primitive.");
    Assert.That(json, Does.Not.Contain("\"Value\""), "IScalar wrapper should NOT serialize as { \"Value\": ... }.");

    // Round-trip back: wrapper reconstructed.
    var loadResult = await adapter.Load().Run();
    Assert.That(loadResult, Is.InstanceOf<EffResult<IEnumerable<IScalarRow>>.Success>());
    var loaded = ((EffResult<IEnumerable<IScalarRow>>.Success)loadResult).Value.ToList();
    Assert.That(loaded, Has.Count.EqualTo(1));
    Assert.That(loaded[0].Id.Value, Is.EqualTo("ORD-001"));
    Assert.That(loaded[0].Quantity, Is.EqualTo(7));
  }
}

[FlowthruSchema]
public partial record LabelledRow
{
  [SerializedLabel("customer_id")]
  public required int CustomerId { get; init; }

  [SerializedLabel("full_name")]
  public required string FullName { get; init; }
}

public readonly record struct OrderRef(string Value) : IScalar;

[FlowthruSchema]
public partial record IScalarRow
{
  public required OrderRef Id { get; init; }
  public required int Quantity { get; init; }
}
