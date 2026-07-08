using Flowthru.Prelude;
using Flowthru.Validation.Runtime;
using Flowthru.Validation.Runtime.S3;

namespace Flowthru.Data.Storage.S3;

/// <summary>
/// Storage medium for reading and writing bytes against an <c>s3://</c> object.
/// Composes with any format serializer (Csv / Parquet / Json / …), so a Flow
/// targets S3 by writing an <c>s3://bucket/key</c> path on a Catalog Item — no
/// format change required.
/// </summary>
/// <remarks>
/// <para>
/// <strong>The medium owns no AWS knowledge.</strong> Every operation routes
/// through an <see cref="IS3Gateway"/>: the production gateway wraps the AWS SDK,
/// the shipped <see cref="Local.LocalFileS3Gateway"/> stands in offline. Direct
/// construction is mostly useful for tests; production code reaches this medium
/// through the resolver via an <c>s3://</c> path.
/// </para>
/// <para>
/// <strong>Atomic writes.</strong> <see cref="WriteStream"/> is a single object
/// PUT — all-or-nothing at the object level, so a failed write never leaves a
/// partial object behind.
/// </para>
/// </remarks>
public sealed class S3StorageMedium : IStorageMedium, ISupportsFingerprint, ISupportsByteLocation
{
  private readonly IS3Gateway _gateway;
  private readonly string _bucket;
  private readonly string _key;
  private readonly IReadOnlyList<ServiceDependency> _serviceDependencies;

  public S3StorageMedium(IS3Gateway gateway, string bucket, string key, int readCapacity = int.MaxValue)
  {
    _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
    if (string.IsNullOrWhiteSpace(bucket))
    {
      throw new ArgumentException("Bucket cannot be null or whitespace.", nameof(bucket));
    }
    if (string.IsNullOrWhiteSpace(key))
    {
      throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));
    }
    _bucket = bucket;
    _key = key;

    // Only attach the memory-domain read dependency when a finite cap is
    // declared (ADR-0019 opt-in). Unbounded reads carry no dependency, so the
    // scheduler's default behaviour is unchanged. See S3ReadDependency (#111).
    _serviceDependencies = readCapacity >= int.MaxValue
      ? Array.Empty<ServiceDependency>()
      : new ServiceDependency[] { new ServiceDependency.External(new S3ReadDependency(readCapacity)) };
  }

  /// <inheritdoc/>
  public IReadOnlyList<ServiceDependency> ServiceDependencies => _serviceDependencies;

  /// <inheritdoc/>
  /// <remarks>
  /// <see cref="StorageTraits.IsTransactional"/> is <c>true</c>: a single S3
  /// <c>PutObject</c> replaces the object all-or-nothing — a failed write leaves
  /// no torn state — so the honest declaration follows the same atomic-write
  /// reasoning the Sheets adapter uses. <see cref="StorageTraits.CanAppend"/>
  /// stays <c>false</c>: S3 has no in-place append.
  /// </remarks>
  public StorageTraits Traits => new()
  {
    IsPersistent = true,
    CanStream = true,
    IsTransactional = true,
  };

  /// <inheritdoc/>
  public FlowIO<Stream> ReadStream() =>
    FlowIO.LiftAsync(ct => _gateway.GetObject(_bucket, _key, ct));

  /// <inheritdoc/>
  public FlowIO<FlowUnit> WriteStream(Stream stream) =>
    FlowIO.LiftAsync(async ct =>
    {
      if (stream is null) throw new ArgumentNullException(nameof(stream));
      await _gateway.PutObject(_bucket, _key, stream, ct).ConfigureAwait(false);
      return FlowUnit.Default;
    });

  /// <inheritdoc/>
  public FlowIO<bool> Exists() =>
    FlowIO.LiftAsync(ct => _gateway.ObjectExists(_bucket, _key, ct));

  /// <inheritdoc/>
  /// <remarks>
  /// Probes write-reachability the only honest way an object store allows:
  /// PUT a zero-byte sentinel beside the target key, then best-effort delete it
  /// (mirroring the filesystem medium's sentinel probe). A refused PUT — denied
  /// permission, missing bucket, unreachable endpoint — surfaces as a
  /// <see cref="ValidationErrorType.WriteAccessDenied"/> result at pre-flight
  /// rather than a runtime exception. Fail-as-value: never throws.
  /// </remarks>
  public FlowIO<ValidationResult> InspectTarget() =>
    FlowIO.LiftAsync(async ct =>
    {
      var probeKey = $"{_key}.flowthru-probe-{Guid.NewGuid():N}";
      try
      {
        using var empty = new MemoryStream(Array.Empty<byte>());
        await _gateway.PutObject(_bucket, probeKey, empty, ct).ConfigureAwait(false);
        return ValidationResult.Success();
      }
      catch (Exception ex)
      {
        return ValidationResult.Failure(
          catalogKey: $"s3://{_bucket}/{_key}",
          errorType: ValidationErrorType.WriteAccessDenied,
          message: $"Cannot write to s3://{_bucket}/{_key}",
          details: $"A write-probe PUT was refused: {ex.Message}. Verify the bucket exists, "
            + "the endpoint is reachable, and the resolved credentials grant s3:PutObject."
        );
      }
      finally
      {
        try
        {
          await _gateway.DeleteObject(_bucket, probeKey, CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
          // Probe cleanup failure is non-fatal — the sentinel is a zero-byte object.
        }
      }
    });

  /// <inheritdoc/>
  public bool IsAddressable => true;

  /// <inheritdoc/>
  /// <remarks>
  /// Routes through the gateway — the credential owner — so the access
  /// handoff is minted by the same seam that reads and writes the object,
  /// never from ambient state the medium holds. The production gateway
  /// returns the object's <c>s3://</c> URI plus endpoint / region /
  /// credential entries; the file-backed stub returns the backing file's
  /// path directly. No object body is transferred, and the key need not
  /// hold an object yet — a write target is addressable before the first
  /// write.
  /// </remarks>
  public FlowIO<ByteLocation> LocateBytes() =>
    FlowIO.LiftAsync(
      ct => _gateway.LocateObject(_bucket, _key, ct),
      source: $"S3StorageMedium.LocateBytes[s3://{_bucket}/{_key}]"
    );

  /// <inheritdoc/>
  /// <remarks>
  /// Fingerprints the object by its ETag (one HEAD request, no body transfer).
  /// When no object exists — nothing to fingerprint — the call surfaces a FlowIO
  /// failure, which the cache plan records as "fingerprint unknown" and treats as
  /// a cache miss rather than aborting pre-flight.
  /// </remarks>
  public FlowIO<string> Fingerprint() =>
    FlowIO.LiftAsync(
      async ct =>
      {
        var etag = await _gateway.GetETag(_bucket, _key, ct).ConfigureAwait(false);
        if (string.IsNullOrEmpty(etag))
        {
          throw new InvalidOperationException(
            $"Cannot fingerprint s3://{_bucket}/{_key}: no object exists at this key "
            + "(or the store exposes no ETag). The dependent step is treated as a cache miss.");
        }
        return etag;
      },
      source: $"S3StorageMedium.Fingerprint[s3://{_bucket}/{_key}]"
    );
}
