using Flowthru.Diagnostics;
using Flowthru.Diagnostics.Mermaid;

namespace Flowthru.Extensions.Metadata.Mermaid.Tests;

/// <summary>
/// Smoke tests for <see cref="MermaidMetadataBuilderExtensions.AddMermaidMetadata"/>
/// — the contributed extension method on
/// <see cref="FlowthruMetadataBuilder"/> that registers a
/// <see cref="MermaidMetadataProvider"/> as both pre-run and post-run.
/// </summary>
[TestFixture]
[Category("Metadata.Mermaid")]
public class MermaidMetadataBuilderExtensionsTests
{
  [Test]
  public void AddMermaidMetadata_DefaultConfig_RegistersProviderForBothPhases()
  {
    var builder = new FlowthruMetadataBuilder();
    builder.AddMermaidMetadata();

    Assert.That(builder.PreRunProviders, Has.Count.EqualTo(1));
    Assert.That(builder.PostRunProviders, Has.Count.EqualTo(1));
    Assert.That(builder.PreRunProviders[0], Is.InstanceOf<MermaidMetadataProvider>());
    Assert.That(builder.PostRunProviders[0], Is.InstanceOf<MermaidMetadataProvider>());

    Assert.That(
      ReferenceEquals(builder.PreRunProviders[0], builder.PostRunProviders[0]),
      Is.True,
      "Pre-run and post-run registrations should reference the same provider instance."
    );
  }

  [Test]
  public void AddMermaidMetadata_ConfigureCallback_AppliedBeforeBuild()
  {
    var builder = new FlowthruMetadataBuilder();
    builder.AddMermaidMetadata(opt => opt
      .WithOutputDirectory("custom-dir")
      .WithDirection(MermaidFlowchartDirection.RightToLeft)
    );

    var provider = (MermaidMetadataProvider)builder.PreRunProviders[0];
    Assert.That(provider.OutputDirectory, Is.EqualTo("custom-dir"));
  }

  [Test]
  public void AddMermaidMetadata_ReturnsBuilder_ForChaining()
  {
    var builder = new FlowthruMetadataBuilder();
    var result = builder.AddMermaidMetadata();
    Assert.That(result, Is.SameAs(builder));
  }

  [Test]
  public void AddMermaidMetadata_NullBuilder_Throws()
  {
    Assert.That(
      () => MermaidMetadataBuilderExtensions.AddMermaidMetadata(null!),
      Throws.ArgumentNullException
    );
  }
}
