using Flowthru.Core.Data.Storage;

namespace Flowthru.Tests.Kits.Medium;

/// <summary>
/// Abstract conformance suite for <see cref="IStorageMediumProvider"/> implementors.
/// Verifies URI dispatch correctness — a provider claims certain schemes via
/// <see cref="IStorageMediumProvider.CanHandle"/>, and <see cref="IStorageMediumProvider.Create"/>
/// produces a non-null medium for matching URIs.
/// </summary>
public abstract class StorageMediumProviderConformance
{
  /// <summary>Builds a fresh provider instance.</summary>
  protected abstract IStorageMediumProvider CreateProvider();

  /// <summary>URIs the provider should accept (return <c>true</c> from <c>CanHandle</c>).</summary>
  protected abstract IEnumerable<Uri> AcceptedUris { get; }

  /// <summary>URIs the provider should reject. Default includes a few common schemes the
  /// HTTP / S3 / SFTP-style providers don't handle.</summary>
  protected virtual IEnumerable<Uri> RejectedUris =>
    new[]
    {
      new Uri("file:///tmp/example.txt"),
      new Uri("s3://bucket/key"),
      new Uri("sftp://host/path"),
    };

  // ── URI dispatch ─────────────────────────────────────────────────────────

  [Test]
  public void CanHandle_AcceptedUris_ReturnsTrue()
  {
    var provider = CreateProvider();
    foreach (var uri in AcceptedUris)
    {
      Assert.That(
        provider.CanHandle(uri),
        Is.True,
        $"Provider claims to handle {uri.Scheme}:// but CanHandle('{uri}') returned false."
      );
    }
  }

  [Test]
  public void CanHandle_RejectedUris_ReturnsFalse()
  {
    var provider = CreateProvider();
    var accepted = AcceptedUris.Select(u => u.Scheme).ToHashSet();

    foreach (var uri in RejectedUris)
    {
      // Skip rejected URIs whose scheme overlaps with an accepted scheme — the subclass
      // may legitimately use one of the default-rejected schemes.
      if (accepted.Contains(uri.Scheme))
      {
        continue;
      }

      Assert.That(
        provider.CanHandle(uri),
        Is.False,
        $"Provider should not handle {uri.Scheme}:// but CanHandle('{uri}') returned true."
      );
    }
  }

  // ── Create ───────────────────────────────────────────────────────────────

  [Test]
  public void Create_AcceptedUri_ReturnsNonNullMedium()
  {
    var provider = CreateProvider();
    foreach (var uri in AcceptedUris)
    {
      var medium = provider.Create(uri);
      Assert.That(
        medium,
        Is.Not.Null,
        $"Create('{uri}') returned null; provider must produce a medium for any URI it claims to handle."
      );
    }
  }
}
