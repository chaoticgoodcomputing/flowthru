using Flowthru.Core.Abstractions;
using Flowthru.Core.Data.Storage;
using Flowthru.Tests.Kits.Fixtures;

namespace Flowthru.Tests.Kits.Format;

/// <summary>
/// Abstract conformance suite that every <see cref="IFormatSerializer{TRow}"/> implementor
/// in a first-party Flowthru extension must inherit from. Codifies the round-trip contract:
/// rows loaded from a JSON fixture, serialized via the format under test, deserialized back,
/// must equal the original.
/// </summary>
/// <typeparam name="TRow">The row schema type. Must be JSON-loadable
/// (<see cref="IStructuredSerializable"/>) so the fixture can be deserialized.</typeparam>
/// <remarks>
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
///   protected override IFormatSerializer&lt;TraditionalSchema&gt; CreateSerializer()
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

  /// <summary>Builds a fresh instance of the format serializer under test.</summary>
  protected abstract IFormatSerializer<TRow> CreateSerializer();

  /// <summary>
  /// Optional row equality comparer. Defaults to <see cref="EqualityComparer{T}.Default"/>.
  /// </summary>
  protected virtual IEqualityComparer<TRow> RowComparer => EqualityComparer<TRow>.Default;

  // ── Round-trip contract ─────────────────────────────────────────────────

  [Test]
  public async Task SerializeAndDeserialize_RoundTrips()
  {
    var serializer = CreateSerializer();

    if (!serializer.Traits.CanWrite)
    {
      Assert.Pass(
        "Format declares Traits.CanWrite = false (read-only format). The round-trip "
          + "scenario is not applicable; the format's read path should be exercised by an "
          + "extension-specific deserialize-only test using a stream produced by a different "
          + "writer (e.g., ClosedXML for .xlsx). v1 conformance covers contract obligations "
          + "shared by all formats; deserialize-only conformance for read-only formats is a "
          + "Phase B follow-up."
      );
    }

    using var buffer = new MemoryStream();
    await serializer.SerializeRows(buffer, ToAsync(FixtureRows));

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
      "GetPropertyMappingConfiguration() is a contractual obligation per IFormatSerializer."
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
