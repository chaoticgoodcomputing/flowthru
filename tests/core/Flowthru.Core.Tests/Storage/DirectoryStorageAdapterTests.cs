using Flowthru.Data.Catalog;
using Flowthru.Data.Schema;
using Flowthru.Data.Storage;
using Flowthru.Prelude;
using SysIO = System.IO;

namespace Flowthru.Core.Tests.Storage;

[FlowthruSchema]
public partial record DirRow
{
  public required int Id { get; init; }
  public required string Name { get; init; }
}

[FlowthruSchema]
public partial record DirDoc
{
  public required string Title { get; init; }
  public required int Score { get; init; }
}

/// <summary>
/// End-to-end tests for <see cref="DirectoryStorageAdapter{T}"/>
/// + the <c>ItemFactory.Directory</c> family of smart constructors.
/// Reactivates the legacy directory-storage conformance coverage
/// against the Phase-2 storage substrate.
/// </summary>
[TestFixture]
public class DirectoryStorageAdapterTests
{
  private string _root = null!;

  [SetUp]
  public void SetUp()
  {
    _root = SysIO.Path.Combine(SysIO.Path.GetTempPath(), $"flowthru-dir-{Guid.NewGuid():N}");
    SysIO.Directory.CreateDirectory(_root);
  }

  [TearDown]
  public void TearDown()
  {
    if (SysIO.Directory.Exists(_root))
    {
      try { SysIO.Directory.Delete(_root, recursive: true); }
      catch { /* best effort */ }
    }
  }

  // ── JsonArrays — directory of JSON-array files ────────────────────────

  [Test]
  public async Task JsonArrays_RoundTrip_PreservesEntriesPerFile()
  {
    var item = ItemFactory.Directory.JsonArrays<DirRow>(
      label: "files",
      directoryPath: _root,
      filePattern: "*.json"
    );

    var fileA = SysIO.Path.Combine(_root, "a.json");
    var fileB = SysIO.Path.Combine(_root, "b.json");
    var input = new Directory<IEnumerable<DirRow>>(new Dictionary<string, IEnumerable<DirRow>>
    {
      [fileA] = new[]
      {
        new DirRow { Id = 1, Name = "alpha" },
        new DirRow { Id = 2, Name = "beta" },
      },
      [fileB] = new[]
      {
        new DirRow { Id = 3, Name = "gamma" },
      },
    });

    var saveResult = await item.Save(input).Run();
    Assert.That(saveResult, Is.InstanceOf<EffResult<FlowUnit>.Success>());

    var loadResult = await item.Load().Run();
    Assert.That(loadResult, Is.InstanceOf<EffResult<Directory<IEnumerable<DirRow>>>.Success>());
    var loaded = ((EffResult<Directory<IEnumerable<DirRow>>>.Success)loadResult).Value;

    Assert.That(loaded.Count, Is.EqualTo(2));
    Assert.That(loaded[fileA].Select(r => r.Name), Is.EquivalentTo(new[] { "alpha", "beta" }));
    Assert.That(loaded[fileB].Single().Id, Is.EqualTo(3));
  }

  [Test]
  public async Task JsonArrays_HardDeletesExistingFilesOnSave()
  {
    var item = ItemFactory.Directory.JsonArrays<DirRow>("hard-delete", _root, "*.json");
    var stale = SysIO.Path.Combine(_root, "stale.json");
    await SysIO.File.WriteAllTextAsync(stale, "[]");
    Assert.That(SysIO.File.Exists(stale), Is.True);

    var newDir = new Directory<IEnumerable<DirRow>>(new Dictionary<string, IEnumerable<DirRow>>
    {
      [SysIO.Path.Combine(_root, "fresh.json")] =
        new[] { new DirRow { Id = 9, Name = "fresh" } },
    });
    await item.Save(newDir).Run();

    Assert.That(SysIO.File.Exists(stale), Is.False,
      "Save should hard-delete stale files matching the pattern so post-Save state matches the saved Directory<T>.");
  }

  [Test]
  public async Task JsonArrays_LoadOnEmptyDirectory_ReturnsEmpty()
  {
    var item = ItemFactory.Directory.JsonArrays<DirRow>("empty", _root, "*.json");
    var loadResult = await item.Load().Run();
    var loaded = ((EffResult<Directory<IEnumerable<DirRow>>>.Success)loadResult).Value;
    Assert.That(loaded.Count, Is.EqualTo(0));
  }

  [Test]
  public async Task JsonArrays_LoadOnMissingDirectory_ReturnsEmpty()
  {
    var missing = SysIO.Path.Combine(_root, "does-not-exist");
    var item = ItemFactory.Directory.JsonArrays<DirRow>("missing", missing, "*.json");

    var loadResult = await item.Load().Run();
    var loaded = ((EffResult<Directory<IEnumerable<DirRow>>>.Success)loadResult).Value;
    Assert.That(loaded.Count, Is.EqualTo(0));
  }

  [Test]
  public async Task JsonArrays_NonMatchingFiles_AreIsolatedFromLoad()
  {
    var item = ItemFactory.Directory.JsonArrays<DirRow>("isolated", _root, "*.json");
    await SysIO.File.WriteAllTextAsync(
      SysIO.Path.Combine(_root, "noise.txt"),
      "this is not json"
    );

    var fileA = SysIO.Path.Combine(_root, "data.json");
    var input = new Directory<IEnumerable<DirRow>>(new Dictionary<string, IEnumerable<DirRow>>
    {
      [fileA] = new[] { new DirRow { Id = 1, Name = "ok" } },
    });
    await item.Save(input).Run();

    var loadResult = await item.Load().Run();
    var loaded = ((EffResult<Directory<IEnumerable<DirRow>>>.Success)loadResult).Value;
    Assert.That(loaded.Count, Is.EqualTo(1),
      "Files that don't match filePattern should be invisible to Load.");
  }

  // ── JsonDocuments — directory of single-doc files ─────────────────────

  [Test]
  public async Task JsonDocuments_RoundTrip_PreservesOneDocPerFile()
  {
    var item = ItemFactory.Directory.JsonDocuments<DirDoc>(
      "docs", _root, "*.json"
    );

    var first = SysIO.Path.Combine(_root, "first.json");
    var second = SysIO.Path.Combine(_root, "second.json");
    var input = new Directory<DirDoc>(new Dictionary<string, DirDoc>
    {
      [first] = new DirDoc { Title = "first-title", Score = 10 },
      [second] = new DirDoc { Title = "second-title", Score = 20 },
    });

    await item.Save(input).Run();

    var loadResult = await item.Load().Run();
    var loaded = ((EffResult<Directory<DirDoc>>.Success)loadResult).Value;

    Assert.That(loaded.Count, Is.EqualTo(2));
    Assert.That(loaded[first].Title, Is.EqualTo("first-title"));
    Assert.That(loaded[second].Score, Is.EqualTo(20));
  }

  [Test]
  public async Task InspectShallow_OnMissingDirectory_ReturnsNotFound()
  {
    var missing = SysIO.Path.Combine(_root, "no-dir");
    var item = ItemFactory.Directory.JsonArrays<DirRow>("inspect-missing", missing, "*.json");

    var result = await item.InspectShallow().Run();
    var validation = ((EffResult<ValidationResult>.Success)result).Value;
    Assert.That(validation.IsValid, Is.False);
    Assert.That(
      validation.Errors.Any(e => e.ErrorType == ValidationErrorType.NotFound),
      Is.True,
      "Missing directory should surface as a NotFound validation error."
    );
  }

  [Test]
  public async Task BareKeysInSavePath_ResolveIntoDirectory()
  {
    var item = ItemFactory.Directory.JsonArrays<DirRow>("bare-keys", _root, "*.json");
    var input = new Directory<IEnumerable<DirRow>>(new Dictionary<string, IEnumerable<DirRow>>
    {
      ["bare.json"] = new[] { new DirRow { Id = 7, Name = "bare-key" } },
    });
    await item.Save(input).Run();
    Assert.That(SysIO.File.Exists(SysIO.Path.Combine(_root, "bare.json")), Is.True,
      "Bare keys (without absolute paths) should resolve into the configured directory."
    );
  }

  [Test]
  public async Task PerFileLoadFailure_PreservesTypedRuntimeErrorVerbatim()
  {
    // The pre-Phase-7.6 implementation throw-then-recaptured per-file
    // failures, smushing the typed inner RuntimeError into a wrapped
    // IOException → External. The FlowIO-chain rewrite preserves the
    // original RuntimeError so downstream consumers can still
    // pattern-match its closed-sum variant.
    var sentinel = new Flowthru.Validation.Runtime.RuntimeError.InvariantViolated(
      "sentinel-check", "sentinel-detail"
    );
    var directoryAdapter = new DirectoryStorageAdapter<int>(
      _root,
      "*.json",
      perFile => new FailingPerFileAdapter(sentinel)
    );

    // The directory needs at least one matching file for the
    // per-file adapter to be invoked.
    await SysIO.File.WriteAllTextAsync(SysIO.Path.Combine(_root, "trigger.json"), "{}");

    var loadResult = await directoryAdapter.Load().Run();
    Assert.That(loadResult, Is.InstanceOf<EffResult<Directory<int>>.Failure>());
    var failure = (EffResult<Directory<int>>.Failure)loadResult;

    Assert.That(failure.Error, Is.SameAs(sentinel),
      "Per-file adapter's typed RuntimeError should propagate verbatim — "
      + "not be wrapped in an External by a throw-recapture at the directory boundary."
    );
  }

  /// <summary>
  /// Per-file adapter that always fails Load with the supplied
  /// sentinel <see cref="Flowthru.Validation.Runtime.RuntimeError"/>.
  /// </summary>
  private sealed class FailingPerFileAdapter : IStorageAdapter<int>
  {
    private readonly Flowthru.Validation.Runtime.RuntimeError _sentinel;
    public FailingPerFileAdapter(Flowthru.Validation.Runtime.RuntimeError sentinel)
    {
      _sentinel = sentinel;
    }
    public StorageTraits Traits => new();
    public FlowIO<int> Load() => FlowIO<int>.Fail(_sentinel);
    public FlowIO<FlowUnit> Save(int data) => FlowIO.Pure(FlowUnit.Default);
    public FlowIO<bool> Exists() => FlowIO.Pure(true);
    public FlowIO<ValidationResult> InspectShallow(int sampleSize = 100) =>
      FlowIO.Pure(ValidationResult.Success());
    public FlowIO<ValidationResult> InspectDeep() =>
      FlowIO.Pure(ValidationResult.Success());
    public FlowIO<ValidationResult> InspectTarget() =>
      FlowIO.Pure(ValidationResult.Success());
  }
}
