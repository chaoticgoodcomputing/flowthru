namespace Flowthru.Validation.Runtime.S3;

/// <summary>
/// Conflict identity of the memory an <c>s3://</c> read consumes (ADR-0019,
/// issue #111). Surfaced through Core's <see cref="ServiceDependency.External"/>
/// so <c>ParallelFlowScheduler</c> can bound how many S3 reads run at once.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Why a shared identity.</strong> Unlike an EF Core database (one
/// conflict key per physical database) the constrained resource here is
/// <em>process read-buffer memory</em>, not any one object: a read of an
/// <c>s3://</c> object with a seek-required format (Parquet, Excel) buffers the
/// whole object into memory and materialises row groups, so N concurrent reads
/// hold N objects' worth of memory <em>regardless of which objects they are</em>.
/// The <see cref="DagId"/> is therefore a constant shared by every S3 item, so
/// all buffered reads contend on one key and the scheduler bounds their total
/// concurrency — not a per-object key that would leave 34 distinct objects
/// unconstrained (the exact shape that crash-loops in #111).
/// </para>
/// <para>
/// <strong>Reads only.</strong> <see cref="WriteCapacity"/> is unbounded: an S3
/// write is a single-object PUT already guarded by the single-producer law, and
/// the op-class is part of the key (<c>Read:</c> vs <c>Write:</c>), so bounding
/// reads never serialises writes. The read bound is opt-in — unbounded by
/// default (this dependency is only attached when a capacity is declared via
/// <c>S3Options.MaxConcurrentReads</c>), keeping the ADR-0019 "network is
/// ∞ by default" posture while giving memory-constrained hosts a safe cap.
/// </para>
/// </remarks>
internal sealed record S3ReadDependency(int ReadCapacity)
  : IExtensionServiceDependency, ICapacityConstrainable
{
  /// <inheritdoc/>
  public string DagId => "s3:read";

  /// <inheritdoc/>
  public string DisplayName => "S3 read buffer";

  /// <inheritdoc/>
  public string Category => "aws-s3";

  /// <summary>Unbounded — S3 writes are single-object PUTs under the single-producer law.</summary>
  public int WriteCapacity => int.MaxValue;

  /// <inheritdoc/>
  public IExtensionServiceDependency ClampTo(int writeCapacity, int readCapacity) =>
    this with { ReadCapacity = Math.Min(ReadCapacity, readCapacity) };
}
