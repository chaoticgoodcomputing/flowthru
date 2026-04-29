using Flowthru.Core.Data.Storage;
using Flowthru.Core.Data.Validation;

namespace Flowthru.Tests.Kits.Storage;

/// <summary>
/// Abstract conformance suite that every <see cref="IStorageAdapter{T}"/> implementor in a
/// first-party Flowthru extension must inherit from. Codifies the contract — Inspect{Shallow,
/// Deep,Target}, Save/Load, Exists, Traits honesty — and runs each test method against the
/// fixture path declared at fixture-construction time.
/// </summary>
/// <typeparam name="T">The container type the adapter loads/saves (e.g.
/// <c>IEnumerable&lt;TRow&gt;</c>, a single value, or <c>byte[]</c> for binary adapters).</typeparam>
/// <remarks>
/// <para>
/// <strong>Subclass pattern.</strong> Each subclass declares a <c>static</c> source of fixture
/// paths and decorates the class with <c>[TestFixtureSource(nameof(...))]</c>. NUnit
/// instantiates the fixture once per source entry, passing the fixture path through the
/// constructor.
/// </para>
/// <code>
/// [TestFixtureSource(nameof(Fixtures))]
/// public class ParquetStorageAdapterConformance
///   : StorageAdapterConformance&lt;IEnumerable&lt;TraditionalSchema&gt;&gt;
/// {
///   public static IEnumerable&lt;string&gt; Fixtures =&gt; new[] { "Flat/Simple/rows.json" };
///   public ParquetStorageAdapterConformance(string fixturePath) : base(fixturePath) { }
///
///   protected override IStorageAdapter&lt;...&gt; CreateWellFormed(...) =&gt; ...;
///   // ...
/// }
/// </code>
/// <para>
/// <strong>Why constructor-driven over [TestCaseSource].</strong> NUnit 4 requires
/// <c>[TestCaseSource]</c> arguments to be statically-resolvable, which prevents subclasses
/// from contributing instance-level fixture lists. <c>[TestFixtureSource]</c> resolves at
/// fixture-instantiation time, which is when the subclass is concrete and its static members
/// are reachable.
/// </para>
/// <para>
/// <strong>Multi-shape adapters (EFCore).</strong> Use generic test fixtures with multiple
/// <c>[TestFixture(typeof(FlatEntity))]</c> + <c>[TestFixture(typeof(NestedEntity))]</c> on
/// a parameterized conformance subclass — one class definition runs both shapes.
/// </para>
/// </remarks>
public abstract class StorageAdapterConformance<T>
{
  /// <summary>
  /// The fixture path under <c>Fixtures/</c> that this fixture instance exercises. Captured
  /// from the <c>[TestFixtureSource]</c>-supplied constructor argument.
  /// </summary>
  protected string FixturePath { get; }

  /// <summary>The fixture data, loaded once per fixture instance.</summary>
  protected T FixtureData { get; private set; } = default!;

  protected StorageAdapterConformance(string fixturePath)
  {
    FixturePath = fixturePath;
  }

  [OneTimeSetUp]
  public void LoadFixtureData()
  {
    FixtureData = LoadFixture(FixturePath);
  }

  // ── Subclass overrides ───────────────────────────────────────────────────

  /// <summary>
  /// Builds an adapter that, when read, will return data equivalent to <paramref name="data"/>.
  /// Used for round-trip and well-formed inspection scenarios.
  /// </summary>
  protected abstract IStorageAdapter<T> CreateWellFormed(T data);

  /// <summary>
  /// Builds an adapter pointing at a nonexistent / inaccessible source. Used for the
  /// missing-data scenario — <see cref="IStorageAdapter{T}.Exists"/> should return false and
  /// <see cref="IStorageAdapter{T}.InspectShallow"/> should report a NotFound failure.
  /// </summary>
  protected abstract IStorageAdapter<T> CreateMissingSource();

  /// <summary>
  /// Loads a JSON fixture into the adapter's container type <typeparamref name="T"/>.
  /// Most subclasses delegate to <see cref="Fixtures.FixtureLoader.Load{TRow}(string)"/>.
  /// </summary>
  protected abstract T LoadFixture(string fixturePath);

  /// <summary>Optional comparer for round-trip equivalence.</summary>
  protected virtual IEqualityComparer<T>? Comparer => null;

  // ── Happy-path tests ─────────────────────────────────────────────────────

  [Test]
  public Task SaveAndLoad_RoundTrips()
  {
    var adapter = CreateWellFormed(FixtureData);
    if (!adapter.Traits.CanWrite)
    {
      Assert.Pass(
        "Adapter declares Traits.CanWrite = false (read-only adapter). Save/Load round-trip "
          + "is not applicable; the adapter's read path is exercised by the well-formed "
          + "inspection scenarios. Trait consistency is covered by "
          + "StorageAdapterTraitsConformance<T>."
      );
    }
    return StorageAdapterAssertions.SaveAndLoadRoundTrips(adapter, FixtureData, Comparer);
  }

  [Test]
  public Task InspectShallow_WellFormed_Succeeds() =>
    StorageAdapterAssertions.InspectShallowSucceeds(CreateWellFormed(FixtureData));

  [Test]
  public Task InspectDeep_WellFormed_Succeeds() =>
    StorageAdapterAssertions.InspectDeepSucceeds(CreateWellFormed(FixtureData));

  [Test]
  public Task InspectTarget_Writable_Succeeds()
  {
    // For read-only adapters, InspectTarget should still succeed trivially per Traits.CanWrite=false
    // contract (covered by StorageAdapterTraitsConformance<T>). The kit-level expectation is the
    // same in both cases: InspectTarget on a well-formed adapter returns IsValid.
    return StorageAdapterAssertions.InspectTargetSucceeds(CreateWellFormed(FixtureData));
  }

  [Test]
  public Task Exists_WellFormed_ReturnsTrue() =>
    StorageAdapterAssertions.ExistsReturns(CreateWellFormed(FixtureData), expected: true);

  // ── Sad-path tests ──────────────────────────────────────────────────────

  [Test]
  public Task InspectShallow_MissingSource_Fails() =>
    StorageAdapterAssertions.InspectShallowFails(
      CreateMissingSource(),
      ValidationErrorType.NotFound
    );

  [Test]
  public Task Exists_MissingSource_ReturnsFalse() =>
    StorageAdapterAssertions.ExistsReturns(CreateMissingSource(), expected: false);

  // ── Note on read-only target testing ────────────────────────────────────
  //
  // A "create the adapter against a destination that's genuinely unwritable" scenario was
  // considered for v1 but dropped. The OS-portable mechanisms (chmod 555, NTFS ACLs, paths
  // on read-only filesystems) vary too widely to mandate at the kit level, and Phase A
  // proof-of-concept against Parquet revealed that FileStorageMedium.InspectTarget does NOT
  // reject paths under a nonexistent parent directory — a behavior worth investigating
  // separately (potential drift between InspectTarget's docstring and implementation).
  //
  // Trait consistency (CanWrite=false ⇒ InspectTarget trivially valid) is covered by
  // StorageAdapterTraitsConformance<T> instead, which doesn't need a real unwritable target.
}
