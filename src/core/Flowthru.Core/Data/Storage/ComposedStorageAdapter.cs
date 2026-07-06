namespace Flowthru.Data.Storage;

/// <summary>
/// Composes <see cref="IStorageMedium"/> + <see cref="IFormatRowReader{TRow}"/>
/// (+ optional <see cref="IFormatRowWriter{TRow}"/>) +
/// <see cref="IContainerAdapter{TContainer, TRow}"/> into a complete
/// <see cref="IStorageAdapter{T}"/>. The canonical composition strategy
/// when medium / format / container are orthogonal (file-system formats —
/// JSON over filesystem, CSV over filesystem, Parquet over filesystem).
/// </summary>
/// <typeparam name="TContainer">The in-memory container type.</typeparam>
/// <typeparam name="TRow">The row type the format handles.</typeparam>
/// <remarks>
/// <para>
/// Read-only formats (e.g., Excel via ExcelDataReader) implement
/// <see cref="IFormatRowReader{TRow}"/> only and are wired through the
/// reader-only constructor; the resulting adapter reports
/// <see cref="StorageTraits.CanWrite"/> = <c>false</c> and <c>Save</c>
/// fails fast.
/// </para>
/// </remarks>
public sealed class ComposedStorageAdapter<TContainer, TRow>
  : IStorageAdapter<TContainer>, ISupportsFingerprint, IHasServiceDependencies
  where TRow : notnull
{
  private readonly IStorageMedium _medium;
  private readonly IFormatRowReader<TRow> _reader;
  private readonly IFormatRowWriter<TRow>? _writer;
  private readonly IContainerAdapter<TContainer, TRow> _container;

  /// <summary>
  /// Constructs an adapter from a full-duplex format serializer (read +
  /// write). The serializer is wired into both segments.
  /// </summary>
  public ComposedStorageAdapter(
    IStorageMedium medium,
    IFormatSerializer<TRow> format,
    IContainerAdapter<TContainer, TRow> container
  )
    : this(medium, format, format, container) { }

  /// <summary>
  /// Constructs an adapter with separate reader and writer segments.
  /// Pass <c>null</c> for the writer to construct a read-only adapter.
  /// </summary>
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
  /// Composed traits — most-restrictive wins on constraints; both layers
  /// must agree on capabilities. Medium-level traits dominate; format
  /// contributes the streaming capability.
  /// </remarks>
  public StorageTraits Traits =>
    new()
    {
      CanRead = _medium.Traits.CanRead,
      CanWrite = _medium.Traits.CanWrite && _writer is not null && _writer.Traits.CanWrite,
      IsPersistent = _medium.Traits.IsPersistent,
      CanStream = _medium.Traits.CanStream && _reader.Traits.CanStream,
      CanAppend = _medium.Traits.CanAppend,
      IsTransactional = _medium.Traits.IsTransactional,
      // Concurrency capacity is a medium-level property (the endpoint, not
      // the format, is the contended resource); carry it through honestly.
      WriteCapacity = _medium.Traits.WriteCapacity,
      ReadCapacity = _medium.Traits.ReadCapacity,
    };

  /// <inheritdoc/>
  /// <remarks>
  /// Surfaces the underlying medium's conflict resources (ADR-0019) so a
  /// composed item — e.g. JSON over a rate-limited HTTP endpoint — gates
  /// the same way a direct adapter does. File-backed mediums declare none,
  /// so file-format items stay ungated.
  /// </remarks>
  public IReadOnlyList<ServiceDependency> ServiceDependencies => _medium.ServiceDependencies;

  /// <inheritdoc/>
  /// <remarks>
  /// Wraps the row-iteration in a <see cref="FlowIO.LiftAsync{A}"/>
  /// boundary. Format extensions throw
  /// <see cref="SchemaMismatchException"/> from inside the row stream
  /// to signal a structural mismatch (header / column / shape) — the
  /// `IAsyncEnumerable` iterator can't fail a <see cref="FlowIO{A}"/>
  /// directly from a <c>yield return</c>, so an exception is the
  /// only way out. We re-translate that to the typed
  /// <see cref="RuntimeError.SchemaMismatch"/> here so consumers see
  /// the failure as a value with full pattern-match fidelity, not as
  /// a generic <see cref="RuntimeError.External"/> wrapping the
  /// exception.
  /// </remarks>
  public FlowIO<TContainer> Load() =>
    from stream in _medium.ReadStream()
    from container in FlowIO.LiftAsync(
      async ct =>
      {
        try
        {
          var rows = _reader.DeserializeRows(stream, ct);
          return await _container.FromRows(rows).ConfigureAwait(false);
        }
        finally
        {
          stream.Dispose();
        }
      },
      source: $"ComposedStorageAdapter.Load[{typeof(TRow).Name}]"
    ).MapError(TranslateSchemaMismatch)
    select container;

  /// <summary>
  /// Lift a wrapped <see cref="SchemaMismatchException"/> out of the
  /// generic <see cref="RuntimeError.External"/> envelope into the
  /// typed <see cref="RuntimeError.SchemaMismatch"/> variant. Other
  /// errors pass through unchanged.
  /// </summary>
  private static RuntimeError TranslateSchemaMismatch(RuntimeError error) =>
    error is RuntimeError.External external && external.Cause is SchemaMismatchException smx
      ? new RuntimeError.SchemaMismatch(
          Source: external.Source,
          Detail: smx.Message,
          InnerExceptionInfo: smx.InnerException?.ToString()
        )
      : error;

  /// <inheritdoc/>
  public FlowIO<FlowUnit> Save(TContainer data)
  {
    if (!Traits.CanWrite)
    {
      return FlowIO.Fail<FlowUnit>(
        new RuntimeError.External(
          "ComposedStorageAdapter.Save",
          new InvalidOperationException(
            "Cannot write to a read-only storage adapter. "
              + "Verify StorageTraits.CanWrite before calling Save()."
          )
        )
      );
    }

    return from memStream in FlowIO.LiftAsync<Stream>(async ct =>
      {
        var stream = new MemoryStream();
        var rows = _container.ToRows(data);
        await _writer!.SerializeRows(stream, rows).ConfigureAwait(false);
        stream.Position = 0;
        return stream;
      })
      from result in _medium.WriteStream(memStream)
      select result;
  }

  /// <inheritdoc/>
  public FlowIO<bool> Exists() => _medium.Exists();

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectShallow(int sampleSize) =>
    FlowIO.LiftAsync<ValidationResult>(async ct =>
      await InspectInternal(ct, sampleSize, full: false).ConfigureAwait(false)
    );

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectDeep() =>
    FlowIO.LiftAsync<ValidationResult>(async ct =>
      await InspectInternal(ct, sampleSize: 0, full: true).ConfigureAwait(false)
    );

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectTarget() => _medium.InspectTarget();

  /// <inheritdoc/>
  /// <remarks>
  /// Delegates to the underlying <see cref="IStorageMedium"/> when it
  /// implements <see cref="ISupportsFingerprint"/>. When the medium
  /// does not (e.g. <see cref="MemoryStorageAdapter{T}"/>-style
  /// composition or extension-supplied mediums without metadata
  /// validators), the composed adapter surfaces a FlowIO failure so
  /// the cache plan can record "fingerprint unknown" for the
  /// dependent step. Pre-flight is not aborted; the step is
  /// downgraded to a cache miss instead.
  /// </remarks>
  public FlowIO<string> Fingerprint() =>
    _medium is ISupportsFingerprint fingerprintable
      ? fingerprintable.Fingerprint()
      : FlowIO.Fail<string>(new RuntimeError.External(
          $"ComposedStorageAdapter.Fingerprint[{typeof(TRow).Name}]",
          new InvalidOperationException(
            $"Underlying storage medium '{_medium.GetType().Name}' does not implement "
            + "ISupportsFingerprint; this composed adapter cannot produce a leaf fingerprint."
          )));

  // ── Inspection core ────────────────────────────────────────────────────

  private async Task<ValidationResult> InspectInternal(
    CancellationToken ct,
    int sampleSize,
    bool full
  )
  {
    var label = typeof(TRow).Name;

    // Check existence; treat a Run-level Failure as a NotFound finding so
    // the result is a valid ValidationResult rather than a FlowIO failure.
    var existsResult = await Exists().Run(ct).ConfigureAwait(false);
    if (existsResult is EffResult<bool>.Failure ef)
    {
      return ValidationResult.Failure(
        catalogKey: label,
        errorType: ValidationErrorType.NotFound,
        message: $"Failed to check existence for '{label}'",
        details: ef.Error.Message
      );
    }

    var exists = ((EffResult<bool>.Success)existsResult).Value;
    if (!exists)
    {
      return ValidationResult.Failure(
        catalogKey: label,
        errorType: ValidationErrorType.NotFound,
        message: $"Data source for '{label}' does not exist",
        details: "Medium exists check returned false"
      );
    }

    // Read and (lightly or fully) iterate rows.
    var streamResult = await _medium.ReadStream().Run(ct).ConfigureAwait(false);
    if (streamResult is EffResult<Stream>.Failure sf)
    {
      return ValidationResult.Failure(
        catalogKey: label,
        errorType: ValidationErrorType.NotFound,
        message: $"Failed to open data source for '{label}'",
        details: sf.Error.Message
      );
    }

    var stream = ((EffResult<Stream>.Success)streamResult).Value;
    try
    {
      var rows = _reader.DeserializeRows(stream, ct);
      var count = 0;
      await foreach (var _ in rows.WithCancellation(ct).ConfigureAwait(false))
      {
        count++;
        if (!full && sampleSize > 0 && count >= sampleSize)
        {
          break;
        }
      }
      return ValidationResult.Success();
    }
    catch (SchemaMismatchException ex)
    {
      return ValidationResult.Failure(
        catalogKey: label,
        errorType: ValidationErrorType.SchemaMismatch,
        message: ex.Message,
        details: ex.InnerException?.ToString() ?? ex.ToString()
      );
    }
    catch (Exception ex)
    {
      return ValidationResult.Failure(
        catalogKey: label,
        errorType: ValidationErrorType.DeserializationError,
        message: $"Failed to deserialize {(full ? "all" : "sample")} data for '{label}'",
        details: ex.Message
      );
    }
    finally
    {
      stream.Dispose();
    }
  }
}
