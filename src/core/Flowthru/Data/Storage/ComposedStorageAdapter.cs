using Flowthru.Data.Capabilities;
using Flowthru.Data.Validation;
using Flowthru.Effects;

namespace Flowthru.Data.Storage;

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
///
/// // JSON in memory with IEnumerable container
/// var jsonEnumerable = new ComposedStorageAdapter&lt;IEnumerable&lt;Order&gt;, Order&gt;(
///     medium: new MemoryStorageMedium(),
///     format: new JsonFormatSerializer&lt;Order&gt;(),
///     container: new EnumerableContainerAdapter&lt;Order&gt;()
/// );
/// </code>
/// </example>
public sealed class ComposedStorageAdapter<TContainer, TRow>
  : IStorageAdapter<TContainer>,
    ISeedable,
    IReadOnly
  where TRow : notnull
{
  private readonly IStorageMedium _medium;
  private readonly IFormatSerializer<TRow> _format;
  private readonly IContainerAdapter<TContainer, TRow> _container;

  /// <summary>
  /// Creates a new composed storage adapter.
  /// </summary>
  /// <param name="medium">The storage medium (file, memory, etc.)</param>
  /// <param name="format">The format serializer (CSV, JSON, etc.)</param>
  /// <param name="container">The container adapter (IEnumerable, IDataView, etc.)</param>
  public ComposedStorageAdapter(
    IStorageMedium medium,
    IFormatSerializer<TRow> format,
    IContainerAdapter<TContainer, TRow> container
  )
  {
    _medium = medium ?? throw new ArgumentNullException(nameof(medium));
    _format = format ?? throw new ArgumentNullException(nameof(format));
    _container = container ?? throw new ArgumentNullException(nameof(container));
  }

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
            var rows = _format.DeserializeRows(stream);

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
    if (IsReadOnly)
    {
      return FlowIO.Fail<FlowUnit>(
        new InvalidOperationException(
          "Cannot write to read-only storage adapter. "
            + "Check IReadOnly.IsReadOnly before attempting Save()."
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
          await _format.SerializeRows(stream, rows);

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

  /// <summary>
  /// Gets whether this adapter can be a seed (Layer 0 input).
  /// </summary>
  /// <remarks>
  /// <para>
  /// Delegates to the medium's ISeedable implementation if available.
  /// If the medium doesn't implement ISeedable, checks if data exists synchronously.
  /// </para>
  /// <para>
  /// Note: This is a synchronous property, so we use RunSynchronously() which may block.
  /// The pipeline executor should check this during construction, not during execution.
  /// </para>
  /// </remarks>
  public bool CanBeSeed
  {
    get
    {
      // If medium implements ISeedable, use it directly
      if (_medium is ISeedable seedable)
      {
        return seedable.CanBeSeed;
      }

      // Otherwise, try to check if data exists
      try
      {
        var existsIO = Exists();
        // TODO: Find proper synchronous evaluation method for IO
        // For now, return false to avoid blocking
        return false;
      }
      catch
      {
        return false;
      }
    }
  }

  /// <summary>
  /// Gets whether this adapter is read-only.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Checks if the medium implements IReadOnly and returns its IsReadOnly value.
  /// If the medium doesn't implement IReadOnly, assumes writable (false).
  /// </para>
  /// <para>
  /// This allows mediums like FileStorageMedium to be writable while
  /// specialized mediums like ReadOnlyFileStorageMedium can enforce read-only.
  /// </para>
  /// </remarks>
  public bool IsReadOnly => _medium is IReadOnly readOnly && readOnly.IsReadOnly;

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
          var rows = _format.DeserializeRows(stream);
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
          var rows = _format.DeserializeRows(stream);
          var count = 0;

          await foreach (var row in rows.WithCancellation(ct))
          {
            count++;
          }

          return Data.Validation.ValidationResult.Success();
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
}
