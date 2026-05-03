using Flowthru.Core.Data.Capabilities;
using Flowthru.Core.Data.Validation;
using Flowthru.Core.Effects;

namespace Flowthru.Core.Data.Storage;

/// <summary>
/// Composed storage adapter that delegates to medium, format, and container layers.
/// </summary>
/// <typeparam name="TContainer">The in-memory container type (IEnumerable, IDataView, Seq)</typeparam>
/// <typeparam name="TRow">The row schema type</typeparam>
/// <remarks>
/// <para>
/// <strong>Composition Pattern:</strong>
/// </para>
/// <para>
/// This class composes three independent concerns:
/// </para>
/// <code>
/// Medium (WHERE)    → Format (HOW)         → Container (WHAT)
/// File/Memory/Net   → CSV/JSON/Parquet     → IEnumerable/IDataView/Seq
/// </code>
/// <para>
/// <strong>Multiplicative Flexibility:</strong>
/// </para>
/// <para>
/// With M mediums, F formats, and C containers, you get M × F × C combinations
/// with only M + F + C implementations.
/// </para>
/// <para>
/// Example: 3 mediums × 4 formats × 3 containers = 36 combinations with 10 implementations.
/// </para>
/// <para>
/// <strong>Capability Implementation:</strong>
/// </para>
/// <para>
/// This adapter can optionally implement capability interfaces based on the
/// underlying medium and format capabilities. Capabilities are implemented
/// as explicit interface implementations to avoid polluting the base interface.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // CSV file with IEnumerable container
/// var csvEnum = new ComposedStorageAdapter&lt;IEnumerable&lt;Company&gt;, Company&gt;(
///     medium: new FileStorageMedium("data.csv"),
///     format: new CsvFormatSerializer&lt;Company&gt;(),
///     container: new EnumerableContainerAdapter&lt;Company&gt;()
/// );
///
/// // Same CSV file with IDataView container
/// var csvDataView = new ComposedStorageAdapter&lt;IDataView, Company&gt;(
///     medium: new FileStorageMedium("data.csv"),
///     format: new CsvFormatSerializer&lt;Company&gt;(),
///     container: new DataViewContainerAdapter&lt;Company&gt;(mlContext)
/// );
/// </code>
/// </example>
public sealed class ComposedStorageAdapter<TContainer, TRow> : IStorageAdapter<TContainer>
  where TRow : notnull
{
  private readonly IStorageMedium _medium;
  private readonly IFormatRowReader<TRow> _reader;
  private readonly IFormatRowWriter<TRow>? _writer;
  private readonly IContainerAdapter<TContainer, TRow> _container;

  /// <summary>
  /// Creates a new composed storage adapter from a write-capable format serializer.
  /// Backward-compatible entry point: any <see cref="IFormatSerializer{TRow}"/> is
  /// both a reader and a writer and is wired into both segments.
  /// </summary>
  /// <param name="medium">The storage medium (file, memory, etc.).</param>
  /// <param name="format">The full-duplex format serializer (CSV, JSON, Parquet).</param>
  /// <param name="container">The container adapter (IEnumerable, IDataView, etc.).</param>
  public ComposedStorageAdapter(
    IStorageMedium medium,
    IFormatSerializer<TRow> format,
    IContainerAdapter<TContainer, TRow> container
  )
    : this(medium, format, format, container)
  {
  }

  /// <summary>
  /// Creates a new composed storage adapter with separate reader and writer segments
  /// (Phase D capability-segmented interfaces). Read-only formats — e.g.,
  /// <c>ExcelFormatSerializer</c> implementing only <see cref="IFormatRowReader{TRow}"/> —
  /// pass <see langword="null"/> for the writer; the resulting adapter exposes
  /// <see cref="Traits"/>.<see cref="StorageTraits.CanWrite"/> as <see langword="false"/>
  /// and <see cref="Save"/> fails fast.
  /// </summary>
  /// <param name="medium">The storage medium.</param>
  /// <param name="reader">Format reader segment. Required.</param>
  /// <param name="writer">Format writer segment. <see langword="null"/> for read-only formats.</param>
  /// <param name="container">The container adapter.</param>
  public ComposedStorageAdapter(
    IStorageMedium medium,
    IFormatRowReader<TRow> reader,
    IFormatRowWriter<TRow>? writer,
    IContainerAdapter<TContainer, TRow> container
  )
  {
    _medium = medium ?? throw new ArgumentNullException(nameof(medium));
    _reader = reader ?? throw new ArgumentNullException(nameof(reader));
    _writer = writer;
    _container = container ?? throw new ArgumentNullException(nameof(container));
  }

  /// <inheritdoc/>
  /// <remarks>
  /// <para>
  /// Merges traits from all three composition layers:
  /// </para>
  /// <list type="bullet">
  /// <item><strong>Medium:</strong> Provides constraints (CanWrite, RequiresNetwork, IsPersistent)</item>
  /// <item><strong>Format:</strong> Provides streaming capability (CanStream)</item>
  /// <item><strong>Container:</strong> Currently no traits (in-memory projection)</item>
  /// </list>
  /// <para>
  /// Constraints use AND logic (most restrictive wins).
  /// Capabilities use AND logic (all layers must support it).
  /// </para>
  /// </remarks>
  public StorageTraits Traits =>
    new StorageTraits
    {
      // Medium determines storage-level constraints
      CanRead = _medium.Traits.CanRead,
      // CanWrite combines: medium can write AND a writer segment was provided AND the
      // writer's runtime traits allow it. The writer being null is the structural
      // (compile-time) read-only-ness signal; _writer.Traits.CanWrite carries any
      // runtime read-only-ness signal layered on top.
      CanWrite = _medium.Traits.CanWrite && _writer is not null && _writer.Traits.CanWrite,
      CanInspect = _medium.Traits.CanInspect,
      IsPersistent = _medium.Traits.IsPersistent,
      RequiresNetwork = _medium.Traits.RequiresNetwork,
      // Format determines streaming capability (medium must support it too)
      CanStream = _medium.Traits.CanStream && _reader.Traits.CanStream,
      // Medium determines append/transactional capabilities
      CanAppend = _medium.Traits.CanAppend,
      IsTransactional = _medium.Traits.IsTransactional,
    };

  /// <inheritdoc/>
  public FlowIO<TContainer> Load()
  {
    // Compose IO operations functionally using LINQ comprehension syntax
    return from stream in _medium.ReadStream()
      from container in FlowIO.LiftAsync(
        async (CancellationToken ct) =>
        {
          try
          {
            // 2. Format: Deserialize bytes to rows
            var rows = _reader.DeserializeRows(stream);

            // 3. Container: Materialize rows into container
            var result = await _container.FromRows(rows);

            return result;
          }
          finally
          {
            stream.Dispose();
          }
        }
      )
      select container;
  }

  /// <inheritdoc/>
  public FlowIO<FlowUnit> Save(TContainer data)
  {
    // Check if read-only before attempting write
    if (!Traits.CanWrite)
    {
      return FlowIO.Fail<FlowUnit>(
        new InvalidOperationException(
          "Cannot write to read-only storage adapter. "
            + "Check StorageTraits.CanWrite before attempting Save()."
        )
      );
    }

    // Compose IO operations functionally
    return from memStream in FlowIO.LiftAsync(
        async (CancellationToken ct) =>
        {
          var stream = new MemoryStream();

          // 1. Container: Convert container to rows
          var rows = _container.ToRows(data);

          // 2. Format: Serialize rows to bytes
          await _writer!.SerializeRows(stream, rows);

          stream.Position = 0;
          return stream;
        }
      )
      from result in _medium.WriteStream(memStream)
      select result;
  }

  /// <inheritdoc/>
  public FlowIO<bool> Exists()
  {
    return _medium.Exists();
  }

  /// <inheritdoc />
  public FlowIO<Data.Validation.ValidationResult> InspectShallow(int sampleSize)
  {
    return FlowIO.LiftAsync(
      async (CancellationToken ct) =>
      {
        // Check if medium exists
        bool exists;
        try
        {
          exists = await Exists().Run(ct);
        }
        catch (Exception ex)
        {
          return Data.Validation.ValidationResult.Failure(
            catalogKey: typeof(TRow).Name,
            errorType: Data.Validation.ValidationErrorType.NotFound,
            message: $"Failed to check if data source exists for '{typeof(TRow).Name}'",
            details: ex.Message
          );
        }

        if (!exists)
        {
          return Data.Validation.ValidationResult.Failure(
            catalogKey: typeof(TRow).Name,
            errorType: Data.Validation.ValidationErrorType.NotFound,
            message: $"Data source for '{typeof(TRow).Name}' does not exist",
            details: "Medium exists check returned false"
          );
        }

        // Attempt to read and deserialize a sample
        try
        {
          var stream = await _medium.ReadStream().Run(ct);
          await using var _ = stream;

          // Deserialize sample rows
          var rows = _reader.DeserializeRows(stream);
          var sample = new List<TRow>();
          var count = 0;

          await foreach (var row in rows.WithCancellation(ct))
          {
            sample.Add(row);
            count++;
            if (count >= sampleSize && sampleSize > 0)
            {
              break;
            }
          }

          return Data.Validation.ValidationResult.Success();
        }
        catch (Data.Validation.SchemaMismatchException ex)
        {
          // Provider raised a structural-mismatch signal (e.g. CSV header not
          // matching schema). Pre-flight surfaces this as SchemaMismatch — the
          // canonically-correct category per ValidationErrorType's own definition,
          // and the same category EFCore's shape validator uses. See Phase F in
          // docs/scratch/extension-conformance-kits.md.
          return Data.Validation.ValidationResult.Failure(
            catalogKey: typeof(TRow).Name,
            errorType: Data.Validation.ValidationErrorType.SchemaMismatch,
            message: ex.Message,
            details: ex.InnerException?.ToString() ?? ex.ToString()
          );
        }
        catch (Exception ex)
        {
          return Data.Validation.ValidationResult.Failure(
            catalogKey: typeof(TRow).Name,
            errorType: Data.Validation.ValidationErrorType.DeserializationError,
            message: $"Failed to deserialize sample data for '{typeof(TRow).Name}'",
            details: ex.Message
          );
        }
      }
    );
  }

  /// <inheritdoc />
  public FlowIO<Data.Validation.ValidationResult> InspectDeep()
  {
    return FlowIO.LiftAsync(
      async (CancellationToken ct) =>
      {
        // Check if medium exists
        bool exists;
        try
        {
          exists = await Exists().Run(ct);
        }
        catch (Exception ex)
        {
          return Data.Validation.ValidationResult.Failure(
            catalogKey: typeof(TRow).Name,
            errorType: Data.Validation.ValidationErrorType.NotFound,
            message: $"Failed to check if data source exists for '{typeof(TRow).Name}'",
            details: ex.Message
          );
        }

        if (!exists)
        {
          return Data.Validation.ValidationResult.Failure(
            catalogKey: typeof(TRow).Name,
            errorType: Data.Validation.ValidationErrorType.NotFound,
            message: $"Data source for '{typeof(TRow).Name}' does not exist",
            details: "Medium exists check returned false"
          );
        }

        // Attempt to read and deserialize all data
        try
        {
          var stream = await _medium.ReadStream().Run(ct);
          await using var _ = stream;

          // Deserialize all rows to validate entire dataset
          var rows = _reader.DeserializeRows(stream);
          var count = 0;

          await foreach (var row in rows.WithCancellation(ct))
          {
            count++;
          }

          return Data.Validation.ValidationResult.Success();
        }
        catch (Data.Validation.SchemaMismatchException ex)
        {
          return Data.Validation.ValidationResult.Failure(
            catalogKey: typeof(TRow).Name,
            errorType: Data.Validation.ValidationErrorType.SchemaMismatch,
            message: ex.Message,
            details: ex.InnerException?.ToString() ?? ex.ToString()
          );
        }
        catch (Exception ex)
        {
          return Data.Validation.ValidationResult.Failure(
            catalogKey: typeof(TRow).Name,
            errorType: Data.Validation.ValidationErrorType.DeserializationError,
            message: $"Failed to deserialize all data for '{typeof(TRow).Name}'",
            details: ex.Message
          );
        }
      }
    );
  }

  /// <inheritdoc/>
  /// <remarks>
  /// Delegates to the underlying storage medium's <c>InspectTarget</c> implementation.
  /// For file-backed media this probes directory existence and write permissions.
  /// </remarks>
  public FlowIO<Data.Validation.ValidationResult> InspectTarget() => _medium.InspectTarget();
}
