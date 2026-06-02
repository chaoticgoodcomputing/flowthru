using Flowthru.Data.Catalog;
using Flowthru.Data.Schema;
using Flowthru.Data.Storage;
using Flowthru.Hosting;
using Flowthru.Prelude;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Extensions.AWS.S3.Tests;

[FlowthruSchema]
public partial record S3Order
{
  [SerializedLabel("id")]
  public required int Id { get; init; }

  [SerializedLabel("sku")]
  public required string Sku { get; init; }
}

/// <summary>
/// The headline acceptance test: a Catalog Item declared at an <c>s3://</c> path
/// round-trips a Flow Item end-to-end through the resolver-dispatched
/// <see cref="S3StorageMedium"/> — no per-item <c>.WithResolver(...)</c> required.
/// Runs offline over the shipped local stub, so it exercises the full
/// write→read path (and the s3:// path mapping) with no Docker or AWS account.
/// </summary>
[TestFixture]
[Category("AwsS3")]
public class S3BackedJsonItemTests
{
  private string _root = null!;

  [SetUp]
  public void SetUp()
  {
    _root = Path.Combine(Path.GetTempPath(), $"flowthru-s3-jsonitem-{Guid.NewGuid():N}");
    Directory.CreateDirectory(_root);
  }

  [TearDown]
  public void TearDown()
  {
    if (Directory.Exists(_root))
    {
      try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
    }
  }

  private ServiceProvider BuildProvider<TCatalog>() where TCatalog : class
  {
    var services = new ServiceCollection();
    services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
    services.AddFlowthru(b =>
    {
      b.UseLocalS3(_root);
      b.RegisterCatalog<TCatalog>(sp => (TCatalog)Activator.CreateInstance(
        typeof(TCatalog), sp.GetRequiredService<IStorageMediumResolver>())!);
    });
    return services.BuildServiceProvider();
  }

  [Test]
  public async Task JsonItem_AtS3Path_SaveThenLoad_RoundTrips()
  {
    using var sp = BuildProvider<S3OrdersCatalog>();
    var catalog = sp.GetRequiredService<S3OrdersCatalog>();

    var input = new[]
    {
      new S3Order { Id = 1, Sku = "ABC" },
      new S3Order { Id = 2, Sku = "XYZ" },
    };

    var saveResult = await catalog.Orders.Save(input).Run();
    Assert.That(saveResult, Is.InstanceOf<EffResult<FlowUnit>.Success>(),
      $"Save through the s3:// medium should succeed. Got: {saveResult}");

    // The object really landed via the s3:// → {root}/{bucket}/{key} mapping.
    Assert.That(File.Exists(Path.Combine(_root, "demo", "orders.json")), Is.True,
      "The saved object should land at the mapped local-stub path.");

    var loadResult = await catalog.Orders.Load().Run();
    var loaded = ((EffResult<IEnumerable<S3Order>>.Success)loadResult).Value.ToList();

    Assert.That(loaded, Has.Count.EqualTo(2));
    Assert.That(loaded[0], Is.EqualTo(input[0]));
    Assert.That(loaded[1], Is.EqualTo(input[1]));
  }

  [Test]
  public async Task JsonItem_AtS3Path_LoadsPreExistingObject()
  {
    // Seed an object directly under the stub root, as if it already lived in S3.
    var seedDir = Path.Combine(_root, "demo");
    Directory.CreateDirectory(seedDir);
    await File.WriteAllTextAsync(
      Path.Combine(seedDir, "seed.json"),
      """[ { "id": 7, "sku": "SEED" } ]""");

    using var sp = BuildProvider<S3SeedCatalog>();
    var catalog = sp.GetRequiredService<S3SeedCatalog>();

    var loadResult = await catalog.Rows.Load().Run();
    var rows = ((EffResult<IEnumerable<S3Order>>.Success)loadResult).Value.ToList();

    Assert.That(rows, Has.Count.EqualTo(1));
    Assert.That(rows[0].Id, Is.EqualTo(7));
    Assert.That(rows[0].Sku, Is.EqualTo("SEED"));
  }

  // ── Test catalogs ─────────────────────────────────────────────────────────

  private sealed class S3OrdersCatalog : CatalogAbstract
  {
    public S3OrdersCatalog(IStorageMediumResolver resolver) : base(resolver: resolver) { }

    public IItem<IEnumerable<S3Order>> Orders =>
      CreateItem(() => Item.Of<IEnumerable<S3Order>>("Orders")
        .Json()
        .AtPath("s3://demo/orders.json")
        .Build());
  }

  private sealed class S3SeedCatalog : CatalogAbstract
  {
    public S3SeedCatalog(IStorageMediumResolver resolver) : base(resolver: resolver) { }

    public IItem<IEnumerable<S3Order>> Rows =>
      CreateItem(() => Item.Of<IEnumerable<S3Order>>("Rows")
        .Json()
        .AtPath("s3://demo/seed.json")
        .Build());
  }
}
