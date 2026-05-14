using Flowthru.Data.Catalog;
using Flowthru.Data.Schema;
using Flowthru.Data.Storage;
using Flowthru.Hosting;
using Flowthru.Prelude;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Core.Tests.Catalog;

[FlowthruSchema]
public partial record AmbientRow
{
  [SerializedLabel("id")]
  public required int Id { get; init; }

  [SerializedLabel("payload")]
  public required string Payload { get; init; }
}

/// <summary>
/// Phase 1 of the smart-caching-and-slicing RFC. Verifies that
/// <see cref="CatalogAbstract.CreateItem{T}"/> pushes the catalog's
/// DI-resolved <see cref="IStorageMediumResolver"/> into the ambient
/// slot during factory invocation, so format builders can pick it up
/// without per-item <c>.WithResolver(...)</c> ceremony.
/// </summary>
[TestFixture]
public class CatalogAmbientResolverTests
{
  [Test]
  public void CreateItem_WithCatalogResolver_PushesAmbientDuringFactory()
  {
    // The catalog is constructed with a resolver; during CreateItem<T>(...)
    // the factory closure runs inside an ambient scope so a builder
    // calling .AtPath("custom://...") resolves through the resolver.
    var resolver = new StorageMediumResolver(
      new IStorageMediumProvider[] { new FakeMediumProvider("custom") }
    );
    var catalog = new ResolverAwareCatalog(resolver);

    // Outside CreateItem, no ambient is set.
    Assert.That(StorageMediumResolver.Current, Is.Null);

    var item = catalog.SingletonItem;

    Assert.That(item, Is.Not.Null);
    Assert.That(item.Label, Is.EqualTo("SingletonItem"));

    // After CreateItem returns, the ambient must unwind.
    Assert.That(StorageMediumResolver.Current, Is.Null,
      "Ambient scope must unwind once the factory returns.");
  }

  [Test]
  public void CreateItem_WithoutCatalogResolver_FallsBackToFilesystem()
  {
    // A catalog constructed without a resolver behaves as before — only
    // bare paths and file:// resolve; non-file schemes throw with the
    // standard diagnostic.
    var catalog = new ResolverlessCatalog();

    // Bare-path items still resolve.
    Assert.That(catalog.LocalItem, Is.Not.Null);

    // Non-file items throw with the schema diagnostic.
    Assert.Throws<InvalidOperationException>(() => _ = catalog.RemoteItem);
  }

  [Test]
  public void DICatalog_ResolvedThroughFlowthruService_ReceivesAmbientResolver()
  {
    // End-to-end DI: register a fake-scheme provider, then a catalog that
    // declares an item with a non-file URI. The catalog must transparently
    // pick up the resolver from DI so .AtPath("custom://...") works.
    var services = new ServiceCollection();
    services.AddSingleton<IStorageMediumProvider>(new FakeMediumProvider("custom"));
    services.AddFlowthru(b => b.RegisterCatalog<DIResolverAwareCatalog>(sp =>
      new DIResolverAwareCatalog(sp.GetRequiredService<IStorageMediumResolver>())
    ));
    var sp = services.BuildServiceProvider();

    var catalog = sp.GetRequiredService<DIResolverAwareCatalog>();
    var item = catalog.RemoteItem;

    Assert.That(item, Is.Not.Null);
    Assert.That(item.Label, Is.EqualTo("RemoteItem"));
  }

  // ── Test catalogs ─────────────────────────────────────────────────────────

  private sealed class ResolverAwareCatalog : CatalogAbstract
  {
    public ResolverAwareCatalog(IStorageMediumResolver resolver)
      : base(resolver: resolver) { }

    public IItem<AmbientRow> SingletonItem =>
      CreateItem(() => Item.Of<AmbientRow>("SingletonItem")
        .Json()
        .AtPath("custom://endpoint/data.json")
        .Build());
  }

  private sealed class ResolverlessCatalog : CatalogAbstract
  {
    public IItem<IEnumerable<AmbientRow>> LocalItem =>
      CreateItem(() => Item.Of<IEnumerable<AmbientRow>>("LocalItem")
        .Json()
        .AtPath("/tmp/local.json")
        .Build());

    public IItem<AmbientRow> RemoteItem =>
      CreateItem(() => Item.Of<AmbientRow>("RemoteItem")
        .Json()
        .AtPath("https://example.com/data.json")
        .Build());
  }

  private sealed class DIResolverAwareCatalog : CatalogAbstract
  {
    public DIResolverAwareCatalog(IStorageMediumResolver resolver)
      : base(resolver: resolver) { }

    public IItem<AmbientRow> RemoteItem =>
      CreateItem(() => Item.Of<AmbientRow>("RemoteItem")
        .Json()
        .AtPath("custom://endpoint/data.json")
        .Build());
  }

  // ── Fakes ─────────────────────────────────────────────────────────────────

  private sealed class FakeMediumProvider : IStorageMediumProvider
  {
    private readonly string _scheme;
    public FakeMediumProvider(string scheme) => _scheme = scheme;
    public bool CanHandle(Uri uri) =>
      uri.Scheme.Equals(_scheme, StringComparison.OrdinalIgnoreCase);
    public IStorageMedium Create(Uri uri) => new FakeMedium(uri);
  }

  private sealed class FakeMedium : IStorageMedium
  {
    public Uri Uri { get; }
    public FakeMedium(Uri uri) => Uri = uri;
    public StorageTraits Traits => new();
    public FlowIO<Stream> ReadStream() =>
      FlowIO.LiftAsync<Stream>(_ => throw new NotImplementedException("Test fake."));
    public FlowIO<FlowUnit> WriteStream(Stream stream) =>
      FlowIO.LiftAsync<FlowUnit>(_ => throw new NotImplementedException());
    public FlowIO<bool> Exists() => FlowIO.Pure(true);
  }
}
