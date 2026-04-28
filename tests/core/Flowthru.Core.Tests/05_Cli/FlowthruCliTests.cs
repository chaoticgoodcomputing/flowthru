using Flowthru.Core.Cli;
using Flowthru.Core.Data;
using Flowthru.Core.Data.Validation;
using Flowthru.Core.Flows;
using Flowthru.Core.Graph;
using Flowthru.Core.Graph.Meta.Models;
using Flowthru.Core.Services;
using Flowthru.Core.Services.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace Flowthru.Core.Tests.Cli;

/// <summary>
/// Unit tests for <see cref="FlowthruCli"/> covering its public surface (constructor,
/// <c>RunAsync</c>) and exercising the private helpers (<c>ShowHelp</c>, <c>ShowUsage</c>,
/// <c>ShowVersion</c>, <c>FormatResult</c>) through them.
/// </summary>
[TestFixture]
[Category("Cli")]
public class FlowthruCliTests
{
  private StubFlowthruService _service = null!;
  private StringWriter _output = null!;
  private FlowthruCli _cli = null!;

  [SetUp]
  public void SetUp()
  {
    _service = new StubFlowthruService();
    _output = new StringWriter();
    _cli = new FlowthruCli(_service, NullLogger<FlowthruCli>.Instance, _output);
  }

  [TearDown]
  public void TearDown()
  {
    _output?.Dispose();
  }

  // ─────────────────────────────────────────────────────────────────────────
  // ShowHelp / ShowVersion / ShowUsage (exercised via RunAsync)
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task RunAsync_HelpFlag_PrintsHelpAndReturnsZero()
  {
    var exitCode = await _cli.RunAsync(new[] { "--help" });

    Assert.That(exitCode, Is.EqualTo(0));
    var text = _output.ToString();
    Assert.That(text, Contains.Substring("Flowthru"));
    Assert.That(text, Contains.Substring("Usage: flowthru"));
    Assert.That(text, Contains.Substring("Available Flows:"));
  }

  [Test]
  public async Task RunAsync_VersionFlag_PrintsVersionAndReturnsZero()
  {
    var exitCode = await _cli.RunAsync(new[] { "--version" });

    Assert.That(exitCode, Is.EqualTo(0));
    Assert.That(_output.ToString(), Does.StartWith("Flowthru v"));
  }

  [Test]
  public async Task RunAsync_UnknownFlag_FailsWithFatalErrorAndReturnsOne()
  {
    // The argument parser throws on unknown flags, which the CLI catches in its
    // unhandled-exception handler — so exit code is 1 and the message is "Fatal error: ..."
    var exitCode = await _cli.RunAsync(new[] { "--bogus-flag-that-does-not-exist" });

    Assert.That(exitCode, Is.EqualTo(1));
    Assert.That(_output.ToString(), Contains.Substring("Fatal error:"));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // FormatResult (exercised via RunAsync's success / failure paths)
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task RunAsync_SuccessfulExecution_FormatsResultAndReturnsZero()
  {
    _service.NextResult = FlowResult.CreateSuccess(
      executionTime: TimeSpan.FromMilliseconds(123),
      stepResults: new Dictionary<string, StepResult>
      {
        ["StepA"] = StepResult.CreateSuccess("StepA", TimeSpan.FromMilliseconds(50), 100, 200),
      },
      flowName: "TestFlow"
    );

    var exitCode = await _cli.RunAsync(Array.Empty<string>());

    Assert.That(exitCode, Is.EqualTo(0));
    var text = _output.ToString();
    Assert.That(text, Contains.Substring("Pipeline: TestFlow"));
    Assert.That(text, Contains.Substring("✓ SUCCESS"));
    Assert.That(text, Contains.Substring("Nodes: 1 executed"));
  }

  [Test]
  public async Task RunAsync_FailedExecution_PrintsFailedNodesAndReturnsOne()
  {
    var failure = StepResult.CreateFailure(
      "StepFailed",
      TimeSpan.FromMilliseconds(10),
      new InvalidOperationException("boom")
    );
    _service.NextResult = FlowResult.CreateFailure(
      TimeSpan.FromMilliseconds(50),
      new InvalidOperationException("boom"),
      stepResults: new Dictionary<string, StepResult> { ["StepFailed"] = failure },
      flowName: "TestFlow"
    );

    var exitCode = await _cli.RunAsync(Array.Empty<string>());

    Assert.That(exitCode, Is.EqualTo(1));
    var text = _output.ToString();
    Assert.That(text, Contains.Substring("✗ FAILED"));
    Assert.That(text, Contains.Substring("Failed Nodes:"));
    Assert.That(text, Contains.Substring("StepFailed"));
    Assert.That(text, Contains.Substring("boom"));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Cancellation
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task RunAsync_Cancelled_ReturnsExitCode130()
  {
    _service.ThrowOnExecute = new OperationCanceledException();

    var exitCode = await _cli.RunAsync(Array.Empty<string>(), CancellationToken.None);

    Assert.That(exitCode, Is.EqualTo(130));
  }

  [Test]
  public async Task RunAsync_UnhandledException_PrintsFatalAndReturnsOne()
  {
    _service.ThrowOnExecute = new InvalidOperationException("system blew up");

    var exitCode = await _cli.RunAsync(Array.Empty<string>());

    Assert.That(exitCode, Is.EqualTo(1));
    Assert.That(_output.ToString(), Contains.Substring("Fatal error: system blew up"));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Stub
  // ─────────────────────────────────────────────────────────────────────────

  private sealed class StubFlowthruService : IFlowthruService
  {
    public IReadOnlyCollection<string> FlowNames { get; set; } = new[] { "TestFlow" };
    public IReadOnlyList<CatalogAbstract> Catalogs { get; } = Array.Empty<CatalogAbstract>();

    public FlowResult? NextResult { get; set; }
    public Exception? ThrowOnExecute { get; set; }

    public Task<FlowResult> ExecuteFlowAsync(
      ExecutionOptions? options = null,
      bool exportMetadata = true,
      CancellationToken cancellationToken = default
    )
    {
      if (ThrowOnExecute is not null)
      {
        throw ThrowOnExecute;
      }

      return Task.FromResult(
        NextResult
          ?? FlowResult.CreateSuccess(
            TimeSpan.Zero,
            new Dictionary<string, StepResult>(),
            "TestFlow"
          )
      );
    }

    public FlowMetadata GetFlowMetadata(string flowName) =>
      new()
      {
        Name = flowName,
        Description = "Stub flow for CLI tests",
        StepCount = 1,
        LayerCount = 1,
        ExternalInputs = Array.Empty<string>(),
        IsBuilt = true,
      };

    public DagMetadata GetDagMetadata(
      string? flowName = null,
      FlowSliceStrategy? sliceStrategy = null
    ) => throw new NotImplementedException("Not used by tested CLI paths.");

    public Task<ValidationResult> ValidateFlowAsync(
      string flowName,
      CancellationToken cancellationToken = default
    ) => throw new NotImplementedException("Not used by tested CLI paths.");
  }
}
