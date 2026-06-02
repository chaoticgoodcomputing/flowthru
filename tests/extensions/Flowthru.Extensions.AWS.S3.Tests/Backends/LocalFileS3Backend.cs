using Flowthru.Data.Storage.S3.Local;
using Flowthru.Extensions.AWS.S3.Tests.Support;

namespace Flowthru.Extensions.AWS.S3.Tests.Backends;

/// <summary>
/// Offline backend for <see cref="Contract.S3GatewayLaws{TBackend}"/>: each
/// <see cref="CreateResource"/> yields a <see cref="LocalFileS3Gateway"/> over a
/// fresh temp root directory with its own unique key prefix. No external
/// dependency, no credentials, no network — runs on every PR.
/// </summary>
/// <remarks>
/// Disjoint state is structural: a GUID-keyed temp root per call means no two
/// resources ever share a tree. The created roots are tracked and deleted in
/// <see cref="Cleanup"/>; failed deletes are ignored (best effort).
/// </remarks>
public sealed class LocalFileS3Backend : IS3GatewayBackend
{
  private readonly List<string> _roots = new();
  private readonly object _gate = new();
  private int _counter;

  public S3GatewayContext CreateResource()
  {
    var n = Interlocked.Increment(ref _counter);
    var root = Path.Combine(Path.GetTempPath(), $"flowthru-s3-laws-{Guid.NewGuid():N}");
    lock (_gate)
    {
      _roots.Add(root);
    }

    return new S3GatewayContext(
      Gateway: new LocalFileS3Gateway(root),
      Bucket: "laws-bucket",
      KeyPrefix: $"k{n}/{Guid.NewGuid():N}/");
  }

  public Task Cleanup()
  {
    lock (_gate)
    {
      foreach (var root in _roots)
      {
        if (Directory.Exists(root))
        {
          try { Directory.Delete(root, recursive: true); }
          catch { /* best effort */ }
        }
      }
      _roots.Clear();
    }
    return Task.CompletedTask;
  }
}
