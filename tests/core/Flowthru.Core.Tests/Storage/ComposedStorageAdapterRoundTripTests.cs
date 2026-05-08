using Flowthru.Data.Schema;
using Flowthru.Data.Storage;
using Flowthru.Prelude;

namespace Flowthru.Core.Tests.Storage;

/// <summary>
/// Smoke test for the Phase 2B-2 storage substrate: composes
/// <see cref="FileStorageMedium"/> + <see cref="JsonFormatSerializer{TRow}"/> +
/// <see cref="EnumerableContainerAdapter{T}"/> through
/// <see cref="ComposedStorageAdapter{TContainer, TRow}"/> and verifies a
/// full round-trip and inspection-on-missing-source path. The full
/// Conformance Laws (2B-4) extend this with property-based generators and
/// counterexample fixtures.
/// </summary>
[TestFixture]
public class ComposedStorageAdapterRoundTripTests
{
  private string _tempDir = null!;

  [SetUp]
  public void SetUp()
  {
    _tempDir = Path.Combine(Path.GetTempPath(), $"flowthru-2B2-{Guid.NewGuid():N}");
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
        // Best-effort cleanup.
      }
    }
  }

  [Test]
  public async Task SaveAndLoad_RoundTrips()
  {
    var path = Path.Combine(_tempDir, "round-trip.json");
    var adapter = MakeAdapter(path);

    var input = new[]
    {
      new TestRow { Id = 1, Name = "alpha" },
      new TestRow { Id = 2, Name = "beta" },
      new TestRow { Id = 3, Name = "gamma" },
    };

    var saveResult = await adapter.Save(input).Run();
    Assert.That(saveResult, Is.InstanceOf<EffResult<FlowUnit>.Success>(),
      "Save should succeed against a writable file path.");
    Assert.That(File.Exists(path), Is.True, "File should exist after Save.");

    var loadResult = await adapter.Load().Run();
    Assert.That(loadResult, Is.InstanceOf<EffResult<IEnumerable<TestRow>>.Success>(),
      "Load should succeed against the saved file.");
    var loaded = ((EffResult<IEnumerable<TestRow>>.Success)loadResult).Value.ToList();
    Assert.That(loaded, Has.Count.EqualTo(3));
    Assert.That(loaded[0].Id, Is.EqualTo(1));
    Assert.That(loaded[0].Name, Is.EqualTo("alpha"));
    Assert.That(loaded[2].Name, Is.EqualTo("gamma"));
  }

  [Test]
  public async Task Exists_ReturnsFalseForMissingFile()
  {
    var path = Path.Combine(_tempDir, "missing.json");
    var adapter = MakeAdapter(path);

    var result = await adapter.Exists().Run();
    Assert.That(result, Is.InstanceOf<EffResult<bool>.Success>());
    Assert.That(((EffResult<bool>.Success)result).Value, Is.False);
  }

  [Test]
  public async Task InspectShallow_OnMissingSource_ReturnsNotFound()
  {
    var path = Path.Combine(_tempDir, "missing.json");
    var adapter = MakeAdapter(path);

    var result = await adapter.InspectShallow(sampleSize: 10).Run();
    Assert.That(result, Is.InstanceOf<EffResult<ValidationResult>.Success>());
    var validationResult = ((EffResult<ValidationResult>.Success)result).Value;
    Assert.That(validationResult.IsValid, Is.False);
    Assert.That(validationResult.Errors[0].ErrorType, Is.EqualTo(ValidationErrorType.NotFound));
  }

  [Test]
  public async Task InspectShallow_OnWellFormedSource_Succeeds()
  {
    var path = Path.Combine(_tempDir, "well-formed.json");
    var adapter = MakeAdapter(path);

    await adapter.Save(new[] { new TestRow { Id = 1, Name = "x" } }).Run();

    var result = await adapter.InspectShallow(sampleSize: 10).Run();
    Assert.That(result, Is.InstanceOf<EffResult<ValidationResult>.Success>());
    var validationResult = ((EffResult<ValidationResult>.Success)result).Value;
    Assert.That(validationResult.IsValid, Is.True,
      $"Expected valid, got: {string.Join(", ", validationResult.Errors.Select(e => e.Message))}");
  }

  [Test]
  public async Task InspectTarget_ProbesWriteAccess()
  {
    var path = Path.Combine(_tempDir, "target.json");
    var adapter = MakeAdapter(path);

    var result = await adapter.InspectTarget().Run();
    Assert.That(result, Is.InstanceOf<EffResult<ValidationResult>.Success>());
    var validationResult = ((EffResult<ValidationResult>.Success)result).Value;
    Assert.That(validationResult.IsValid, Is.True,
      "InspectTarget should succeed against a writable temp directory.");
  }

  [Test]
  public async Task Save_OnReadOnlyAdapter_FailsFast()
  {
    var path = Path.Combine(_tempDir, "read-only.json");

    // Construct read-only adapter via the (reader-only) constructor.
    var adapter = new ComposedStorageAdapter<IEnumerable<TestRow>, TestRow>(
      medium: new FileStorageMedium(path),
      reader: new JsonFormatSerializer<TestRow>(),
      writer: null,
      container: new EnumerableContainerAdapter<TestRow>()
    );

    Assert.That(adapter.Traits.CanWrite, Is.False);

    var result = await adapter.Save(new[] { new TestRow { Id = 1, Name = "x" } }).Run();
    Assert.That(result, Is.InstanceOf<EffResult<FlowUnit>.Failure>(),
      "Save on a read-only adapter should fail at the FlowIO level.");
  }

  private static ComposedStorageAdapter<IEnumerable<TestRow>, TestRow> MakeAdapter(string path) =>
    new(
      medium: new FileStorageMedium(path),
      format: new JsonFormatSerializer<TestRow>(),
      container: new EnumerableContainerAdapter<TestRow>()
    );
}

[FlowthruSchema]
public partial record TestRow
{
  public required int Id { get; init; }
  public required string Name { get; init; }
}
