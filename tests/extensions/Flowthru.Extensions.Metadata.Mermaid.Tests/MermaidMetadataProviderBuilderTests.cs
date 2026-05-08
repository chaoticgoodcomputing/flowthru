using Flowthru.Diagnostics.Mermaid;
using Microsoft.Extensions.Logging.Abstractions;

namespace Flowthru.Extensions.Metadata.Mermaid.Tests;

/// <summary>
/// Fluent-builder tests — return-type chaining + null-argument
/// guards on <see cref="MermaidMetadataProviderBuilder"/>.
/// </summary>
[TestFixture]
[Category("Metadata.Mermaid")]
public class MermaidMetadataProviderBuilderTests
{
  [Test]
  public void WithOutputDirectory_ReturnsBuilder()
  {
    var builder = new MermaidMetadataProviderBuilder();
    Assert.That(builder.WithOutputDirectory("metadata"), Is.SameAs(builder));
  }

  [Test]
  public void WithFilenameTemplate_ReturnsBuilder()
  {
    var builder = new MermaidMetadataProviderBuilder();
    Assert.That(builder.WithFilenameTemplate("dag-{FlowName}"), Is.SameAs(builder));
  }

  [Test]
  public void WithRunFilenameTemplate_ReturnsBuilder()
  {
    var builder = new MermaidMetadataProviderBuilder();
    Assert.That(builder.WithRunFilenameTemplate("run-{FlowName}"), Is.SameAs(builder));
  }

  [Test]
  public void WithTimestamp_NullFormat_UsesDefault()
  {
    var builder = new MermaidMetadataProviderBuilder();
    Assert.That(builder.WithTimestamp(null), Is.SameAs(builder));
  }

  [Test]
  public void WithTimestamp_CustomFormat_AppliesFormat()
  {
    var builder = new MermaidMetadataProviderBuilder();
    Assert.That(builder.WithTimestamp("yyyy-MM-dd"), Is.SameAs(builder));
  }

  [Test]
  public void WithDirection_LeftToRight_ReturnsBuilder()
  {
    var builder = new MermaidMetadataProviderBuilder();
    Assert.That(
      builder.WithDirection(MermaidFlowchartDirection.LeftToRight),
      Is.SameAs(builder)
    );
  }

  [Test]
  public void WithActiveStepColor_AppliesColor()
  {
    var builder = new MermaidMetadataProviderBuilder();
    Assert.That(builder.WithActiveStepColor("#FF0000"), Is.SameAs(builder));
  }

  [Test]
  public void WithActiveDataColor_AppliesColor()
  {
    var builder = new MermaidMetadataProviderBuilder();
    Assert.That(builder.WithActiveDataColor("#0000FF"), Is.SameAs(builder));
  }

  [Test]
  public void WithFailedStepColor_AppliesColor()
  {
    var builder = new MermaidMetadataProviderBuilder();
    Assert.That(builder.WithFailedStepColor("#FF1111"), Is.SameAs(builder));
  }

  [Test]
  public void WithSkippedStepColor_AppliesColor()
  {
    var builder = new MermaidMetadataProviderBuilder();
    Assert.That(builder.WithSkippedStepColor("#999999"), Is.SameAs(builder));
  }

  [Test]
  public void WithShowFullDag_ReturnsBuilder()
  {
    var builder = new MermaidMetadataProviderBuilder();
    Assert.That(builder.WithShowFullDag(false), Is.SameAs(builder));
  }

  [Test]
  public void WithLogger_AppliesLogger()
  {
    var builder = new MermaidMetadataProviderBuilder();
    Assert.That(builder.WithLogger(NullLogger.Instance), Is.SameAs(builder));
  }

  [Test]
  public void FullChain_BuildsProviderWithoutThrowing()
  {
    var provider = new MermaidMetadataProviderBuilder()
      .WithOutputDirectory("metadata")
      .WithFilenameTemplate("dag-{FlowName}")
      .WithRunFilenameTemplate("run-{FlowName}")
      .WithTimestamp("yyyyMMdd")
      .WithDirection(MermaidFlowchartDirection.LeftToRight)
      .WithActiveStepColor("#2E7D32")
      .WithActiveDataColor("#2E7D32")
      .WithFailedStepColor("#C62828")
      .WithSkippedStepColor("#757575")
      .WithShowFullDag(true)
      .WithLogger(NullLogger.Instance)
      .Build();

    Assert.That(provider, Is.Not.Null);
    Assert.That(provider, Is.InstanceOf<MermaidMetadataProvider>());
  }

  [Test]
  public void NullArguments_ThrowArgumentNullException()
  {
    var builder = new MermaidMetadataProviderBuilder();
    Assert.That(() => builder.WithOutputDirectory(null!), Throws.ArgumentNullException);
    Assert.That(() => builder.WithFilenameTemplate(null!), Throws.ArgumentNullException);
    Assert.That(() => builder.WithRunFilenameTemplate(null!), Throws.ArgumentNullException);
    Assert.That(() => builder.WithActiveStepColor(null!), Throws.ArgumentNullException);
    Assert.That(() => builder.WithActiveDataColor(null!), Throws.ArgumentNullException);
    Assert.That(() => builder.WithFailedStepColor(null!), Throws.ArgumentNullException);
    Assert.That(() => builder.WithSkippedStepColor(null!), Throws.ArgumentNullException);
    Assert.That(() => builder.WithLogger(null!), Throws.ArgumentNullException);
  }

  [Test]
  public void Build_MalformedTimestampFormat_ThrowsArgumentException()
  {
    var builder = new MermaidMetadataProviderBuilder().WithTimestamp("\"");
    Assert.That(() => builder.Build(), Throws.ArgumentException);
  }
}
