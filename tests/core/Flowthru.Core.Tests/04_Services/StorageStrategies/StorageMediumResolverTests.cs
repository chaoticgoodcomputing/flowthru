using Flowthru.Core.Data.Capabilities;
using Flowthru.Core.Data.Storage;
using Flowthru.Core.Data.Validation;
using Flowthru.Core.Effects;

namespace Flowthru.Core.Tests.Services.StorageStrategies;

/// <summary>
/// Tests for <see cref="StorageMediumResolver"/>'s public registration API. The resolver
/// is the entry point for extension authors to register custom storage medium providers
/// (e.g., a future S3 or Azure Blob provider).
/// </summary>
[TestFixture]
[Category("Services")]
[Category("StorageStrategies")]
public class StorageMediumResolverTests
{
  [Test]
  public void Register_AddsProviderAndReturnsResolver()
  {
    var resolver = new StorageMediumResolver();
    var provider = new StubProvider("custom");

    var result = resolver.Register(provider);

    Assert.That(result, Is.SameAs(resolver), "Register should return resolver for chaining.");
  }

  [Test]
  public void Register_FluentChain_RegistersMultipleProviders()
  {
    var resolver = new StorageMediumResolver()
      .Register(new StubProvider("scheme1"))
      .Register(new StubProvider("scheme2"));

    // Resolver chain is exercised when a matching URI is resolved
    var medium = resolver.Resolve("scheme1://anything");

    Assert.That(medium, Is.InstanceOf<StubMedium>());
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Stubs
  // ─────────────────────────────────────────────────────────────────────────

  private sealed class StubProvider : IStorageMediumProvider
  {
    private readonly string _scheme;

    public StubProvider(string scheme) => _scheme = scheme;

    public bool CanHandle(Uri uri) =>
      string.Equals(uri.Scheme, _scheme, StringComparison.OrdinalIgnoreCase);

    public IStorageMedium Create(Uri uri) => new StubMedium();
  }

  private sealed class StubMedium : IStorageMedium
  {
    public StorageTraits Traits => new StorageTraits();

    public FlowIO<Stream> ReadStream() => FlowIO.Lift<Stream>(() => new MemoryStream());

    public FlowIO<FlowUnit> WriteStream(Stream stream) => FlowIO.Pure(FlowUnit.Default);

    public FlowIO<bool> Exists() => FlowIO.Pure(false);

    public FlowIO<ValidationResult> InspectTarget() => FlowIO.Pure(ValidationResult.Success());
  }
}
