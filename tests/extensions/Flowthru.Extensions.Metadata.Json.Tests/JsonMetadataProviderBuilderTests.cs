using Flowthru.Meta;
using Flowthru.Meta.Providers;
using Microsoft.Extensions.Logging.Abstractions;

namespace Flowthru.Extensions.Metadata.Json.Tests;

/// <summary>
/// Tests for <see cref="JsonMetadataProviderBuilder"/> fluent setters.
/// </summary>
[TestFixture]
public class JsonMetadataProviderBuilderTests
{
  [Test]
  public void WithFilenameTemplate_ReturnsBuilder()
  {
    var builder = new JsonMetadataProviderBuilder();
    Assert.That(builder.WithFilenameTemplate("dag-{FlowName}"), Is.SameAs(builder));
  }

  [Test]
  public void WithRunFilenameTemplate_ReturnsBuilder()
  {
    var builder = new JsonMetadataProviderBuilder();
    Assert.That(builder.WithRunFilenameTemplate("run-{FlowName}"), Is.SameAs(builder));
  }

  [Test]
  public void WithTimestamp_NullFormat_UsesDefault()
  {
    var builder = new JsonMetadataProviderBuilder();
    Assert.That(builder.WithTimestamp(null), Is.SameAs(builder));
  }

  [Test]
  public void WithTimestamp_CustomFormat_AppliesFormat()
  {
    var builder = new JsonMetadataProviderBuilder();
    Assert.That(builder.WithTimestamp("yyyy-MM-dd"), Is.SameAs(builder));
  }

  [Test]
  public void UseCompactFormat_ReturnsBuilder()
  {
    var builder = new JsonMetadataProviderBuilder();
    Assert.That(builder.UseCompactFormat(), Is.SameAs(builder));
  }

  [Test]
  public void UseIndentedFormat_ReturnsBuilder()
  {
    var builder = new JsonMetadataProviderBuilder();
    Assert.That(builder.UseIndentedFormat(), Is.SameAs(builder));
  }

  [Test]
  public void WithLogger_AppliesLogger()
  {
    var builder = new JsonMetadataProviderBuilder();
    Assert.That(builder.WithLogger(NullLogger.Instance), Is.SameAs(builder));
  }

  [Test]
  public void FullChain_BuildsProviderWithoutThrowing()
  {
    var provider = new JsonMetadataProviderBuilder()
      .WithOutputDirectory("metadata")
      .WithFilenameTemplate("dag-{FlowName}")
      .WithRunFilenameTemplate("run-{FlowName}")
      .WithTimestamp("yyyyMMdd")
      .UseCompactFormat()
      .UseIndentedFormat() // toggle back; later wins
      .WithLogger(NullLogger.Instance)
      .Build();

    Assert.That(provider, Is.Not.Null);
    Assert.That(provider, Is.InstanceOf<JsonMetadataProvider>());
  }

  [Test]
  public void NullArguments_ThrowArgumentNullException()
  {
    var builder = new JsonMetadataProviderBuilder();
    Assert.That(() => builder.WithFilenameTemplate(null!), Throws.ArgumentNullException);
    Assert.That(() => builder.WithRunFilenameTemplate(null!), Throws.ArgumentNullException);
  }
}
