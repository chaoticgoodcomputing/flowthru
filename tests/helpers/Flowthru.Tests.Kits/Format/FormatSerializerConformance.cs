using Flowthru.Core.Abstractions;
using Flowthru.Core.Data.Capabilities;
using Flowthru.Core.Data.Storage;
using Flowthru.Tests.Kits.Fixtures;

namespace Flowthru.Tests.Kits.Format;

/// <summary>
/// Abstract conformance suite that every format extension must inherit from. Codifies the
/// round-trip contract: rows loaded from a JSON fixture, serialized via the format under
/// test, deserialized back, must equal the original.
/// </summary>
/// <typeparam name="TRow">The row schema type. Must be JSON-loadable
/// (<see cref="IStructuredSerializable"/>) so the fixture can be deserialized.</typeparam>
/// <remarks>
/// <para>
/// <strong>Reader floor, writer optional.</strong> The kit's contract is built on the
/// reader segment (<see cref="IFormatRowReader{TRow}"/>), which every first-party format
/// implements. The round-trip test additionally requires a writer
/// (<see cref="IFormatRowWriter{TRow}"/>); read-only formats — Excel via ExcelDataReader —
/// pass <see cref="CreateSerializer"/> as a reader and the round-trip test skips
/// vacuously, while contractual obligations on <see cref="IFormatBase{TRow}"/>
/// (property-mapping configuration, trait honesty) still fire.
/// </para>
/// <para>
/// <strong>Subclass pattern.</strong> Each subclass declares a <c>static</c> source of
/// fixture paths and decorates the class with <c>[TestFixtureSource(nameof(...))]</c>.
/// NUnit instantiates the fixture once per source entry, passing the fixture path through
/// the constructor.
/// </para>
/// <code>
/// [TestFixtureSource(nameof(Fixtures))]
/// public class ParquetTraditionalSchemaConformance
///   : FormatSerializerConformance&lt;TraditionalSchema&gt;
/// {
///   public static IEnumerable&lt;string&gt; Fixtures =&gt; new[] { "Flat/Simple/rows.json" };
///   public ParquetTraditionalSchemaConformance(string fixturePath) : base(fixturePath) { }
///
///   protected override IFormatRowReader&lt;TraditionalSchema&gt; CreateSerializer()
///     =&gt; new ParquetFormatSerializer&lt;TraditionalSchema&gt;();
/// }
/// </code>
/// <para>
/// <strong>Cross-format alignment.</strong> All format kits run the same fixture set.
/// Behavioral drift between formats — the symptom Phase 7 of the coverage audit set out to
/// fix — surfaces here as a test failure rather than a manual categorization audit.
/// </para>
/// </remarks>
public abstract class FormatSerializerConformance<TRow>
  where TRow : notnull, IStructuredSerializable
{
  /// <summary>The fixture path under <c>Fixtures/</c> for this fixture instance.</summary>
  protected string FixturePath { get; }

  /// <summary>The fixture rows, loaded once per fixture instance.</summary>
  protected List<TRow> FixtureRows { get; private set; } = default!;

  protected FormatSerializerConformance(string fixturePath)
  {
    FixturePath = fixturePath;
  }

  [OneTimeSetUp]
  public async Task LoadFixtureData()
  {
    FixtureRows = await FixtureLoader.LoadAsync<TRow>(FixturePath);
  }

  /// <summary>
  /// Builds a fresh instance of the format under test. Returns the reader segment —
  /// the floor every first-party format implements. If the format additionally
  /// implements <see cref="IFormatRowWriter{TRow}"/> (the common case via
  /// <see cref="IFormatSerializer{TRow}"/>), the round-trip test detects this via
  /// pattern match and exercises the full duplex contract.
  /// </summary>
  protected abstract IFormatRowReader<TRow> CreateSerializer();

  /// <summary>
  /// Optional row equality comparer. Defaults to <see cref="EqualityComparer{T}.Default"/>.
  /// </summary>
  protected virtual IEqualityComparer<TRow> RowComparer => EqualityComparer<TRow>.Default;

  /// <summary>
  /// Optional row-feature gate. When non-null, the round-trip test consults the
  /// serializer's <see cref="IFormatBase{TRow}.RowFeatures"/> against this
  /// predicate; if the format does not satisfy it, the test passes vacuously with an
  /// explanatory message. When null (the default), the round-trip runs unconditionally.
  /// </summary>
  /// <remarks>
  /// Subclasses exercising fixtures that require a specific row-feature claim should
  /// override this to gate the test. For example, a conformance subclass for the
  /// <c>Flat/IScalar/</c> fixture overrides as
  /// <c>features =&gt; features.SupportsIScalar</c>. Formats that haven't claimed the
  /// feature skip the round-trip; formats that have claimed it must round-trip
  /// successfully or the test fails.
  /// </remarks>
  protected virtual Func<FormatRowFeatures, bool>? RequiredFeatures => null;

  // ── Round-trip contract ─────────────────────────────────────────────────

  [Test]
  public async Task SerializeAndDeserialize_RoundTrips()
  {
    var serializer = CreateSerializer();

    if (RequiredFeatures is { } predicate && !predicate(serializer.RowFeatures))
    {
      Assert.Pass(
        $"Format does not declare the row features required by fixture '{FixturePath}'. "
          + "The conformance subclass gates this test on a specific RowFeatures predicate; "
          + "the serializer's declared features do not satisfy it. This is an honest "
          + "skip — the capability matrix records the format as not supporting the "
          + "fixture's required feature."
      );
    }

    // Structural read-only-ness: format does not implement the writer segment. Compile-
    // time signal — captured by the type system, not just the runtime trait flag.
    if (serializer is not IFormatRowWriter<TRow> writer)
    {
      Assert.Pass(
        "Format does not implement IFormatRowWriter<TRow> (structurally read-only — "
          + "e.g., Excel via ExcelDataReader). The round-trip scenario is not applicable; "
          + "the format's read path is exercised by an extension-specific deserialize-only "
          + "test using a stream produced by a different writer (e.g., ClosedXML for .xlsx)."
      );
      return;
    }

    // Runtime read-only-ness: writer exists at compile time but is gated off at runtime
    // (e.g., medium pointed at a read-only file system). Honored even when the structural
    // signal would permit writing.
    if (!writer.Traits.CanWrite)
    {
      Assert.Pass(
        "Format declares Traits.CanWrite = false at runtime. The writer segment exists "
          + "structurally, but is disabled by configuration; round-trip is not applicable."
      );
      return;
    }

    using var buffer = new MemoryStream();
    await writer.SerializeRows(buffer, ToAsync(FixtureRows));

    buffer.Position = 0;

    var actualRows = new List<TRow>();
    await foreach (var row in serializer.DeserializeRows(buffer))
    {
      actualRows.Add(row);
    }

    Assert.That(
      actualRows,
      Has.Count.EqualTo(FixtureRows.Count),
      $"Round-trip row count mismatch. Fixture: {FixturePath}"
    );

    for (var i = 0; i < FixtureRows.Count; i++)
    {
      Assert.That(
        RowComparer.Equals(FixtureRows[i], actualRows[i]),
        Is.True,
        $"Round-trip row mismatch at index {i}. Fixture: {FixturePath}\n"
          + $"Expected: {FixtureRows[i]}\n"
          + $"Actual:   {actualRows[i]}"
      );
    }
  }

  // ── Property mapping contract ───────────────────────────────────────────

  [Test]
  public void GetPropertyMappingConfiguration_ReturnsNonNull()
  {
    var serializer = CreateSerializer();
    var mapping = serializer.GetPropertyMappingConfiguration();
    Assert.That(
      mapping,
      Is.Not.Null,
      "GetPropertyMappingConfiguration() is a contractual obligation per IFormatBase."
    );
  }

  // ── Helpers ─────────────────────────────────────────────────────────────

  private static async IAsyncEnumerable<TRow> ToAsync(IEnumerable<TRow> source)
  {
    foreach (var row in source)
    {
      yield return row;
      await Task.Yield();
    }
  }
}
