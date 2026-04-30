using Flowthru.Meta;
using Flowthru.Meta.Providers;
using Microsoft.Extensions.Logging.Abstractions;

namespace Flowthru.Extensions.Metadata.Mermaid.Tests;

/// <summary>
/// Tests for <see cref="MermaidMetadataProviderBuilder"/> fluent setters.
/// </summary>
/// <remarks>
/// Each setter is a 4–5 line method that assigns a private field and returns <c>this</c>.
/// One test per setter exercises the line. The full-chain test verifies that all setters
/// compose without throwing through a <see cref="MermaidMetadataProviderBuilder.Build"/>.
/// </remarks>
[TestFixture]
public class MermaidMetadataProviderBuilderTests
{
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
      builder.WithDirection(MermaidMetadataProvider.MermaidFlowchartDirection.LeftToRight),
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
    Assert.That(builder.WithActiveDataColor("#00FF00"), Is.SameAs(builder));
  }

  [Test]
  public void WithFailedStepColor_AppliesColor()
  {
    var builder = new MermaidMetadataProviderBuilder();
    Assert.That(builder.WithFailedStepColor("#0000FF"), Is.SameAs(builder));
  }

  [Test]
  public void WithNotRunStepColor_AppliesColor()
  {
    var builder = new MermaidMetadataProviderBuilder();
    Assert.That(builder.WithNotRunStepColor("#888888"), Is.SameAs(builder));
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
      .WithDirection(MermaidMetadataProvider.MermaidFlowchartDirection.LeftToRight)
      .WithActiveStepColor("#FF0000")
      .WithActiveDataColor("#00FF00")
      .WithFailedStepColor("#0000FF")
      .WithNotRunStepColor("#888888")
      .WithShowFullDag(false)
      .WithLogger(NullLogger.Instance)
      .Build();

    Assert.That(provider, Is.Not.Null);
    Assert.That(provider, Is.InstanceOf<MermaidMetadataProvider>());
  }

  [Test]
  public void NullArguments_ThrowArgumentNullException()
  {
    var builder = new MermaidMetadataProviderBuilder();
    Assert.That(() => builder.WithFilenameTemplate(null!), Throws.ArgumentNullException);
    Assert.That(() => builder.WithRunFilenameTemplate(null!), Throws.ArgumentNullException);
    Assert.That(() => builder.WithActiveStepColor(null!), Throws.ArgumentNullException);
    Assert.That(() => builder.WithActiveDataColor(null!), Throws.ArgumentNullException);
    Assert.That(() => builder.WithFailedStepColor(null!), Throws.ArgumentNullException);
    Assert.That(() => builder.WithNotRunStepColor(null!), Throws.ArgumentNullException);
  }
}
