using System.Net;
using Flowthru.Data.Catalog;
using Flowthru.Data.Schema;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Http;
using Flowthru.Hosting;
using Flowthru.Prelude;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Extensions.Http.Tests;

[FlowthruSchema]
public partial record HttpBackedRow
{
  [SerializedLabel("id")]
  public required int Id { get; init; }

  [SerializedLabel("name")]
  public required string Name { get; init; }
}

[FlowthruSchema]
public partial record HttpBackedSingletonRow
{
  [SerializedLabel("id")]
  public required int Id { get; init; }

  [SerializedLabel("title")]
  public required string Title { get; init; }
}

/// <summary>
/// Phase 1 of the smart-caching-and-slicing RFC. Verifies that with
/// <c>UseHttp()</c> registered, a catalog declaring
/// <c>Item.Of&lt;T&gt;("x").Json().AtPath("https://…").Build()</c>
/// loads end-to-end through the resolver-dispatched
/// <see cref="HttpStorageMedium"/> — no per-item <c>.WithResolver(...)</c>
/// required.
/// </summary>
[TestFixture]
[Category("Http")]
public class HttpBackedJsonItemTests
{
  [Test]
  public async Task JsonArray_AtHttpsPath_LoadsThroughResolvedHttpMedium()
  {
    const string body = """
      [
        { "id": 1, "name": "Alice" },
        { "id": 2, "name": "Bob" }
      ]
      """;

    var fake = new FakeHandler(HttpStatusCode.OK, body);
    var services = new ServiceCollection();
    services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
    services.AddSingleton<IStorageMediumProvider>(
      new HttpStorageMediumProvider(new HttpClient(fake))
    );
    services.AddFlowthru(b =>
      b.RegisterCatalog<HttpArrayCatalog>(sp =>
        new HttpArrayCatalog(sp.GetRequiredService<IStorageMediumResolver>())
      )
    );
    var sp = services.BuildServiceProvider();

    var catalog = sp.GetRequiredService<HttpArrayCatalog>();
    var loadResult = await catalog.Rows.Load().Run();

    Assert.That(loadResult, Is.InstanceOf<EffResult<IEnumerable<HttpBackedRow>>>());
    var success = loadResult as EffResult<IEnumerable<HttpBackedRow>>.Success;
    Assert.That(success, Is.Not.Null,
      $"Load should succeed end-to-end. Got: {loadResult}");
    var rows = success!.Value.ToList();
    Assert.That(rows, Has.Count.EqualTo(2));
    Assert.That(rows[0].Id, Is.EqualTo(1));
    Assert.That(rows[0].Name, Is.EqualTo("Alice"));
    Assert.That(rows[1].Id, Is.EqualTo(2));
    Assert.That(rows[1].Name, Is.EqualTo("Bob"));
    Assert.That(fake.Requests, Has.Count.GreaterThanOrEqualTo(1),
      "The fake handler should have received at least one GET.");
  }

  [Test]
  public async Task JsonSingleton_AtHttpsPath_LoadsThroughResolvedHttpMedium()
  {
    const string body = """
      { "id": 42, "title": "Hello" }
      """;

    var fake = new FakeHandler(HttpStatusCode.OK, body);
    var services = new ServiceCollection();
    services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
    services.AddSingleton<IStorageMediumProvider>(
      new HttpStorageMediumProvider(new HttpClient(fake))
    );
    services.AddFlowthru(b =>
      b.RegisterCatalog<HttpSingletonCatalog>(sp =>
        new HttpSingletonCatalog(sp.GetRequiredService<IStorageMediumResolver>())
      )
    );
    var sp = services.BuildServiceProvider();

    var catalog = sp.GetRequiredService<HttpSingletonCatalog>();
    var loadResult = await catalog.Item.Load().Run();

    Assert.That(loadResult, Is.InstanceOf<EffResult<HttpBackedSingletonRow>.Success>(),
      $"Singleton load should succeed end-to-end. Got: {loadResult}");
    var value = ((EffResult<HttpBackedSingletonRow>.Success)loadResult).Value;
    Assert.That(value.Id, Is.EqualTo(42));
    Assert.That(value.Title, Is.EqualTo("Hello"));
  }

  // ── Test catalogs ─────────────────────────────────────────────────────────

  private sealed class HttpArrayCatalog : CatalogAbstract
  {
    public HttpArrayCatalog(IStorageMediumResolver resolver)
      : base(resolver: resolver) { }

    public IItem<IEnumerable<HttpBackedRow>> Rows =>
      CreateItem(() => Item.Of<IEnumerable<HttpBackedRow>>("Rows")
        .Json()
        .AtPath("https://example.com/rows.json")
        .Build());
  }

  private sealed class HttpSingletonCatalog : CatalogAbstract
  {
    public HttpSingletonCatalog(IStorageMediumResolver resolver)
      : base(resolver: resolver) { }

    public IItem<HttpBackedSingletonRow> Item =>
      CreateItem(() => Flowthru.Data.Catalog.Item.Of<HttpBackedSingletonRow>("Item")
        .Json()
        .AtPath("https://example.com/item.json")
        .Build());
  }
}
