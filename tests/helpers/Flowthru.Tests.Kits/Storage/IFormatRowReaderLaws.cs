using Flowthru.Data.Storage;

namespace Flowthru.Tests.Kits.Storage;

/// <summary>
/// Laws every read-only <see cref="IFormatRowReader{TRow}"/>
/// implementer must satisfy. Sibling to
/// <see cref="IFormatSerializerLaws{TRow}"/>: that kit covers
/// round-trip behavior for full-duplex formats; this one covers
/// formats whose backing provider can only read (Excel via
/// ExcelDataReader, certain HTTP/GQL responses, etc.).
/// </summary>
/// <typeparam name="TRow">The row type the format handles.</typeparam>
/// <remarks>
/// <para>
/// <strong>Subclass contract.</strong> The test fixture provides
/// (a) a fresh reader instance via <see cref="CreateReader"/>, and
/// (b) a fixture stream that — when read by the reader — should
/// yield <see cref="ExpectedRows"/>. Test fixtures typically build
/// the stream via a third-party writer the format itself cannot
/// produce (e.g. ClosedXML for .xlsx).
/// </para>
/// <para>
/// <strong>Read-only assertion.</strong> The kit asserts the format
/// is *structurally* read-only — it doesn't implement
/// <see cref="IFormatRowWriter{TRow}"/> and reports
/// <see cref="StorageTraits.CanWrite"/> = <c>false</c>. A format that
/// has a writer should use <see cref="IFormatSerializerLaws{TRow}"/>
/// instead.
/// </para>
/// </remarks>
public abstract class IFormatRowReaderLaws<TRow>
  where TRow : notnull
{
  /// <summary>Build a fresh reader instance for one test case.</summary>
  protected abstract IFormatRowReader<TRow> CreateReader();

  /// <summary>
  /// Build a fixture stream that, when read by the reader returned
  /// from <see cref="CreateReader"/>, yields <see cref="ExpectedRows"/>.
  /// </summary>
  /// <remarks>
  /// The kit owns stream disposal — implementations should return a
  /// stream the kit can dispose at the end of the law.
  /// </remarks>
  protected abstract Stream CreateFixtureStream();

  /// <summary>The rows that <see cref="CreateFixtureStream"/> encodes.</summary>
  protected abstract IEnumerable<TRow> ExpectedRows { get; }

  /// <summary>Equality predicate for two rows. Defaults to <see cref="EqualityComparer{TRow}.Default"/>.</summary>
  protected virtual bool RowsEqual(TRow a, TRow b) =>
    EqualityComparer<TRow>.Default.Equals(a, b);

  // ── Read-only structural law ──────────────────────────────────────────

  /// <summary>
  /// Read-only formats must not implement
  /// <see cref="IFormatRowWriter{TRow}"/>; the absence of the writer
  /// segment is the compile-time read-only-ness signal. A
  /// reader-only-by-construction format that *did* implement the
  /// writer would silently break consumers that depended on the
  /// structural claim.
  /// </summary>
  [Test]
  public void IsStructurallyReadOnlyLaw()
  {
    var reader = CreateReader();
    Assert.That(reader, Is.Not.InstanceOf<IFormatRowWriter<TRow>>(),
      "Read-only format must not implement IFormatRowWriter<TRow>; "
      + "use IFormatSerializerLaws<TRow> for full-duplex formats."
    );
    Assert.That(reader.Traits.CanWrite, Is.False,
      "Read-only format must report Traits.CanWrite = false."
    );
  }

  // ── Trait / marker drift law ──────────────────────────────────────────

  /// <summary>
  /// <see cref="StorageTraits.CanStream"/> must agree with the
  /// structural <see cref="IFormatStreamReader{TRow}"/> marker, same
  /// as for full-duplex formats. Drift between the two would let a
  /// format declare itself streaming via the marker while reporting
  /// it as buffered (or vice versa) at the trait level.
  /// </summary>
  [Test]
  public void TraitsAgreeWithMarkerInterfacesLaw()
  {
    var reader = CreateReader();
    var declaresStreamReader = reader is IFormatStreamReader<TRow>;
    Assert.That(reader.Traits.CanStream, Is.EqualTo(declaresStreamReader),
      $"Traits.CanStream ({reader.Traits.CanStream}) must agree with "
      + $"IFormatStreamReader<{typeof(TRow).Name}> implementation ({declaresStreamReader})."
    );
  }

  // ── Deserialize-fixture law ───────────────────────────────────────────

  /// <summary>
  /// The reader, given the fixture stream, yields exactly
  /// <see cref="ExpectedRows"/>. The fundamental contract for a
  /// read-only format: *can it read what the fixture encodes?*
  /// </summary>
  [Test]
  public async Task DeserializeFixtureLaw()
  {
    var reader = CreateReader();
    var expected = ExpectedRows.ToList();
    Assert.That(expected, Is.Not.Empty, "ExpectedRows must yield at least one row.");

    using var stream = CreateFixtureStream();
    var output = new List<TRow>();
    await foreach (var row in reader.DeserializeRows(stream))
    {
      output.Add(row);
    }

    Assert.That(output, Has.Count.EqualTo(expected.Count),
      "Deserialized row count should equal the fixture's encoded row count.");
    for (int i = 0; i < expected.Count; i++)
    {
      Assert.That(RowsEqual(output[i], expected[i]), Is.True,
        $"Row {i} differs from fixture. Expected {expected[i]}, got {output[i]}.");
    }
  }
}
