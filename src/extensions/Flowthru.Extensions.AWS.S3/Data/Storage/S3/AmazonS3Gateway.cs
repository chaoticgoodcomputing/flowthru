using System.Net;
using Amazon;
using Amazon.Runtime.Credentials;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace Flowthru.Data.Storage.S3;

/// <summary>
/// Production <see cref="IS3Gateway"/> backed by the AWS SDK. The single place
/// in this extension that references <c>AWSSDK.S3</c> — it translates the neutral
/// seam to <see cref="IAmazonS3"/> calls and the SDK's not-found responses back
/// to neutral results, so nothing above the seam sees a Amazon type.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Credentials come from the standard chain.</strong> The client is
/// built with no explicit credentials, so the AWS SDK resolves them itself —
/// environment variables, the shared profile, or an ECS/EC2 instance role.
/// Endpoint and addressing are configured via <see cref="S3Options"/>; nothing
/// secret is hardcoded or read by Flowthru.
/// </para>
/// <para>
/// <strong>One client for the host's lifetime.</strong> The gateway owns a
/// single <see cref="IAmazonS3"/> (as the HTTP medium owns one
/// <see cref="System.Net.Http.HttpClient"/>) and disposes it when the DI
/// container disposes the gateway singleton.
/// </para>
/// </remarks>
public sealed class AmazonS3Gateway : IS3Gateway, IDisposable
{
  private readonly IAmazonS3 _client;
  private readonly bool _ownsClient;

  /// <summary>Build a gateway from configuration options (the production path).</summary>
  public AmazonS3Gateway(IOptions<S3Options> options)
  {
    if (options is null) throw new ArgumentNullException(nameof(options));
    _client = CreateClient(options.Value);
    _ownsClient = true;
  }

  /// <summary>
  /// Build a gateway over a caller-supplied <see cref="IAmazonS3"/>. The caller
  /// owns the client's lifetime; the gateway does not dispose it. Used for tests
  /// and advanced composition (e.g. a client wired to LocalStack).
  /// </summary>
  public AmazonS3Gateway(IAmazonS3 client)
  {
    _client = client ?? throw new ArgumentNullException(nameof(client));
    _ownsClient = false;
  }

  private static IAmazonS3 CreateClient(S3Options options)
  {
    var config = new AmazonS3Config { Timeout = options.Timeout };
    if (!string.IsNullOrWhiteSpace(options.Region))
    {
      config.RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region);
    }
    if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
    {
      config.ServiceURL = options.ServiceUrl;
    }
    if (options.ForcePathStyle)
    {
      config.ForcePathStyle = true;
    }
    return new AmazonS3Client(config);
  }

  /// <inheritdoc/>
  public async Task<Stream> GetObject(string bucket, string key, CancellationToken ct)
  {
    try
    {
      var response = await _client.GetObjectAsync(bucket, key, ct).ConfigureAwait(false);
      return response.ResponseStream;
    }
    catch (AmazonS3Exception ex) when (IsNotFound(ex))
    {
      throw new FileNotFoundException($"No object at s3://{bucket}/{key}.", $"s3://{bucket}/{key}", ex);
    }
  }

  /// <inheritdoc/>
  public async Task PutObject(string bucket, string key, Stream content, CancellationToken ct)
  {
    if (content is null) throw new ArgumentNullException(nameof(content));

    // The SDK needs a seekable stream (or a known length) to sign and size the
    // upload. Format serializers hand a seekable MemoryStream, so the common
    // path buffers nothing; an unseekable stream is buffered once here.
    Stream uploadStream = content;
    MemoryStream? buffered = null;
    if (!content.CanSeek)
    {
      buffered = new MemoryStream();
      await content.CopyToAsync(buffered, ct).ConfigureAwait(false);
      buffered.Position = 0;
      uploadStream = buffered;
    }

    try
    {
      var request = new PutObjectRequest
      {
        BucketName = bucket,
        Key = key,
        InputStream = uploadStream,
        AutoCloseStream = false,
      };
      await _client.PutObjectAsync(request, ct).ConfigureAwait(false);
    }
    finally
    {
      buffered?.Dispose();
    }
  }

  /// <inheritdoc/>
  public async Task<bool> ObjectExists(string bucket, string key, CancellationToken ct)
  {
    try
    {
      await _client.GetObjectMetadataAsync(bucket, key, ct).ConfigureAwait(false);
      return true;
    }
    catch (AmazonS3Exception ex) when (IsNotFound(ex))
    {
      return false;
    }
  }

  /// <inheritdoc/>
  public async Task DeleteObject(string bucket, string key, CancellationToken ct)
  {
    // S3 DeleteObject is idempotent — a missing key still returns success.
    await _client.DeleteObjectAsync(bucket, key, ct).ConfigureAwait(false);
  }

  /// <inheritdoc/>
  public async Task<string?> GetETag(string bucket, string key, CancellationToken ct)
  {
    try
    {
      var metadata = await _client.GetObjectMetadataAsync(bucket, key, ct).ConfigureAwait(false);
      return NormalizeETag(metadata.ETag);
    }
    catch (AmazonS3Exception ex) when (IsNotFound(ex))
    {
      return null;
    }
  }

  /// <inheritdoc/>
  /// <remarks>
  /// <para>
  /// The access handoff is keyed by a neutral vocabulary a native S3 reader
  /// interprets: <c>region</c>, <c>endpoint</c> (present only when a custom
  /// endpoint is configured), <c>url_style</c> (<c>path</c> when path-style
  /// addressing is forced), and the credential entries
  /// <c>access_key_id</c> / <c>secret_access_key</c> / <c>session_token</c>.
  /// </para>
  /// <para>
  /// Credentials are resolved through the same default chain the client
  /// itself uses — environment variables, shared profile, ECS/EC2 role — so
  /// the handoff carries exactly what a read or write through this gateway
  /// would use. It is minted per call and never stored; an unresolvable
  /// chain throws, which the medium lifts into a <c>FlowIO</c> failure.
  /// </para>
  /// </remarks>
  public async Task<ByteLocation> LocateObject(string bucket, string key, CancellationToken ct)
  {
    ct.ThrowIfCancellationRequested();

    var access = new Dictionary<string, string>();
    var config = _client.Config;
    if (config.RegionEndpoint is not null)
    {
      access["region"] = config.RegionEndpoint.SystemName;
    }
    if (!string.IsNullOrWhiteSpace(config.ServiceURL))
    {
      access["endpoint"] = config.ServiceURL;
    }
    if (config is AmazonS3Config { ForcePathStyle: true })
    {
      access["url_style"] = "path";
    }

    var chain = await DefaultAWSCredentialsIdentityResolver
      .GetCredentialsAsync(config)
      .ConfigureAwait(false);
    var credentials = await chain.GetCredentialsAsync().ConfigureAwait(false);
    if (!string.IsNullOrEmpty(credentials.AccessKey))
    {
      access["access_key_id"] = credentials.AccessKey;
    }
    if (!string.IsNullOrEmpty(credentials.SecretKey))
    {
      access["secret_access_key"] = credentials.SecretKey;
    }
    if (credentials.UseToken)
    {
      access["session_token"] = credentials.Token;
    }

    return new ByteLocation.RemoteUri(new Uri($"s3://{bucket}/{key}"), access);
  }

  /// <inheritdoc/>
  public void Dispose()
  {
    if (_ownsClient)
    {
      _client.Dispose();
    }
  }

  // A missing object, or a missing bucket, both read as "no object here".
  private static bool IsNotFound(AmazonS3Exception ex) =>
    ex.StatusCode == HttpStatusCode.NotFound
    || string.Equals(ex.ErrorCode, "NoSuchKey", StringComparison.Ordinal)
    || string.Equals(ex.ErrorCode, "NoSuchBucket", StringComparison.Ordinal)
    || string.Equals(ex.ErrorCode, "NotFound", StringComparison.Ordinal);

  private static string? NormalizeETag(string? etag)
  {
    if (string.IsNullOrEmpty(etag)) return null;
    return etag.Trim('"');
  }
}
