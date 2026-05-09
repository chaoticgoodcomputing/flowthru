using Flowthru.Core.Data;
using Flowthru.Core.Data.Storage;
using SysIO = System.IO;

namespace Flowthru.Tests.Kits.Storage;

/// <summary>
/// Conformance suite that every <see cref="DirectoryStorageAdapter{T}"/> composition in a
/// first-party Flowthru extension must inherit from. Extends
/// <see cref="StorageAdapterConformance{T}"/> with directory-specific contract checks:
/// per-file boundary preservation on round-trip, hard-delete of stale files matching the
/// pattern on Save, empty-directory round-trip, and isolation from non-matching files.
/// </summary>
/// <typeparam name="TInner">The per-file payload type (e.g. <c>byte[]</c>,
/// <c>IEnumerable&lt;TRow&gt;</c>, a single deserialised <c>T</c>).</typeparam>
/// <remarks>
/// <para>
/// <strong>What's already covered by the parent kit:</strong> Save/Load round-trip via the
/// supplied <see cref="StorageAdapterConformance{T}.Comparer"/>, missing-source inspection,
/// well-formed inspection (shallow + deep), Exists, and the Phase F "schema declares column
/// not in source" scenario. Subclasses get all of those for free.
/// </para>
/// <para>
/// <strong>What this kit adds:</strong>
/// </para>
/// <list type="bullet">
/// <item>
///   <c>Save_HardDeletesExistingMatchingFiles</c> — confirms the deterministic-rerun
///   contract: writing a <see cref="DirectoryOf{T}"/> deletes pre-existing files matching
///   the adapter's pattern that aren't in the new directory.
/// </item>
/// <item>
///   <c>SaveLoad_EmptyDirectory_RoundTrips</c> — confirms an empty-directory write
///   produces a directory with zero entries on load, and that the directory itself is
///   created if absent.
/// </item>
/// <item>
///   <c>Load_NonMatchingFiles_AreIgnored</c> — confirms files that don't match the
///   adapter's glob are not surfaced as entries.
/// </item>
/// </list>
/// <para>
/// Subclasses must expose the directory path and file pattern they're testing so the kit
/// can plant fixture files (stale matching, non-matching) without going through the
/// adapter. The two <c>PlantWellFormedFile</c> / <c>PlantNonMatchingFile</c> abstractions
/// keep format concerns out of the kit.
/// </para>
/// </remarks>
public abstract class DirectoryStorageAdapterConformance<TInner>
  : StorageAdapterConformance<DirectoryOf<TInner>>
{
  protected DirectoryStorageAdapterConformance(string fixturePath) : base(fixturePath) { }

  // ── Subclass overrides ───────────────────────────────────────────────────

  /// <summary>
  /// Path the well-formed adapter writes / reads under. Used by the kit to plant test
  /// files outside the adapter's Save path.
  /// </summary>
  protected abstract string WellFormedDirectoryPath { get; }

  /// <summary>
  /// File extension the adapter writes (e.g. <c>".csv"</c>, <c>".png"</c>). Used to
  /// construct stale and non-matching file paths in the kit's planted-file tests.
  /// </summary>
  protected abstract string FileExtension { get; }

  /// <summary>
  /// Builds a fresh adapter pointing at the well-formed directory <em>without</em> seeding
  /// it. Used by the planted-file tests so we can introduce files via the filesystem
  /// before invoking the adapter.
  /// </summary>
  protected abstract IStorageAdapter<DirectoryOf<TInner>> CreateAdapterForWellFormedPath();

  /// <summary>
  /// Writes a well-formed file at <paramref name="filePath"/> using the same on-disk
  /// format the adapter consumes. The kit will place the file inside the well-formed
  /// directory (matching the adapter's glob); the adapter is expected to <em>delete</em>
  /// it during a subsequent Save call.
  /// </summary>
  /// <remarks>
  /// The content can be any well-formed instance — its purpose is to verify Save's
  /// hard-delete behaviour, not equality with later loads. Subclasses typically call
  /// their format's serialiser with a synthetic row or a single fixture entry.
  /// </remarks>
  protected abstract void PlantWellFormedFile(string filePath);

  /// <summary>
  /// Writes a non-matching file at <paramref name="filePath"/> (deliberately a different
  /// extension than <see cref="FileExtension"/>). Default implementation writes a UTF-8
  /// text file — override only if the extension's filesystem semantics need something
  /// more specialised.
  /// </summary>
  protected virtual void PlantNonMatchingFile(string filePath) =>
    File.WriteAllText(filePath, "non-matching content");

  // ── Directory-specific tests ─────────────────────────────────────────────

  /// <summary>
  /// Save deletes pre-existing files matching the pattern that aren't in the new directory.
  /// This is the deterministic-rerun contract: after Save, the directory state matches the
  /// <see cref="DirectoryOf{T}"/> that produced it — no leftovers from prior runs.
  /// </summary>
  [Test]
  public async Task Save_HardDeletesExistingMatchingFiles_ForDeterministicReruns()
  {
    var adapter = CreateAdapterForWellFormedPath();
    if (!adapter.Traits.CanWrite)
    {
      Assert.Pass(
        "Adapter declares Traits.CanWrite = false. Hard-delete on Save is not applicable; "
          + "deterministic-rerun semantics only matter for write-capable adapters."
      );
    }

    SysIO.Directory.CreateDirectory(WellFormedDirectoryPath);
    var stalePath = Path.Combine(
      WellFormedDirectoryPath,
      $"stale-{Guid.NewGuid():N}{FileExtension}"
    );
    PlantWellFormedFile(stalePath);
    Assert.That(File.Exists(stalePath), Is.True, "Test setup: stale file should exist before Save.");

    await adapter.Save(FixtureData).Run();

    Assert.That(
      File.Exists(stalePath),
      Is.False,
      "Save should delete pre-existing files matching the directory's pattern. The stale "
        + $"file at '{stalePath}' was still present after Save — re-runs would now leave "
        + "stale outputs alongside the fresh ones, breaking the deterministic-rerun contract."
    );
  }

  /// <summary>
  /// An empty <see cref="DirectoryOf{T}"/> round-trips: Save creates the directory if absent
  /// and clears any prior contents; Load returns a directory with zero entries.
  /// </summary>
  [Test]
  public async Task SaveLoad_EmptyDirectory_RoundTrips()
  {
    var adapter = CreateAdapterForWellFormedPath();
    if (!adapter.Traits.CanWrite)
    {
      Assert.Pass(
        "Adapter declares Traits.CanWrite = false. Empty-directory save is not applicable."
      );
    }

    await adapter.Save(DirectoryOf<TInner>.Empty).Run();
    var loaded = await adapter.Load().Run();

    Assert.That(loaded.Count, Is.EqualTo(0));
  }

  /// <summary>
  /// Files whose extension doesn't match the adapter's glob are ignored on Load. This is
  /// the "co-tenant" contract: the adapter doesn't care if other tooling drops a README,
  /// a checksum file, or a sibling format into the directory.
  /// </summary>
  [Test]
  public async Task Load_NonMatchingFiles_AreIgnored()
  {
    var adapter = CreateAdapterForWellFormedPath();

    // Seed the well-formed state via the adapter (or write directly when read-only).
    if (adapter.Traits.CanWrite)
    {
      await adapter.Save(FixtureData).Run();
    }

    SysIO.Directory.CreateDirectory(WellFormedDirectoryPath);
    var nonMatchingPath = Path.Combine(
      WellFormedDirectoryPath,
      $"sidecar-{Guid.NewGuid():N}.txt"
    );
    PlantNonMatchingFile(nonMatchingPath);

    var loaded = await adapter.Load().Run();

    Assert.That(
      loaded.Keys.Any(k => k.EndsWith(".txt", StringComparison.Ordinal)),
      Is.False,
      "Load returned an entry whose key ends in '.txt'. Non-matching files should be "
        + "ignored — the adapter's glob is the contract for which files participate."
    );
  }
}
