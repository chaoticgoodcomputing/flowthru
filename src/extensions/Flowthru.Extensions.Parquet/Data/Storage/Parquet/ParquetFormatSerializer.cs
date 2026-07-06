using System.Runtime.CompilerServices;
using Flowthru.Data.Schema;
using Flowthru.Data.Schema.Mapping;
using Flowthru.Data.Storage.Parquet.Internal;
using Parquet;
using Parquet.Schema;

namespace Flowthru.Data.Storage.Parquet;

/// <summary>
/// Format serializer for Parquet (binary columnar storage) files.
/// Composes via <see cref="ComposedStorageAdapter{TContainer, TRow}"/>
/// with <see cref="FileStorageMedium"/> +
/// <see cref="EnumerableContainerAdapter{TRow}"/>.
/// </summary>
/// <typeparam name="TRow">
/// Row schema. Parquet stores tabular data in a columnar layout, so
/// the schema must be flat (<see cref="IFlatSchema"/>) and binary-
/// serialisable (<see cref="IBinarySerializable"/>).
/// </typeparam>
/// <remarks>
/// <para>
/// <strong>Architecture.</strong> Flowthru schemas use <c>required</c>
/// init-only members; Parquet.Net's serialiser requires a parameterless
/// constructor with mutable properties. The bridge is a runtime-emitted
/// DTO type built via <see cref="System.Reflection.Emit"/>:
/// </para>
/// <code>
/// Serialize:   TRow (required init) → DTO (parameterless ctor) → Parquet
/// Deserialize: Parquet → DTO (parameterless ctor) → TRow (required init)
/// </code>
/// <para>
/// <strong>Capability claims.</strong> Implements
/// <see cref="IFormatStreamReader{TRow}"/> — Parquet's row-group cursor
/// makes early-exit consumers genuinely streaming. Does <em>not</em>
/// implement <see cref="ISupportsIScalar"/> or
/// <see cref="ISupportsNested"/> in this initial migration; both are
/// scoped follow-ups (see §4.8 progress-table notes).
/// </para>
/// <para>
/// <strong>Schema-mismatch translation.</strong> Before deserialising
/// any rows, the file footer's schema is read and compared against the
/// columns the planner derives from <typeparamref name="TRow"/>.
/// Missing columns surface as <see cref="SchemaMismatchException"/>,
/// which the composed adapter's boundary lifts to typed
/// <see cref="Validation.Runtime.RuntimeError.SchemaMismatch"/> on the
/// load path and <see cref="ValidationErrorType.SchemaMismatch"/> on the
/// inspect path.
/// </para>
/// <para>
/// <strong>Streaming.</strong> Rows are yielded one row group at a
/// time, so a consumer that breaks early (e.g. shallow inspection
/// taking only N samples) reads only the first row group rather than
/// the full file. Peak deserialisation memory is bounded by the
/// row-group size of the source file.
/// </para>
/// <para>
/// <strong>Bounded-memory writes.</strong> The write path streams
/// rows in batches of <see cref="ParquetItemOptions{TRow}.RowGroupSize"/>
/// (default 1 000 000). Each batch is flushed as one Parquet row group;
/// peak write-side memory is bounded to one row group regardless of
/// total dataset size.
/// </para>
/// </remarks>
public sealed class ParquetFormatSerializer<TRow>
  : IFormatSerializer<TRow>, IFormatStreamReader<TRow>
  where TRow : notnull, IFlatSchema, IBinarySerializable
{
  private readonly ParquetItemOptions<TRow>? _options;

  public ParquetFormatSerializer() { }

  public ParquetFormatSerializer(ParquetItemOptions<TRow>? options) => _options = options;

  /// <inheritdoc/>
  public StorageTraits Traits => new() { CanStream = true };

  /// <inheritdoc/>
  public async IAsyncEnumerable<TRow> DeserializeRows(
    Stream stream,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
  {
    if (stream is null)
    {
      throw new ArgumentNullException(nameof(stream));
    }

    // Parquet decoding requires random access: ParquetReader reads the footer
    // at the end of the object first, and each row group is re-read from a reset
    // position below. A forward-only source — a real S3 or HTTP response body —
    // reports CanSeek == false, so materialise it into a seekable MemoryStream
    // before decoding. Mirrors the guard in ExcelFormatSerializer. This does not
    // contradict Traits.CanStream: streaming describes incremental row-group
    // *consumption*, not seekability of the underlying byte source.
    MemoryStream? buffered = null;
    if (!stream.CanSeek)
    {
      buffered = new MemoryStream();
      await stream.CopyToAsync(buffered, cancellationToken).ConfigureAwait(false);
      buffered.Position = 0;
      stream = buffered;
    }

    try
    {
      var readOptions = _options?.ToReadOptions();

      using var reader = await ParquetReader
        .CreateAsync(stream, leaveStreamOpen: true, cancellationToken: cancellationToken)
        .ConfigureAwait(false);
      var schema = reader.Schema;
      var rowGroupCount = reader.RowGroupCount;

      // Pre-flight schema check: every column the schema declares must
      // be present in the on-disk file. Without this, missing columns
      // silently deserialize to default values and pre-flight passes on
      // a structurally invalid file. We standardise on
      // SchemaMismatchException across all formats so the composed
      // adapter classifies them uniformly.
      var expectedColumns = PropertyMappingPlanner.Build<TRow>().ByFieldName.Keys;
      var fileColumns = new HashSet<string>(
        schema.Fields.OfType<DataField>().Select(f => f.Name),
        StringComparer.OrdinalIgnoreCase
      );
      var missing = expectedColumns.Where(c => !fileColumns.Contains(c)).ToList();
      if (missing.Count > 0)
      {
        throw new SchemaMismatchException(
          $"Parquet file is missing column(s) declared by schema '{typeof(TRow).Name}': "
          + $"[{string.Join(", ", missing)}]. "
          + $"File columns: [{string.Join(", ", fileColumns)}]."
        );
      }

      var adapter = new ParquetAdapter<TRow>(schema);

      // Yield one row group at a time so early-break consumers (shallow
      // inspection, small-sample readers) avoid full-file materialisation.
      for (var rgi = 0; rgi < rowGroupCount; rgi++)
      {
        cancellationToken.ThrowIfCancellationRequested();
        stream.Position = 0;
        var dtos = await adapter.DeserializeRowGroup(stream, rgi, readOptions).ConfigureAwait(false);
        foreach (var dto in dtos)
        {
          yield return adapter.FromDto(dto);
        }
      }
    }
    finally
    {
      // We opened the reader with leaveStreamOpen: true, so the buffer we
      // allocated here is ours to dispose once enumeration completes or is
      // abandoned. (When the caller's stream was already seekable, buffered is
      // null and this is a no-op — we never own the caller's stream.)
      buffered?.Dispose();
    }
  }

  /// <inheritdoc/>
  public async Task SerializeRows(Stream stream, IAsyncEnumerable<TRow> rows)
  {
    if (stream is null) throw new ArgumentNullException(nameof(stream));
    if (rows is null) throw new ArgumentNullException(nameof(rows));

    var adapter = new ParquetAdapter<TRow>(parquetSchema: null);
    await adapter.SerializeToParquetAsync(
      stream,
      rows,
      writeOptions: _options?.ToWriteOptions(),
      rowGroupSize: _options?.RowGroupSize ?? 1_000_000
    ).ConfigureAwait(false);
  }
}
