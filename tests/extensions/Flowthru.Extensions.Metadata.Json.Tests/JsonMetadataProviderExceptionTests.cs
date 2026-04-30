using System.Text.Json;
using Flowthru.Core.Flows;
using Flowthru.Core.Graph.Meta.Models;
using Flowthru.Core.Meta;
using Flowthru.Meta.Providers;

namespace Flowthru.Extensions.Metadata.Json.Tests;

/// <summary>
/// Targeted tests for the <c>ExceptionJsonConverter</c> nested in
/// <see cref="JsonMetadataProvider"/>. The converter is private; the only way to drive
/// it is through the post-run consume path with a <see cref="FlowResult"/> that carries
/// an exception.
/// </summary>
[TestFixture]
public class JsonMetadataProviderExceptionTests
{
  private string _tempDir = string.Empty;

  [SetUp]
  public void SetUp()
  {
    _tempDir = Path.Combine(
      Path.GetTempPath(),
      $"flowthru-json-exception-converter-{Guid.NewGuid():N}"
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

  [Test]
  public void ExceptionConverter_WritesTypeAndMessage()
  {
    var provider = new JsonMetadataProvider(
      outputDirectory: _tempDir,
      dagFilenameTemplate: "dag",
      runFilenameTemplate: "run",
      timestampConfig: new TimestampConfiguration()
    );

    var inner = new ArgumentNullException("seed");
    var outer = new InvalidOperationException("Step failed", inner);
    var run = new RunMetadata
    {
      Dag = new DagMetadata { FlowName = "ExceptionTestFlow" },
      Result = FlowResult.CreateFailure(
        executionTime: TimeSpan.FromSeconds(1),
        exception: outer,
        flowName: "ExceptionTestFlow"
      ),
    };

    provider.Consume(run);

    var runFile = Directory
      .GetFiles(_tempDir, "run*.json")
      .Single();
    var json = File.ReadAllText(runFile);

    using var doc = JsonDocument.Parse(json);
    var root = doc.RootElement;
    Assert.That(root.TryGetProperty("result", out var result), Is.True);
    Assert.That(result.TryGetProperty("exception", out var exception), Is.True);
    Assert.That(exception.GetProperty("type").GetString(), Is.EqualTo(nameof(InvalidOperationException)));
    Assert.That(exception.GetProperty("message").GetString(), Does.Contain("Step failed"));
    Assert.That(
      exception.TryGetProperty("innerException", out var innerJson),
      Is.True,
      "InnerException should serialize recursively."
    );
    Assert.That(innerJson.GetProperty("type").GetString(), Is.EqualTo(nameof(ArgumentNullException)));
  }

  [Test]
  public void ExceptionConverter_NoInnerException_OmitsInnerExceptionField()
  {
    var provider = new JsonMetadataProvider(
      outputDirectory: _tempDir,
      dagFilenameTemplate: "dag",
      runFilenameTemplate: "run",
      timestampConfig: new TimestampConfiguration()
    );

    var ex = new InvalidOperationException("Standalone");
    var run = new RunMetadata
    {
      Dag = new DagMetadata { FlowName = "ExceptionTestFlow" },
      Result = FlowResult.CreateFailure(
        executionTime: TimeSpan.FromSeconds(1),
        exception: ex,
        flowName: "ExceptionTestFlow"
      ),
    };

    provider.Consume(run);

    var runFile = Directory.GetFiles(_tempDir, "run*.json").Single();
    var json = File.ReadAllText(runFile);

    using var doc = JsonDocument.Parse(json);
    var exception = doc.RootElement.GetProperty("result").GetProperty("exception");

    Assert.That(exception.TryGetProperty("innerException", out _), Is.False);
  }
}
