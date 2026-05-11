using Flowthru.Diagnostics.Json;
using Microsoft.Extensions.Logging.Abstractions;

namespace Flowthru.Extensions.Metadata.Json.Tests;

/// <summary>
/// Fluent-builder tests — return-type chaining + null-argument
/// guards on <see cref="JsonMetadataProviderBuilder"/>.
/// </summary>
[TestFixture]
[Category("Metadata.Json")]
public class JsonMetadataProviderBuilderTests
{
  [Test]
  public void WithOutputDirectory_ReturnsBuilder()
  {
    var builder = new JsonMetadataProviderBuilder();
    Assert.That(builder.WithOutputDirectory("metadata"), Is.SameAs(builder));
  }

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
    Assert.That(() => builder.WithOutputDirectory(null!), Throws.ArgumentNullException);
    Assert.That(() => builder.WithFilenameTemplate(null!), Throws.ArgumentNullException);
    Assert.That(() => builder.WithRunFilenameTemplate(null!), Throws.ArgumentNullException);
    Assert.That(() => builder.WithLogger(null!), Throws.ArgumentNullException);
  }

  [Test]
  public void Build_MalformedTimestampFormat_ThrowsArgumentException()
  {
    // A single unescaped quote is a documented FormatException trigger
    // for DateTime.ToString — the validation should surface it.
    var builder = new JsonMetadataProviderBuilder().WithTimestamp("\"");
    Assert.That(() => builder.Build(), Throws.ArgumentException);
  }
}
