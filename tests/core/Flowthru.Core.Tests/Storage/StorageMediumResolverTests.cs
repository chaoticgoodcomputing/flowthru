using Flowthru.Data.Storage;
using Flowthru.Hosting;
using Flowthru.Prelude;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Core.Tests.Storage;

/// <summary>
/// Dispatch tests for <see cref="StorageMediumResolver"/> — covers
/// the bare-path / file:// fallback, registered-provider dispatch on
/// non-file schemes, and the "no provider for scheme" diagnostic.
/// </summary>
[TestFixture]
public class StorageMediumResolverTests
{
  // ── Filesystem-only fallback ────────────────────────────────────────

  [Test]
  public void Filesystem_BarePath_ResolvesToFileStorageMedium()
  {
    var medium = StorageMediumResolver.Filesystem.Resolve("/tmp/data.csv");
    Assert.That(medium, Is.InstanceOf<FileStorageMedium>());
    Assert.That(((FileStorageMedium)medium).FilePath, Is.EqualTo("/tmp/data.csv"));
  }

  [Test]
  public void Filesystem_FileUri_ResolvesToFileStorageMediumViaLocalPath()
  {
    var medium = StorageMediumResolver.Filesystem.Resolve("file:///tmp/data.csv");
    Assert.That(medium, Is.InstanceOf<FileStorageMedium>());
    Assert.That(((FileStorageMedium)medium).FilePath, Is.EqualTo("/tmp/data.csv"),
      "file:// URI should round-trip through Uri.LocalPath.");
  }

  [Test]
  public void Filesystem_RelativePath_ResolvesToFileStorageMedium()
  {
    var medium = StorageMediumResolver.Filesystem.Resolve("data/local.csv");
    Assert.That(medium, Is.InstanceOf<FileStorageMedium>());
  }

  [Test]
  public void Filesystem_UnknownScheme_Throws()
  {
    var ex = Assert.Throws<InvalidOperationException>(() =>
      StorageMediumResolver.Filesystem.Resolve("https://example.com/data.csv")
    );
    Assert.That(ex!.Message, Does.Contain("https"));
    Assert.That(ex.Message, Does.Contain("UseHttp"),
      "The diagnostic should hint at the corresponding extension registration.");
  }

  [Test]
  public void Filesystem_EmptyOrWhitespace_Throws()
  {
    Assert.Throws<ArgumentException>(() => StorageMediumResolver.Filesystem.Resolve(""));
    Assert.Throws<ArgumentException>(() => StorageMediumResolver.Filesystem.Resolve("   "));
  }

  // ── Provider dispatch ───────────────────────────────────────────────

  [Test]
  public void Resolve_RegisteredProvider_DispatchesByScheme()
  {
    var fakeProvider = new FakeMediumProvider("custom");
    var resolver = new StorageMediumResolver(new[] { (IStorageMediumProvider)fakeProvider });

    var medium = resolver.Resolve("custom://endpoint/resource");

    Assert.That(medium, Is.InstanceOf<FakeMedium>());
    Assert.That(((FakeMedium)medium).Uri.ToString(), Is.EqualTo("custom://endpoint/resource"));
  }

  [Test]
  public void Resolve_FirstMatchingProviderWins()
  {
    var first = new FakeMediumProvider("custom") { Tag = "first" };
    var second = new FakeMediumProvider("custom") { Tag = "second" };
    var resolver = new StorageMediumResolver(new IStorageMediumProvider[] { first, second });

    var medium = (FakeMedium)resolver.Resolve("custom://x");

    Assert.That(medium.Source.Tag, Is.EqualTo("first"),
      "Registration order matters; first matching provider wins.");
  }

  [Test]
  public void Resolve_BarePath_BypassesProviders()
  {
    // Even with a provider registered, bare paths fall through to the
    // built-in file medium — providers never see file paths.
    var observed = new List<Uri>();
    var fakeProvider = new FakeMediumProvider("custom") { Observed = observed };
    var resolver = new StorageMediumResolver(new[] { (IStorageMediumProvider)fakeProvider });

    var medium = resolver.Resolve("/tmp/data.csv");

    Assert.That(medium, Is.InstanceOf<FileStorageMedium>());
    Assert.That(observed, Is.Empty,
      "Provider list should not be consulted for bare paths.");
  }

  // ── DI integration via AddFlowthru ──────────────────────────────────

  [Test]
  public void AddFlowthru_RegistersResolver_WithRegisteredProviders()
  {
    var services = new ServiceCollection();
    services.AddSingleton<IStorageMediumProvider>(new FakeMediumProvider("custom"));
    services.AddFlowthru(_ => { });
    var sp = services.BuildServiceProvider();

    var resolver = sp.GetRequiredService<IStorageMediumResolver>();
    var medium = resolver.Resolve("custom://target");

    Assert.That(medium, Is.InstanceOf<FakeMedium>(),
      "Providers registered before AddFlowthru should be reachable through the resolved IStorageMediumResolver.");
  }

  [Test]
  public void AddFlowthru_NoProviderRegistered_ResolverFallsBackToFilesystem()
  {
    var services = new ServiceCollection();
    services.AddFlowthru(_ => { });
    var sp = services.BuildServiceProvider();

    var resolver = sp.GetRequiredService<IStorageMediumResolver>();
    var medium = resolver.Resolve("/tmp/data.csv");

    Assert.That(medium, Is.InstanceOf<FileStorageMedium>());
  }

  // ── Test fakes ──────────────────────────────────────────────────────

  private sealed class FakeMediumProvider : IStorageMediumProvider
  {
    private readonly string _scheme;
    public string Tag { get; init; } = "";
    public List<Uri>? Observed { get; init; }

    public FakeMediumProvider(string scheme) => _scheme = scheme;

    public bool CanHandle(Uri uri)
    {
      Observed?.Add(uri);
      return uri.Scheme.Equals(_scheme, StringComparison.OrdinalIgnoreCase);
    }

    public IStorageMedium Create(Uri uri) => new FakeMedium(uri, this);
  }

  private sealed class FakeMedium : IStorageMedium
  {
    public Uri Uri { get; }
    public FakeMediumProvider Source { get; }

    public FakeMedium(Uri uri, FakeMediumProvider source)
    {
      Uri = uri;
      Source = source;
    }

    public StorageTraits Traits => new();
    public FlowIO<Stream> ReadStream() =>
      throw new NotImplementedException("Test fake — read path not exercised here.");
    public FlowIO<FlowUnit> WriteStream(Stream stream) =>
      throw new NotImplementedException();
    public FlowIO<bool> Exists() => FlowIO.Pure(true);
  }
}
