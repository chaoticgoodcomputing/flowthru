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

  /// <summary>
  /// The expected validation-error type when <see cref="CreateMissingSource"/>'s adapter is
  /// inspected. Defaults to <see cref="ValidationErrorType.NotFound"/> — appropriate for
  /// filesystem and HTTP adapters where "missing" means the source doesn't exist. EFCore
  /// adapters override to <see cref="ValidationErrorType.EmptyDataset"/> because their
  /// "missing source" scenario is an empty table (the connection works; the data isn't
  /// there). Adapters with other semantics override accordingly.
  /// </summary>
  protected virtual ValidationErrorType MissingSourceErrorType => ValidationErrorType.NotFound;

  // ── Negative-scenario factories (Phase F) ────────────────────────────────
  //
  // Each protected virtual factory describes a known bug shape lifted into the kit so
  // every adapter that could plausibly have the same problem participates. Default
  // implementations return null = "scenario not applicable to this adapter type"; the
  // corresponding [Test] method calls Assert.Pass when the factory returns null.
  //
  // To add a new negative scenario:
  //   1. Add a `protected virtual IStorageAdapter<T>? CreateAdapter<X>() => null;` factory
  //      with a clear name describing the bug shape.
  //   2. Add a `[Test]` method that constructs the adapter via the factory, calls
  //      Assert.Pass when null, and asserts the expected pre-flight outcome otherwise.
  //   3. Each adapter conformance subclass that can construct the scenario opts in by
  //      overriding the factory; subclasses where the scenario is structurally
  //      inapplicable leave the default null.
  //
  // The pattern mirrors `MissingSourceErrorType` + `CreateMissingSource()`: a virtual
  // gate plus a factory the subclass overrides. Cross-extension findings — adapters
  // that surface the same scenario with different error categories, or adapters that
  // silently pass when they shouldn't — are tracked in the phase doc.

  /// <summary>
  /// Optional factory for the "schema declares a column not present in the source"
  /// scenario. Override to construct an adapter pointing at a source that's missing a
  /// column declared in the schema (e.g., a CSV file with a header row missing a column,
  /// a Parquet file written with a different schema, a JSON document missing a property).
  /// Pre-flight should detect the divergence and surface a SchemaMismatch or
  /// DeserializationError.
  /// </summary>
  /// <remarks>
  /// Lifted from the EFCore pre-flight precedent: <c>EFCoreShapeValidator</c> raises
  /// <see cref="ValidationErrorType.SchemaMismatch"/> when an entity property's column
  /// is absent from the live table. This scenario asks the same question of every other
  /// adapter that has a notion of "expected columns from the schema."
  /// </remarks>
  protected virtual IStorageAdapter<T>? CreateAdapterMissingExpectedColumn() => null;

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
      MissingSourceErrorType
    );

  [Test]
  public Task Exists_MissingSource_ReturnsFalse() =>
    StorageAdapterAssertions.ExistsReturns(CreateMissingSource(), expected: false);

  // ── Negative-scenario tests (Phase F) ───────────────────────────────────

  /// <summary>
  /// Asserts that pre-flight detects the case where the schema declares a column that
  /// is absent from the underlying source, and surfaces the failure as
  /// <see cref="ValidationErrorType.SchemaMismatch"/>.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The category contract is strict by design. <see cref="ValidationErrorType"/> is the
  /// programmable error surface — Flow developers writing custom error-handling do
  /// <c>if (error.ErrorType == SchemaMismatch) { … }</c> and expect that to work the
  /// same regardless of underlying storage. Provider-specific exception messages stay
  /// in <see cref="ValidationError.Message"/>; the category just gets standardized.
  /// </para>
  /// <para>
  /// Per the enum's own definition, <c>SchemaMismatch</c> means "Headers or column
  /// names don't match the expected schema" — exactly what a missing column header is.
  /// <c>DeserializationError</c> ("a row failed to deserialize") and
  /// <c>InspectionFailure</c> ("an unexpected exception occurred during inspection")
  /// are misclassifications when the failure is a known structural mismatch. When the
  /// kit catches an adapter using one of those categories for this scenario, the
  /// expected fix is to translate the underlying exception in the adapter, not to
  /// loosen the kit's assertion.
  /// </para>
  /// </remarks>
  [Test]
  public async Task InspectShallow_SchemaDeclaresColumnNotInSource_DetectsMismatch()
  {
    var adapter = CreateAdapterMissingExpectedColumn();
    if (adapter is null)
    {
      Assert.Pass(
        "Negative scenario 'CreateAdapterMissingExpectedColumn' not opted into by this "
          + "subclass. Override the factory in the subclass to opt in. Adapters where "
          + "the scenario is structurally inapplicable (e.g., XML whole-document) "
          + "legitimately leave the default null."
      );
    }

    var result = await adapter!.InspectShallow(sampleSize: 10).Run();

    Assert.That(
      result.IsValid,
      Is.False,
      "Pre-flight should detect that the source is missing a schema-declared column. "
        + "If this passes, the adapter silently admits structurally invalid data — the "
        + "EFCore shape-validator gap from Phase E in another guise."
    );

    Assert.That(
      result.Errors,
      Has.Some.Matches<ValidationError>(e =>
        e.ErrorType == ValidationErrorType.SchemaMismatch
      ),
      "Expected ValidationErrorType.SchemaMismatch. Got: "
        + string.Join(
          " | ",
          result.Errors.Select(e => $"[{e.ErrorType}] {e.Message}")
        )
        + ". If the underlying provider raises something more specific (CsvHelper "
        + "HeaderValidationException, Parquet schema-mismatch, JSON property-not-found), "
        + "the adapter should translate it into ValidationError with "
        + "ErrorType = SchemaMismatch. The provider-specific text belongs in the "
        + "Message field, not in the ErrorType."
    );
  }

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
