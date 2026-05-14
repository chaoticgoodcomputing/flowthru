using System.Text.Json;
using System.Text.Json.Serialization;
using Flowthru.Data.Schema;
using Flowthru.Data.Schema.Mapping;
using Flowthru.Validation.Runtime;

namespace Flowthru.Data.Storage;

/// <summary>
/// Storage adapter for a single <typeparamref name="T"/> value persisted
/// as a JSON object (not wrapped in an array). Used for items like
/// trained models, computed metrics, or configuration documents — single
/// values that don't fit the row-oriented enumerable shape.
/// </summary>
/// <typeparam name="T">The schema type stored as a single value.</typeparam>
/// <remarks>
/// <para>
/// Two backing shapes are supported. The bare-path constructors wire
/// directly into <see cref="FileStorageMedium"/> and use the
/// optimised file-stream paths for I/O and inspection. The
/// <see cref="IStorageMedium"/> constructors compose over any medium —
/// HTTP, S3, etc. — and route I/O through the medium's stream
/// primitives, which is the path Phase 1 of the smart-caching RFC
/// uses to make <c>Item.Of&lt;T&gt;().Json().AtPath("https://…")</c>
/// work end-to-end.
/// </para>
/// </remarks>
public sealed class SingletonJsonAdapter<T> : IStorageAdapter<T>, ISupportsFingerprint
  where T : notnull, IStructuredSerializable
{
  private readonly IStorageMedium _medium;
  private readonly string? _filePath;
  private readonly JsonSerializerOptions _options;

  /// <summary>
  /// Construct an adapter directly over a filesystem path. Equivalent
  /// to wrapping the path in a <see cref="FileStorageMedium"/>; the
  /// dedicated overload preserves the historical fast-path for callers
  /// that already know the location is local.
  /// </summary>
  public SingletonJsonAdapter(string filePath)
    : this(filePath, DefaultOptions()) { }

  /// <summary>
  /// Construct an adapter directly over a filesystem path with explicit
  /// JSON serialization options.
  /// </summary>
  public SingletonJsonAdapter(string filePath, JsonSerializerOptions options)
  {
    if (string.IsNullOrWhiteSpace(filePath))
    {
      throw new ArgumentException("File path cannot be null or whitespace", nameof(filePath));
    }
    _filePath = filePath;
    _medium = new FileStorageMedium(filePath);
    _options = ConfigureConverters(options);
  }

  /// <summary>
  /// Construct an adapter over an arbitrary <see cref="IStorageMedium"/>
  /// — typically the result of resolving a non-filesystem URI through
  /// an <see cref="IStorageMediumResolver"/>. The medium's
  /// <see cref="IStorageMedium.ReadStream"/> /
  /// <see cref="IStorageMedium.WriteStream"/> primitives are used for
  /// I/O and inspection.
  /// </summary>
  public SingletonJsonAdapter(IStorageMedium medium)
    : this(medium, DefaultOptions()) { }

  /// <summary>
  /// Construct an adapter over an arbitrary <see cref="IStorageMedium"/>
  /// with explicit JSON serialization options.
  /// </summary>
  public SingletonJsonAdapter(IStorageMedium medium, JsonSerializerOptions options)
  {
    _medium = medium ?? throw new ArgumentNullException(nameof(medium));
    // Capture file path for fast-path file-only operations only if this
    // is in fact a FileStorageMedium — otherwise leave null and route
    // everything through the medium's stream primitives.
    _filePath = medium is FileStorageMedium fileMedium ? fileMedium.FilePath : null;
    _options = ConfigureConverters(options);
  }

  private static JsonSerializerOptions DefaultOptions() =>
    new()
    {
      WriteIndented = true,
      PropertyNamingPolicy = null,
      DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

  private static JsonSerializerOptions ConfigureConverters(JsonSerializerOptions options)
  {
    if (options is null) throw new ArgumentNullException(nameof(options));
    options.Converters.Add(new SerializedLabelJsonConverterFactory());
    // [SerializedEnum]-decorated enum properties honor their declared
    // mapping (parallel to JsonFormatSerializer). Without this, enums
    // round-trip as ordinals/member names and the on-disk wire format
    // diverges from every other Flowthru format adapter.
    options.Converters.Add(new SerializedEnumJsonConverterFactory());
    return options;
  }

  /// <inheritdoc/>
  public StorageTraits Traits => _medium.Traits;

  /// <inheritdoc/>
  public FlowIO<T> Load()
  {
    // Fast path: when backed by a filesystem path, open the file
    // directly for streaming deserialization. Avoids buffering the
    // entire document in memory and preserves the historical
    // FileStream-based behaviour byte-for-byte.
    if (_filePath is not null)
    {
      var filePath = _filePath;
      return FlowIO.LiftAsync(async ct =>
      {
        await using var stream = new FileStream(
          filePath,
          FileMode.Open,
          FileAccess.Read,
          FileShare.Read,
          bufferSize: 4096,
          useAsync: true
        );
        var value = await JsonSerializer.DeserializeAsync<T>(stream, _options, ct).ConfigureAwait(false);
        if (value is null)
        {
          throw new InvalidOperationException(
            $"Deserialized null value from singleton JSON file at '{filePath}'."
          );
        }
        return value;
      });
    }

    // Generic path: read through the medium's stream primitives.
    return from stream in _medium.ReadStream()
      from value in FlowIO.LiftAsync(
        async ct =>
        {
          try
          {
            var loaded = await JsonSerializer.DeserializeAsync<T>(stream, _options, ct).ConfigureAwait(false);
            if (loaded is null)
            {
              throw new InvalidOperationException(
                "Deserialized null value from singleton JSON medium."
              );
            }
            return loaded;
          }
          finally
          {
            stream.Dispose();
          }
        },
        source: $"SingletonJsonAdapter.Load[{typeof(T).Name}]"
      )
      select value;
  }

  /// <inheritdoc/>
  public FlowIO<FlowUnit> Save(T data)
  {
    // Fast path: when backed by a filesystem path, write to a temp
    // file and atomically rename — same behaviour the original
    // file-only adapter exposed.
    if (_filePath is not null)
    {
      var filePath = _filePath;
      return FlowIO.LiftAsync(async ct =>
      {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
          Directory.CreateDirectory(directory);
        }
        var tempPath = $"{filePath}.tmp.{Guid.NewGuid():N}";
        try
        {
          await using (
            var fs = new FileStream(
              tempPath,
              FileMode.Create,
              FileAccess.Write,
              FileShare.None,
              bufferSize: 4096,
              useAsync: true
            )
          )
          {
            await JsonSerializer.SerializeAsync(fs, data, _options, ct).ConfigureAwait(false);
          }
          File.Move(tempPath, filePath, overwrite: true);
          return FlowUnit.Default;
        }
        catch
        {
          if (File.Exists(tempPath))
          {
            try { File.Delete(tempPath); } catch { /* cleanup best-effort */ }
          }
          throw;
        }
      });
    }

    // Generic path: serialize into a MemoryStream, then hand it off
    // to the medium's WriteStream. The medium decides atomicity and
    // transport semantics.
    return from buffer in FlowIO.LiftAsync<Stream>(async ct =>
      {
        var stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, data, _options, ct).ConfigureAwait(false);
        stream.Position = 0;
        return stream;
      })
      from result in _medium.WriteStream(buffer)
      select result;
  }

  /// <inheritdoc/>
  public FlowIO<bool> Exists() => _medium.Exists();

  /// <summary>
  /// Cached set of required field names from the planner — built once
  /// per adapter instance so repeated inspections don't re-reflect.
  /// Case-insensitive comparison matches the planner's
  /// <see cref="PropertyMappingPlan{TRow}.ByFieldName"/> lookup policy
  /// — if <c>Load</c> would find a field, <c>InspectShallow</c> reports
  /// it present.
  /// </summary>
  private static readonly IReadOnlySet<string> _requiredFieldNames =
    PropertyMappingPlanner.Build<T>().RequiredFieldNames
      .ToHashSet(StringComparer.OrdinalIgnoreCase);

  /// <inheritdoc/>
  /// <remarks>
  /// Verifies that the on-disk JSON object's top-level properties
  /// include every field the schema declares as required (non-nullable).
  /// Extra fields are tolerated — they're silently ignored on Load —
  /// so the rule is <em>data ⊇ schema</em>. Outcomes:
  /// <list type="bullet">
  ///   <item>Source missing → <c>NotFound</c>.</item>
  ///   <item>Source unreadable / malformed JSON → <c>DeserializationError</c>.</item>
  ///   <item>Top-level is not an object → <c>SchemaMismatch</c>.</item>
  ///   <item>Required fields absent → <c>SchemaMismatch</c> with the missing-field diff.</item>
  ///   <item>All required fields present → <c>Success</c> (values are NOT
  ///     value-validated; that's <c>InspectDeep</c>'s contract).</item>
  /// </list>
  /// </remarks>
  public FlowIO<ValidationResult> InspectShallow(int sampleSize) =>
    FlowIO.LiftAsync<ValidationResult>(async ct =>
    {
      var label = typeof(T).Name;

      // Fast-path File.Exists check when local.
      if (_filePath is not null && !File.Exists(_filePath))
      {
        return ValidationResult.Failure(
          catalogKey: label,
          errorType: ValidationErrorType.NotFound,
          message: $"Singleton JSON file not found at '{_filePath}'"
        );
      }

      // For non-file mediums, use the medium's existence probe.
      if (_filePath is null)
      {
        var existsResult = await _medium.Exists().Run(ct).ConfigureAwait(false);
        if (existsResult is EffResult<bool>.Failure existsFailure)
        {
          return ValidationResult.Failure(
            catalogKey: label,
            errorType: ValidationErrorType.NotFound,
            message: $"Failed to probe existence for singleton JSON '{label}'",
            details: existsFailure.Error.Message
          );
        }
        if (!((EffResult<bool>.Success)existsResult).Value)
        {
          return ValidationResult.Failure(
            catalogKey: label,
            errorType: ValidationErrorType.NotFound,
            message: $"Singleton JSON medium reports no data for '{label}'"
          );
        }
      }

      // Read the document. File-backed paths use a FileStream directly
      // (preserves the original fast-path); other mediums go through
      // ReadStream().
      Stream? stream = null;
      JsonDocument? document = null;
      try
      {
        if (_filePath is not null)
        {
          stream = new FileStream(
            _filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            useAsync: true
          );
        }
        else
        {
          var streamResult = await _medium.ReadStream().Run(ct).ConfigureAwait(false);
          if (streamResult is EffResult<Stream>.Failure streamFailure)
          {
            return ValidationResult.Failure(
              catalogKey: label,
              errorType: ValidationErrorType.NotFound,
              message: $"Failed to open singleton JSON medium for '{label}'",
              details: streamFailure.Error.Message
            );
          }
          stream = ((EffResult<Stream>.Success)streamResult).Value;
        }

        document = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        return ValidationResult.Failure(
          catalogKey: label,
          errorType: ValidationErrorType.DeserializationError,
          message: $"Failed to parse singleton JSON for '{label}'",
          details: ex.Message
        );
      }
      finally
      {
        stream?.Dispose();
      }

      using (document)
      {
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
          return ValidationResult.Failure(
            catalogKey: label,
            errorType: ValidationErrorType.SchemaMismatch,
            message: $"Singleton JSON for '{label}' must be a JSON object at the top level",
            details: $"Found {document.RootElement.ValueKind} at the top level. "
              + "Singleton JSON adapters expect an object shape, not an array or scalar."
          );
        }

        var presentFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in document.RootElement.EnumerateObject())
        {
          presentFields.Add(property.Name);
        }

        var missing = new List<string>();
        foreach (var required in _requiredFieldNames)
        {
          if (!presentFields.Contains(required))
          {
            missing.Add(required);
          }
        }
        if (missing.Count > 0)
        {
          return ValidationResult.Failure(
            catalogKey: label,
            errorType: ValidationErrorType.SchemaMismatch,
            message: $"Singleton JSON for '{label}' is missing required field(s): "
              + string.Join(", ", missing.Select(f => $"'{f}'")),
            details: $"Required by schema: {string.Join(", ", _requiredFieldNames.Select(f => $"'{f}'"))}. "
              + $"Present in data: {string.Join(", ", presentFields.Select(f => $"'{f}'"))}."
          );
        }
        return ValidationResult.Success();
      }
    });

  /// <inheritdoc/>
  /// <remarks>
  /// Deep inspection deserializes the full document into a
  /// <typeparamref name="T"/> instance — verifying every value is
  /// type-compatible with the schema, not just that the required
  /// field names are present. Use for critical inputs; the cost
  /// scales with file size.
  /// </remarks>
  public FlowIO<ValidationResult> InspectDeep() =>
    FlowIO.LiftAsync<ValidationResult>(async ct =>
    {
      var label = typeof(T).Name;

      if (_filePath is not null && !File.Exists(_filePath))
      {
        return ValidationResult.Failure(
          catalogKey: label,
          errorType: ValidationErrorType.NotFound,
          message: $"Singleton JSON file not found at '{_filePath}'"
        );
      }

      if (_filePath is null)
      {
        var existsResult = await _medium.Exists().Run(ct).ConfigureAwait(false);
        if (existsResult is EffResult<bool>.Failure existsFailure)
        {
          return ValidationResult.Failure(
            catalogKey: label,
            errorType: ValidationErrorType.NotFound,
            message: $"Failed to probe existence for singleton JSON '{label}'",
            details: existsFailure.Error.Message
          );
        }
        if (!((EffResult<bool>.Success)existsResult).Value)
        {
          return ValidationResult.Failure(
            catalogKey: label,
            errorType: ValidationErrorType.NotFound,
            message: $"Singleton JSON medium reports no data for '{label}'"
          );
        }
      }

      Stream? stream = null;
      try
      {
        if (_filePath is not null)
        {
          stream = new FileStream(
            _filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            useAsync: true
          );
        }
        else
        {
          var streamResult = await _medium.ReadStream().Run(ct).ConfigureAwait(false);
          if (streamResult is EffResult<Stream>.Failure streamFailure)
          {
            return ValidationResult.Failure(
              catalogKey: label,
              errorType: ValidationErrorType.NotFound,
              message: $"Failed to open singleton JSON medium for '{label}'",
              details: streamFailure.Error.Message
            );
          }
          stream = ((EffResult<Stream>.Success)streamResult).Value;
        }
        _ = await JsonSerializer.DeserializeAsync<T>(stream, _options, ct).ConfigureAwait(false);
        return ValidationResult.Success();
      }
      catch (Exception ex)
      {
        return ValidationResult.Failure(
          catalogKey: label,
          errorType: ValidationErrorType.DeserializationError,
          message: $"Failed to deserialize singleton JSON for '{label}'",
          details: ex.Message
        );
      }
      finally
      {
        stream?.Dispose();
      }
    });

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectTarget() => _medium.InspectTarget();

  /// <inheritdoc/>
  /// <remarks>
  /// Delegates to the underlying <see cref="IStorageMedium"/> when
  /// it implements <see cref="ISupportsFingerprint"/>; otherwise
  /// surfaces a FlowIO failure that the cache plan interprets as
  /// "fingerprint unknown" — the consuming step is treated as a
  /// cache miss rather than aborting pre-flight.
  /// </remarks>
  public FlowIO<string> Fingerprint() =>
    _medium is ISupportsFingerprint fingerprintable
      ? fingerprintable.Fingerprint()
      : FlowIO.Fail<string>(new RuntimeError.External(
          $"SingletonJsonAdapter.Fingerprint[{typeof(T).Name}]",
          new InvalidOperationException(
            $"Underlying storage medium '{_medium.GetType().Name}' does not implement "
            + "ISupportsFingerprint; this singleton JSON adapter cannot produce a leaf fingerprint."
          )));
}
