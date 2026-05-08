using Flowthru.Diagnostics;
using Flowthru.Diagnostics.Json;

namespace Flowthru.Extensions.Metadata.Json.Tests;

/// <summary>
/// Smoke tests for <see cref="JsonMetadataBuilderExtensions.AddJsonMetadata"/>
/// — the contributed extension method on <see cref="FlowthruMetadataBuilder"/>
/// that registers a <see cref="JsonMetadataProvider"/> as both pre-run
/// and post-run.
/// </summary>
[TestFixture]
[Category("Metadata.Json")]
public class JsonMetadataBuilderExtensionsTests
{
  [Test]
  public void AddJsonMetadata_DefaultConfig_RegistersProviderForBothPhases()
  {
    var builder = new FlowthruMetadataBuilder();
    builder.AddJsonMetadata();

    Assert.That(builder.PreRunProviders, Has.Count.EqualTo(1));
    Assert.That(builder.PostRunProviders, Has.Count.EqualTo(1));
    Assert.That(builder.PreRunProviders[0], Is.InstanceOf<JsonMetadataProvider>());
    Assert.That(builder.PostRunProviders[0], Is.InstanceOf<JsonMetadataProvider>());

    // The same provider instance fronts both registrations — JsonMetadataProvider
    // implements both interfaces, so we register it once and slot it into each list.
    Assert.That(
      ReferenceEquals(builder.PreRunProviders[0], builder.PostRunProviders[0]),
      Is.True,
      "Pre-run and post-run registrations should reference the same provider instance."
    );
  }

  [Test]
  public void AddJsonMetadata_ConfigureCallback_AppliedBeforeBuild()
  {
    var builder = new FlowthruMetadataBuilder();
    builder.AddJsonMetadata(opt => opt
      .WithOutputDirectory("custom-dir")
      .UseCompactFormat()
    );

    var provider = (JsonMetadataProvider)builder.PreRunProviders[0];
    Assert.That(provider.OutputDirectory, Is.EqualTo("custom-dir"));
  }

  [Test]
  public void AddJsonMetadata_ReturnsBuilder_ForChaining()
  {
    var builder = new FlowthruMetadataBuilder();
    var result = builder.AddJsonMetadata();
    Assert.That(result, Is.SameAs(builder));
  }

  [Test]
  public void AddJsonMetadata_NullBuilder_Throws()
  {
    Assert.That(
      () => JsonMetadataBuilderExtensions.AddJsonMetadata(null!),
      Throws.ArgumentNullException
    );
  }
}
