using Flowthru.Core.Meta;
using Flowthru.Core.Meta.Providers;
using Flowthru.Meta.Providers;
using Flowthru.Tests.Kits.Metadata;

namespace Flowthru.Extensions.Metadata.Mermaid.Tests.Conformance;

/// <summary>
/// Conformance for <see cref="MermaidMetadataProvider"/>.
/// </summary>
[TestFixture]
public class MermaidMetadataProviderConformance : MetadataProviderConformance
{
  private string _tempDir = string.Empty;

  [SetUp]
  public void SetUp()
  {
    _tempDir = Path.Combine(
      Path.GetTempPath(),
      $"flowthru-mermaid-metadata-conformance-{Guid.NewGuid():N}"
    );
    Directory.CreateDirectory(_tempDir);
  }

  [TearDown]
  public void TearDown()
  {
    if (Directory.Exists(_tempDir))
    {
      Directory.Delete(_tempDir, recursive: true);
    }
  }

  protected override IMetadataProvider CreateProvider() =>
    new MermaidMetadataProvider(
      outputDirectory: _tempDir,
      dagFilenameTemplate: "dag",
      runFilenameTemplate: "run",
      timestampConfig: new TimestampConfiguration()
    );
}
