using Flowthru.Core.Meta;
using Flowthru.Core.Meta.Providers;
using Flowthru.Meta.Providers;
using Flowthru.Tests.Kits.Metadata;

namespace Flowthru.Extensions.Metadata.Json.Tests.Conformance;

/// <summary>
/// Conformance for <see cref="JsonMetadataProvider"/>.
/// </summary>
[TestFixture]
public class JsonMetadataProviderConformance : MetadataProviderConformance
{
  private string _tempDir = string.Empty;

  [SetUp]
  public void SetUp()
  {
    _tempDir = Path.Combine(
      Path.GetTempPath(),
      $"flowthru-json-metadata-conformance-{Guid.NewGuid():N}"
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
    new JsonMetadataProvider(
      outputDirectory: _tempDir,
      dagFilenameTemplate: "dag",
      runFilenameTemplate: "run",
      timestampConfig: new TimestampConfiguration()
    );
}
