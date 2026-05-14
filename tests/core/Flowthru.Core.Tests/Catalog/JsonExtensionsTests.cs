using Flowthru.Data.Catalog;
using Flowthru.Data.Schema;
using Flowthru.Data.Storage;
using Flowthru.Prelude;

namespace Flowthru.Core.Tests.Catalog;

/// <summary>
/// Local schemas used by <see cref="JsonExtensionsTests"/>. Inline here so
/// the test project remains self-contained — the kits' fixture schemas
/// are excluded from the FP rewrite phase.
/// </summary>
[FlowthruSchema]
public partial record JsonExtSingletonRow
{
  [SerializedLabel("id")]
  public required int Id { get; init; }

  [SerializedLabel("name")]
  public required string Name { get; init; }
}

[FlowthruSchema]
public partial record JsonExtArrayRow
{
  [SerializedLabel("id")]
  public required int Id { get; init; }

  [SerializedLabel("value")]
  public required string Value { get; init; }
}

/// <summary>
/// Unit-tests for <see cref="JsonSingletonBuilder{T}"/> and
/// <see cref="JsonArrayBuilder{TRow}"/> resolver-dispatch behavior.
/// Phase 1 of the smart-caching-and-slicing RFC: builders fall back
/// through an ambient <see cref="StorageMediumResolver.Current"/> slot
/// before defaulting to <see cref="StorageMediumResolver.Filesystem"/>,
/// so end-users don't need to thread <c>.WithResolver(...)</c> through
/// every <c>Item.Of&lt;T&gt;()</c> declaration.
/// </summary>
[TestFixture]
public class JsonExtensionsTests
{
  // ── JsonSingletonBuilder: explicit-resolver parity ────────────────────────

  [Test]
  public void JsonSingletonBuilder_WithResolver_ResolvesNonFileSchemeViaProvider()
  {
    // The builder accepts an explicit IStorageMediumResolver and, when
    // given a URI with a non-file scheme, dispatches through it instead
    // of falling back to FileStorageMedium.
    var resolver = new StorageMediumResolver(
      new IStorageMediumProvider[] { new FakeMediumProvider("custom") }
    );
    var item = Item.Of<JsonExtSingletonRow>("singleton-explicit")
      .Json()
      .AtPath("custom://endpoint/data.json")
      .WithResolver(resolver)
      .Build();

    Assert.That(item.Label, Is.EqualTo("singleton-explicit"),
      "Build must preserve the label set by Item.Of(...).");
  }

  [Test]
  public void JsonSingletonBuilder_AmbientResolver_UsedWhenNoExplicitResolver()
  {
    // When no .WithResolver(...) is set, the builder consults
    // StorageMediumResolver.Current. This is what CatalogAbstract.CreateItem
    // pushes during item materialization — the mechanism that lets
    // .AtPath("https://…") work without ceremony.
    var resolver = new StorageMediumResolver(
      new IStorageMediumProvider[] { new FakeMediumProvider("custom") }
    );

    using var _ = StorageMediumResolver.PushAmbient(resolver);

    var item = Item.Of<JsonExtSingletonRow>("singleton-ambient")
      .Json()
      .AtPath("custom://endpoint/data.json")
      .Build();

    Assert.That(item.Label, Is.EqualTo("singleton-ambient"));
  }

  [Test]
  public void JsonSingletonBuilder_AtPathNonFileScheme_NoAmbientNoExplicit_ThrowsWithDiagnostic()
  {
    // Without an ambient resolver and without .WithResolver(...), the
    // builder falls back to Filesystem-only, which throws for non-file
    // schemes. The diagnostic must name the scheme so the user can fix
    // the registration.
    var anchor = Item.Of<JsonExtSingletonRow>("singleton-bad")
      .Json()
      .AtPath("https://example.com/data.json");

    var ex = Assert.Throws<InvalidOperationException>(() => anchor.Build());
    Assert.That(ex!.Message, Does.Contain("https"),
      "Diagnostic must name the scheme that failed to dispatch.");
    Assert.That(ex.Message, Does.Contain("UseHttp"),
      "Diagnostic must hint at the corresponding extension registration.");
  }

  [Test]
  public void JsonSingletonBuilder_BarePath_DoesNotConsultProviders()
  {
    // A bare filesystem path must bypass providers entirely and resolve
    // to FileStorageMedium even when an ambient resolver with providers
    // is set. This preserves the existing local-filesystem fast path.
    var observed = new List<Uri>();
    var resolver = new StorageMediumResolver(
      new IStorageMediumProvider[] { new FakeMediumProvider("custom") { Observed = observed } }
    );
    using var _ = StorageMediumResolver.PushAmbient(resolver);

    var item = Item.Of<JsonExtSingletonRow>("singleton-local")
      .Json()
      .AtPath("/tmp/data.json")
      .Build();

    Assert.That(item, Is.Not.Null);
    Assert.That(observed, Is.Empty,
      "Providers must not be consulted for bare paths.");
  }

  // ── JsonArrayBuilder: ambient-fallback parity ─────────────────────────────

  [Test]
  public void JsonArrayBuilder_AmbientResolver_UsedWhenNoExplicitResolver()
  {
    var resolver = new StorageMediumResolver(
      new IStorageMediumProvider[] { new FakeMediumProvider("custom") }
    );
    using var _ = StorageMediumResolver.PushAmbient(resolver);

    var item = Item.Of<IEnumerable<JsonExtArrayRow>>("array-ambient")
      .Json()
      .AtPath("custom://endpoint/rows.json")
      .Build();

    Assert.That(item.Label, Is.EqualTo("array-ambient"));
  }

  [Test]
  public void JsonArrayBuilder_AtPathNonFileScheme_NoAmbientNoExplicit_ThrowsWithDiagnostic()
  {
    var anchor = Item.Of<IEnumerable<JsonExtArrayRow>>("array-bad")
      .Json()
      .AtPath("https://example.com/rows.json");

    var ex = Assert.Throws<InvalidOperationException>(() => anchor.Build());
    Assert.That(ex!.Message, Does.Contain("https"));
    Assert.That(ex.Message, Does.Contain("UseHttp"));
  }

  [Test]
  public void JsonArrayBuilder_ExplicitResolver_TakesPrecedenceOverAmbient()
  {
    // Ambient is a fallback — when the user threads .WithResolver(...)
    // explicitly, that value wins, mirroring the per-item override.
    var ambientObserved = new List<Uri>();
    var ambient = new StorageMediumResolver(
      new IStorageMediumProvider[] { new FakeMediumProvider("custom") { Observed = ambientObserved } }
    );
    var explicitObserved = new List<Uri>();
    var explicitResolver = new StorageMediumResolver(
      new IStorageMediumProvider[] { new FakeMediumProvider("custom") { Observed = explicitObserved } }
    );

    using var _ = StorageMediumResolver.PushAmbient(ambient);

    var item = Item.Of<IEnumerable<JsonExtArrayRow>>("array-explicit-wins")
      .Json()
      .AtPath("custom://endpoint/rows.json")
      .WithResolver(explicitResolver)
      .Build();

    Assert.That(item, Is.Not.Null);
    Assert.That(explicitObserved, Is.Not.Empty,
      "Explicit resolver must be the one consulted.");
    Assert.That(ambientObserved, Is.Empty,
      "Ambient must be ignored when an explicit resolver is supplied.");
  }

  // ── PushAmbient scope semantics ───────────────────────────────────────────

  [Test]
  public void PushAmbient_ScopeUnwinds_OnDispose()
  {
    Assert.That(StorageMediumResolver.Current, Is.Null,
      "No ambient resolver should leak across tests.");

    var inner = new StorageMediumResolver(Array.Empty<IStorageMediumProvider>());
    using (StorageMediumResolver.PushAmbient(inner))
    {
      Assert.That(StorageMediumResolver.Current, Is.SameAs(inner),
        "Current should reflect the most-recent push inside the scope.");
    }

    Assert.That(StorageMediumResolver.Current, Is.Null,
      "Disposing the scope must clear the ambient slot.");
  }

  [Test]
  public void PushAmbient_NestedScopes_RestorePreviousOnInnerDispose()
  {
    var outer = new StorageMediumResolver(Array.Empty<IStorageMediumProvider>());
    var inner = new StorageMediumResolver(Array.Empty<IStorageMediumProvider>());

    using (StorageMediumResolver.PushAmbient(outer))
    {
      Assert.That(StorageMediumResolver.Current, Is.SameAs(outer));
      using (StorageMediumResolver.PushAmbient(inner))
      {
        Assert.That(StorageMediumResolver.Current, Is.SameAs(inner));
      }
      Assert.That(StorageMediumResolver.Current, Is.SameAs(outer),
        "Disposing the inner scope must restore the previous ambient resolver.");
    }
    Assert.That(StorageMediumResolver.Current, Is.Null);
  }

  // ── Test fakes ────────────────────────────────────────────────────────────

  private sealed class FakeMediumProvider : IStorageMediumProvider
  {
    private readonly string _scheme;
    public List<Uri>? Observed { get; init; }

    public FakeMediumProvider(string scheme) => _scheme = scheme;

    public bool CanHandle(Uri uri)
    {
      Observed?.Add(uri);
      return uri.Scheme.Equals(_scheme, StringComparison.OrdinalIgnoreCase);
    }

    public IStorageMedium Create(Uri uri) => new FakeMedium(uri);
  }

  private sealed class FakeMedium : IStorageMedium
  {
    public Uri Uri { get; }
    public FakeMedium(Uri uri) => Uri = uri;
    public StorageTraits Traits => new();
    public FlowIO<Stream> ReadStream() =>
      FlowIO.LiftAsync<Stream>(_ => throw new NotImplementedException("Test fake — read path not exercised."));
    public FlowIO<FlowUnit> WriteStream(Stream stream) =>
      FlowIO.LiftAsync<FlowUnit>(_ => throw new NotImplementedException());
    public FlowIO<bool> Exists() => FlowIO.Pure(true);
  }
}
