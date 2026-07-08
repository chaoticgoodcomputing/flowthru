using Flowthru.Data.Storage;
using Flowthru.Prelude;
using Flowthru.Step.DuckDb;
using Flowthru.Step.DuckDb.Internal;
using Flowthru.Validation.Runtime;
using Flowthru.Validation.Runtime.DuckDb;
using SysIO = System.IO;

namespace Flowthru.Extensions.DuckDB.Tests;

/// <summary>
/// Pins the engine's httpfs availability contract, offline. The bundled
/// DuckDB does not statically link <c>httpfs</c>, so an <c>s3://</c>
/// endpoint on a host where the extension isn't provisioned and
/// downloads are disabled must fail with the typed <c>FTDDB4003</c>
/// value — an explicit, documented failure mode, never a silent
/// download and never an untyped engine error. Also pins the engine's
/// defensive scheme check for directly-constructed requests
/// (<c>FTDDB4001</c>; the step normally rejects these earlier with the
/// item's own label).
/// </summary>
[TestFixture]
[Category("DuckDB")]
public class DuckDbHttpfsAvailabilityTests
{
  private string _root = null!;

  [SetUp]
  public void SetUp()
  {
    _root = SysIO.Path.Combine(
      SysIO.Path.GetTempPath(), $"flowthru-duckdb-httpfs-{Guid.NewGuid():N}");
    SysIO.Directory.CreateDirectory(_root);
  }

  [TearDown]
  public void TearDown()
  {
    if (SysIO.Directory.Exists(_root))
    {
      try { SysIO.Directory.Delete(_root, recursive: true); }
      catch { /* best effort */ }
    }
  }

  [Test]
  public async Task S3Endpoint_WithoutHttpfs_AndDownloadsDisabled_FailsWithTypedHttpfsError()
  {
    // An empty extension directory guarantees httpfs is not provisioned,
    // regardless of what the developer machine has under ~/.duckdb; with
    // downloads disabled the engine must not fall back to the network.
    var emptyExtensionDir = SysIO.Path.Combine(_root, "extensions");
    SysIO.Directory.CreateDirectory(emptyExtensionDir);
    var engine = new InProcessDuckDbEngine(new DuckDbEngineOptions
    {
      ExtensionDirectory = emptyExtensionDir,
      AllowExtensionDownload = false,
    });

    var outcome = await engine.ExecuteTransform(S3Request("s3_offline")).Run();

    Assert.That(outcome, Is.InstanceOf<EffResult<DuckDbTransformResult>.Failure>());
    var error = ((EffResult<DuckDbTransformResult>.Failure)outcome).Error;
    Assert.That(error, Is.InstanceOf<RuntimeError.ExtensionError>(),
      $"Expected the typed httpfs-unavailable error, got: {error}");
    var cause = ((RuntimeError.ExtensionError)error).Cause;
    Assert.That(cause, Is.InstanceOf<DuckDbRuntimeError.HttpfsUnavailable>());
    Assert.Multiple(() =>
    {
      Assert.That(cause.DiagnosticCode, Is.EqualTo("FTDDB4003"));
      Assert.That(cause.Message, Does.Contain("AllowExtensionDownload"),
        "The error must name the switch that would change the behaviour.");
      Assert.That(cause.Message, Does.Contain("INSTALL httpfs"),
        "The error must carry the pre-provisioning remedy.");
    });
  }

  [Test]
  public async Task DirectRequest_WithNonS3RemoteEndpoint_FailsWithTypedRemoteBytesError()
  {
    // The step rejects non-s3 schemes with the item's label before the
    // engine is ever invoked; this pins the engine's own defensive check
    // for requests constructed directly against IDuckDbEngine.
    var engine = new InProcessDuckDbEngine();
    var request = new DuckDbTransformRequest(
      StepLabel: "direct",
      Relations: new[]
      {
        new DuckDbBoundRelation(
          "rows",
          new ByteLocation.RemoteUri(
            new Uri("https://example.com/rows.parquet"),
            new Dictionary<string, string>())),
      },
      Sql: "SELECT * FROM rows",
      OutputLocation: new ByteLocation.LocalFile(SysIO.Path.Combine(_root, "out.parquet")),
      ExpectedColumns: Array.Empty<DuckDbExpectedColumn>(),
      Options: DuckDbTransformOptions.Default
    );

    var outcome = await engine.ExecuteTransform(request).Run();

    Assert.That(outcome, Is.InstanceOf<EffResult<DuckDbTransformResult>.Failure>());
    var error = ((EffResult<DuckDbTransformResult>.Failure)outcome).Error;
    Assert.That(error, Is.InstanceOf<RuntimeError.ExtensionError>());
    var cause = ((RuntimeError.ExtensionError)error).Cause;
    Assert.That(cause, Is.InstanceOf<DuckDbRuntimeError.RemoteBytesUnsupported>());
    Assert.That(cause.DiagnosticCode, Is.EqualTo("FTDDB4001"));
  }

  private DuckDbTransformRequest S3Request(string stepLabel) =>
    new(
      StepLabel: stepLabel,
      Relations: new[]
      {
        new DuckDbBoundRelation(
          "rows",
          new ByteLocation.RemoteUri(
            new Uri("s3://bucket/in.parquet"),
            new Dictionary<string, string>
            {
              ["region"] = "us-east-1",
              ["access_key_id"] = "id",
              ["secret_access_key"] = "secret",
            })),
      },
      Sql: "SELECT * FROM rows",
      OutputLocation: new ByteLocation.LocalFile(SysIO.Path.Combine(_root, "out.parquet")),
      ExpectedColumns: Array.Empty<DuckDbExpectedColumn>(),
      Options: DuckDbTransformOptions.Default
    );
}
