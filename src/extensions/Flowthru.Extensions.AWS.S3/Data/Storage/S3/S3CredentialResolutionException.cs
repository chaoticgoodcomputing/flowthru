namespace Flowthru.Data.Storage.S3;

/// <summary>
/// A credential-resolution failure at the S3 gateway's reveal site
/// (<see cref="AmazonS3Gateway.LocateObject"/>). Its message carries the
/// bucket, key, and the offending exception's <em>type name</em> only — never
/// the underlying AWS SDK exception, which can echo endpoint or request context.
/// It retains no inner cause, so nothing sensitive can be reconstructed from the
/// error as it flows through the <c>FlowIO</c> channel to a persisted run
/// record. See ADR-0026's reveal-site containment.
/// </summary>
public sealed class S3CredentialResolutionException : Exception
{
  public S3CredentialResolutionException(string bucket, string key, string causeType)
    : base(
      $"Could not resolve AWS credentials for s3://{bucket}/{key} "
      + $"(resolution failed with {causeType}). Verify the credential chain — "
      + "environment variables, the shared profile, or an instance role.")
  {
    Bucket = bucket;
    Key = key;
    CauseType = causeType;
  }

  /// <summary>The bucket whose credentials could not be resolved.</summary>
  public string Bucket { get; }

  /// <summary>The object key whose credentials could not be resolved.</summary>
  public string Key { get; }

  /// <summary>The type name of the underlying failure — no message, no secret.</summary>
  public string CauseType { get; }
}
