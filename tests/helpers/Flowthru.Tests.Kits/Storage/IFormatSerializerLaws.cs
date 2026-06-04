using Flowthru.Data.Storage;

namespace Flowthru.Tests.Kits.Storage;

/// <summary>
/// Laws every <see cref="IFormatSerializer{TRow}"/> implementer must
/// satisfy. Subclasses bind a concrete <typeparamref name="TRow"/> and
/// representative sample rows; the inherited tests cover round-trip
/// behavior, empty streams, and large enumerables.
/// </summary>
/// <typeparam name="TRow">The row type the format handles.</typeparam>
/// <remarks>
/// Replaces the prior <c>FormatSerializerConformance&lt;TRow&gt;</c> kit
/// from §1.7. Behaves identically; renamed per §2.11 to align with the
/// algebra-laws framing.
/// </remarks>
public abstract class IFormatSerializerLaws<TRow>
  where TRow : notnull
{
  /// <summary>Build a fresh serializer instance for one test case.</summary>
  protected abstract IFormatSerializer<TRow> CreateSerializer();

  /// <summary>Sample rows the round-trip law uses to construct a representative input.</summary>
  protected abstract IEnumerable<TRow> SampleRows { get; }

  /// <summary>Equality predicate for two rows. Defaults to <see cref="EqualityComparer{TRow}.Default"/>.</summary>
  protected virtual bool RowsEqual(TRow a, TRow b) =>
    EqualityComparer<TRow>.Default.Equals(a, b);

  // ── Round-trip law ─────────────────────────────────────────────────────

  /// <summary>
  /// Round-trip: <c>Serialize(rows)</c> then <c>Deserialize(stream)</c>
  /// yields the original row sequence. The fundamental round-trip
  /// invariant every format must satisfy.
  /// </summary>
  [Test]
  public async Task RoundTripLaw()
  {
    var serializer = CreateSerializer();
    var input = SampleRows.ToList();
    Assert.That(input, Is.Not.Empty, "SampleRows must yield at least one row.");

    using var stream = new MemoryStream();
    await serializer.SerializeRows(stream, ToAsync(input));

    stream.Position = 0;
    var output = new List<TRow>();
    await foreach (var row in serializer.DeserializeRows(stream))
    {
      output.Add(row);
    }

    Assert.That(output, Has.Count.EqualTo(input.Count),
      "Deserialized row count should equal serialized row count.");
    for (int i = 0; i < input.Count; i++)
    {
      Assert.That(RowsEqual(output[i], input[i]), Is.True,
        $"Row {i} differs after round-trip. Expected {input[i]}, got {output[i]}.");
    }
  }

  // ── Trait / marker drift law ───────────────────────────────────────────

  /// <summary>
  /// <see cref="StorageTraits.CanStream"/> on the runtime traits flag
  /// must agree with the structural <see cref="IFormatStreamReader{TRow}"/>
  /// marker — implementing the marker is the compile-time claim, the
  /// flag is the runtime contribution to <see cref="ComposedStorageAdapter{TContainer, TRow}"/>'s
  /// composed traits. Drift between the two would let a format declare
  /// itself streaming via the marker while reporting it as buffered
  /// (or vice versa) to the runtime composer, breaking either consumer
  /// expectations or the runtime trait composition. Adding this law
  /// to the kit makes the agreement testable at the laws level rather
  /// than relying on per-extension review.
  /// </summary>
  [Test]
  public void TraitsAgreeWithMarkerInterfacesLaw()
  {
    var serializer = CreateSerializer();
    var declaresStreamReader = serializer is IFormatStreamReader<TRow>;
    Assert.That(serializer.Traits.CanStream, Is.EqualTo(declaresStreamReader),
      $"Traits.CanStream ({serializer.Traits.CanStream}) must agree with "
      + $"IFormatStreamReader<{typeof(TRow).Name}> implementation ({declaresStreamReader}). "
      + "If the format genuinely streams row-by-row off a forward-only cursor, "
      + "implement IFormatStreamReader<TRow> AND set Traits.CanStream = true. "
      + "Otherwise, implement only IFormatRowReader<TRow> AND leave Traits.CanStream = false."
    );
  }

  // ── Empty-stream law ───────────────────────────────────────────────────

  /// <summary>
  /// Empty round-trip: serializing an empty enumerable produces a stream
  /// that deserializes to an empty enumerable. Important for streaming
  /// formats where the empty case has its own structural shape (CSV with
  /// only the header, JSON with <c>[]</c>, Parquet with the schema but no
  /// row groups).
  /// </summary>
  [Test]
  public async Task EmptyRoundTripLaw()
  {
    var serializer = CreateSerializer();

    using var stream = new MemoryStream();
    await serializer.SerializeRows(stream, ToAsync(Enumerable.Empty<TRow>()));

    stream.Position = 0;
    var count = 0;
    await foreach (var _ in serializer.DeserializeRows(stream))
    {
      count++;
    }

    Assert.That(count, Is.EqualTo(0), "Empty serialization should round-trip to empty deserialization.");
  }

  // ── Non-seekable-input law ─────────────────────────────────────────────

  /// <summary>
  /// <c>DeserializeRows</c> must succeed when handed a <strong>forward-only</strong>
  /// (non-seekable) stream — exactly what a real S3 or HTTP response body is. A
  /// format that needs random access (e.g. Parquet, whose footer lives at the end
  /// of the file) must buffer the input itself rather than assume a seekable
  /// source; a forward-only format reads it as-is. Either way the row sequence must
  /// be identical to the seekable round-trip. Without this law the failure hides
  /// behind every seekable test stand-in and only surfaces against live storage
  /// (issue #105).
  /// </summary>
  [Test]
  public async Task NonSeekableDeserializeLaw()
  {
    var serializer = CreateSerializer();
    var input = SampleRows.ToList();
    Assert.That(input, Is.Not.Empty, "SampleRows must yield at least one row.");

    using var seekable = new MemoryStream();
    await serializer.SerializeRows(seekable, ToAsync(input));
    seekable.Position = 0;

    using var forwardOnly = new NonSeekableStream(seekable);
    Assert.That(forwardOnly.CanSeek, Is.False, "Precondition: the input stream must be non-seekable.");

    var output = new List<TRow>();
    await foreach (var row in serializer.DeserializeRows(forwardOnly))
    {
      output.Add(row);
    }

    Assert.That(output, Has.Count.EqualTo(input.Count),
      "Deserializing a non-seekable stream must yield the same row count as a seekable one.");
    for (int i = 0; i < input.Count; i++)
    {
      Assert.That(RowsEqual(output[i], input[i]), Is.True,
        $"Row {i} differs after a non-seekable round-trip. Expected {input[i]}, got {output[i]}.");
    }
  }

  // ── Helpers ────────────────────────────────────────────────────────────

  private static async IAsyncEnumerable<T> ToAsync<T>(IEnumerable<T> source)
  {
    foreach (var item in source)
    {
      yield return item;
      await Task.Yield();
    }
  }
}
