namespace Flowthru.Data.Storage.S3;

/// <summary>
/// Configuration options for the S3 storage-medium extension. Bound from the
/// <c>Flowthru:S3</c> configuration section by <c>UseS3()</c>; properties not
/// present in configuration retain their defaults.
/// </summary>
/// <remarks>
/// <para>
/// <strong>No credentials live here.</strong> The extension resolves AWS
/// credentials through the standard SDK chain (environment variables, shared
/// profile, ECS/EC2 instance role, etc.) — Flowthru never loads, stores, or
/// sees a secret. These options cover only endpoint and addressing concerns.
/// </para>
/// <para>
/// <strong>S3-compatible stores.</strong> Set <see cref="ServiceUrl"/> and
/// <see cref="ForcePathStyle"/> to target MinIO, LocalStack, Cloudflare R2, or
/// any other S3-API-compatible endpoint instead of AWS.
/// </para>
/// </remarks>
public sealed class S3Options
{
  /// <summary>
  /// AWS region system name (e.g. <c>us-east-1</c>) the client targets. When
  /// <see langword="null"/> (default), the SDK resolves the region from its own
  /// chain (<c>AWS_REGION</c>, profile, instance metadata).
  /// </summary>
  public string? Region { get; set; }

  /// <summary>
  /// Override endpoint URL for an S3-compatible store (e.g.
  /// <c>http://localhost:9000</c> for MinIO/LocalStack). When <see langword="null"/>
  /// (default), the client talks to AWS S3 for the configured region.
  /// </summary>
  public string? ServiceUrl { get; set; }

  /// <summary>
  /// Use path-style addressing (<c>endpoint/bucket/key</c>) instead of
  /// virtual-hosted style (<c>bucket.endpoint/key</c>). Required by MinIO and
  /// LocalStack; harmless against AWS for buckets with compatible names.
  /// Defaults to <c>false</c>.
  /// </summary>
  public bool ForcePathStyle { get; set; }

  /// <summary>
  /// Timeout for individual S3 requests. Defaults to 5 minutes to accommodate
  /// large objects.
  /// </summary>
  public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);

  /// <summary>
  /// Maximum number of <c>s3://</c> object reads the scheduler may run
  /// concurrently across the whole flow. <see cref="int.MaxValue"/> (the
  /// default) is unbounded — the ADR-0019 "network is parallel-safe" posture.
  /// </summary>
  /// <remarks>
  /// Lower this on memory-constrained hosts. A seek-required format (Parquet,
  /// Excel) buffers the whole object into memory to read it, so a wide layer of
  /// concurrent <c>s3://</c> Parquet reads holds many whole objects at once and
  /// can exhaust a small container's memory — surfacing as a non-deterministic
  /// SIGSEGV / OOM / decode-corruption crash (issue #111). A finite cap here
  /// bounds the total concurrent reads (all S3 reads share one memory-domain
  /// conflict key), trading some throughput for a bounded memory ceiling.
  /// Must be ≥ 1.
  /// </remarks>
  public int MaxConcurrentReads { get; set; } = int.MaxValue;
}
