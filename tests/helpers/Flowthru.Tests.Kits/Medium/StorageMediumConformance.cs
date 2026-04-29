using Flowthru.Core.Data.Storage;

namespace Flowthru.Tests.Kits.Medium;

/// <summary>
/// Abstract conformance suite that every <see cref="IStorageMedium"/> implementor in a
/// first-party Flowthru extension must inherit from. Codifies the contract:
/// <see cref="IStorageMedium.ReadStream"/>, <see cref="IStorageMedium.WriteStream"/>,
/// <see cref="IStorageMedium.Exists"/>, and <see cref="IStorageMedium.InspectTarget"/>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Subclass pattern.</strong> Each subclass declares a <c>static</c> source of
/// per-instance scenario identifiers and decorates with <c>[TestFixtureSource(nameof(...))]</c>.
/// NUnit instantiates the fixture once per source entry; the constructor receives the
/// scenario name (typically just one for media that don't have shape variants).
/// </para>
/// <para>
/// <strong>Read-only media.</strong> When <c>Traits.CanWrite = false</c>, the write/round-trip
/// scenarios skip with an <c>Assert.Pass</c> — same convention as
/// <c>StorageAdapterConformance&lt;T&gt;</c> and <c>FormatSerializerConformance&lt;TRow&gt;</c>.
/// </para>
/// </remarks>
public abstract class StorageMediumConformance
{
  /// <summary>The scenario identifier this fixture instance exercises.</summary>
  protected string ScenarioName { get; }

  /// <summary>The byte payload used as fixture data, loaded once per fixture instance.</summary>
  protected byte[] FixtureBytes { get; private set; } = Array.Empty<byte>();

  protected StorageMediumConformance(string scenarioName)
  {
    ScenarioName = scenarioName;
  }

  [OneTimeSetUp]
  public void LoadFixture()
  {
    FixtureBytes = LoadFixtureBytes(ScenarioName);
  }

  // ── Subclass overrides ───────────────────────────────────────────────────

  /// <summary>
  /// Builds an <see cref="IStorageMedium"/> backed by the given byte payload — reading from
  /// it should yield exactly those bytes.
  /// </summary>
  protected abstract IStorageMedium CreateReadable(byte[] data);

  /// <summary>
  /// Builds an <see cref="IStorageMedium"/> pointed at a location that doesn't exist.
  /// <see cref="IStorageMedium.Exists"/> should return false; <see cref="IStorageMedium.ReadStream"/>
  /// should fail.
  /// </summary>
  protected abstract IStorageMedium CreateNonexistent();

  /// <summary>
  /// Builds an <see cref="IStorageMedium"/> against an empty / writable destination — used
  /// for the write+read round-trip when the medium is not read-only. If the medium is
  /// inherently read-only, return the same as <see cref="CreateReadable"/> (the scenario
  /// will be skipped).
  /// </summary>
  protected abstract IStorageMedium CreateWritable();

  /// <summary>
  /// Loads the byte payload for the given scenario. Most subclasses synthesize bytes
  /// directly; reading from a JSON fixture is rare for media-level tests.
  /// </summary>
  protected virtual byte[] LoadFixtureBytes(string scenarioName) =>
    System.Text.Encoding.UTF8.GetBytes(
      $"flowthru-storage-medium-conformance:{scenarioName}\n"
    );

  // ── Read scenarios ──────────────────────────────────────────────────────

  [Test]
  public async Task ReadStream_Readable_ReturnsSeededBytes()
  {
    var medium = CreateReadable(FixtureBytes);

    using var stream = await medium.ReadStream().Run();
    using var copy = new MemoryStream();
    await stream.CopyToAsync(copy);
    var actual = copy.ToArray();

    Assert.That(actual, Is.EqualTo(FixtureBytes));
  }

  [Test]
  public async Task Exists_Readable_ReturnsTrue()
  {
    var medium = CreateReadable(FixtureBytes);
    var exists = await medium.Exists().Run();
    Assert.That(exists, Is.True);
  }

  [Test]
  public async Task Exists_Nonexistent_ReturnsFalse()
  {
    var medium = CreateNonexistent();
    var exists = await medium.Exists().Run();
    Assert.That(exists, Is.False);
  }

  // ── Write scenarios ─────────────────────────────────────────────────────

  [Test]
  public async Task WriteStream_AndReadBack_RoundTrips()
  {
    var medium = CreateWritable();

    if (!medium.Traits.CanWrite)
    {
      Assert.Pass(
        "Medium declares Traits.CanWrite = false (read-only). Round-trip is not applicable; "
          + "the read path is exercised by ReadStream_Readable_ReturnsSeededBytes."
      );
    }

    using (var input = new MemoryStream(FixtureBytes))
    {
      await medium.WriteStream(input).Run();
    }

    using var output = await medium.ReadStream().Run();
    using var copy = new MemoryStream();
    await output.CopyToAsync(copy);

    Assert.That(copy.ToArray(), Is.EqualTo(FixtureBytes));
  }

  // ── InspectTarget — trivially valid by default; medium-specific overrides ─

  [Test]
  public async Task InspectTarget_Writable_Succeeds()
  {
    var medium = CreateWritable();
    var result = await medium.InspectTarget().Run();
    Assert.That(
      result.IsValid,
      Is.True,
      "InspectTarget on a writable medium should succeed; the IStorageMedium DIM defaults "
        + "to trivially valid for media that can't probe destination accessibility."
    );
  }
}
