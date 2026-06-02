namespace Flowthru.Data.Storage.S3;

/// <summary>
/// <see cref="IStorageMediumProvider"/> for the <c>s3://</c> scheme. Registered
/// by <c>UseS3()</c> as a singleton; the host-resolved
/// <see cref="StorageMediumResolver"/> picks it up via DI and routes
/// <c>s3://bucket/key</c> URIs through it to an <see cref="S3StorageMedium"/>.
/// </summary>
/// <remarks>
/// The provider depends only on the <see cref="IS3Gateway"/> seam, so the same
/// provider serves both the production AWS-backed gateway and the offline local
/// stub — the swap happens at gateway registration, never here.
/// </remarks>
public sealed class S3StorageMediumProvider : IStorageMediumProvider
{
  private readonly IS3Gateway _gateway;

  public S3StorageMediumProvider(IS3Gateway gateway)
  {
    _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
  }

  /// <inheritdoc/>
  public bool CanHandle(Uri uri) => uri.Scheme == "s3";

  /// <inheritdoc/>
  public IStorageMedium Create(Uri uri)
  {
    // s3://bucket/key/path → host = bucket, path = /key/path.
    var bucket = uri.Host;
    var key = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));

    if (string.IsNullOrWhiteSpace(bucket))
    {
      throw new InvalidOperationException(
        $"S3 URI '{uri}' has no bucket; expected s3://bucket/key.");
    }
    if (string.IsNullOrWhiteSpace(key))
    {
      throw new InvalidOperationException(
        $"S3 URI '{uri}' has no object key; expected s3://bucket/key.");
    }

    return new S3StorageMedium(_gateway, bucket, key);
  }
}
