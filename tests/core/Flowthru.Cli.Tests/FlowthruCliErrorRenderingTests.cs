using Flowthru.Data.Catalog;
using Flowthru.Diagnostics;
using Flowthru.Flow;
using Flowthru.Hosting;
using Flowthru.Prelude;
using Flowthru.Validation.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Cli.Tests;

/// <summary>
/// Pins the CLI's failure-rendering contract: when a step fails,
/// <see cref="FlowthruCli.RunStandaloneAsync"/> must render a diagnostic-
/// code-tagged line on stderr and exit with code 1; the
/// <see cref="RuntimeErrorClassifier"/> + <see cref="ConsoleErrorFormatter"/>
/// pipeline must surface the "bug in Flowthru" affordance only for
/// <see cref="RuntimeError.InvariantViolated"/>, not for external failures
/// or caller cancellation.
/// </summary>
/// <remarks>
/// <para>
/// Ported (adapted) from quarantined <c>05_Cli/FlowthruCliTests</c> and
/// <c>06_Results/{ConsoleResultFormatter,RuntimeErrorReporting}Tests</c>. The
/// pre-FP-rewrite suite assumed a <c>ConsoleResultFormatter</c> +
/// <c>GitHubIssueUrlBuilder</c> architecture keyed off an
/// <c>ErrorClassification</c> enum; the FP rewrite replaced that with the
/// <see cref="RuntimeError"/> closed sum, so the cases here exercise the
/// new contract via the classifier+formatter and the CLI surface.
/// </para>
/// </remarks>
[TestFixture]
public class FlowthruCliErrorRenderingTests
{
  public sealed class TestCatalog : CatalogAbstract
  {
    public IItem<int> Input => CreateItem(() => ItemFactory.Singleton.Memory<int>("input"));
    public IItem<int> Output => CreateItem(() => ItemFactory.Singleton.Memory<int>("output"));
  }

  [Test]
  public async Task FailingStep_RendersDiagnosticCodeOnStderrAndExitsOne()
  {
    var consoleOut = new StringWriter();
    var consoleErr = new StringWriter();
    Console.SetOut(consoleOut);
    Console.SetError(consoleErr);

    var exit = await FlowthruCli.RunStandaloneAsync(
      Array.Empty<string>(),
      services => services.AddFlowthru(b =>
      {
        b.RegisterCatalog(_ => new TestCatalog());
        b.RegisterFlow<TestCatalog>("boom", catalog =>
        {
          catalog.Input.Save(7).Run().GetAwaiter().GetResult();
          Func<int, int> explode = _ => throw new InvalidOperationException("user step blew up");
          return FlowBuilder.CreateFlow("boom", p =>
            p.AddStep<int, int>("explode", explode, catalog.Input, catalog.Output)
          );
        });
      })
    );

    Assert.That(exit, Is.EqualTo(1),
      "A failed step should make the CLI exit 1.");

    var stderr = consoleErr.ToString();
    Assert.That(stderr, Does.Contain("✗ explode"),
      "Stderr should render the failed step's label with the ✗ marker.");
    Assert.That(stderr, Does.Match(@"FT4\d{3}"),
      "Stderr should contain an FT4xxx diagnostic code from the classifier.");
  }

  [Test]
  public void ExternalError_RendersFT4001ExternalCategoryWithoutFrameworkBugAffordance()
  {
    // External failures (IO, network, etc.) are NOT Flowthru bugs — the
    // renderer must surface them with FT4001 and the "External" category,
    // and NOT include the "bug in Flowthru" affordance. This mirrors the
    // quarantined ConsoleResultFormatter "ExternalErrorFailure" cases:
    // an external factor is the cause, the user (or operator) acts on it
    // outside the flow.
    var error = new RuntimeError.External("disk", new IOException("disk read error"));

    var report = RuntimeErrorClassifier.Classify(error);
    var rendered = ConsoleErrorFormatter.Format(report);

    Assert.That(rendered, Does.Contain(FlowthruDiagnosticCodes.RuntimeExternalFailure),
      "External failures should surface as FT4001.");
    Assert.That(rendered, Does.Contain("External"),
      "External failures should render with the 'External' category label.");
    Assert.That(rendered, Does.Not.Contain("bug in Flowthru"),
      "External failures are NOT Flowthru bugs — the renderer must not "
      + "emit the 'file an issue' affordance.");
  }

  [Test]
  public void InvariantViolated_RendersFileAnIssueAffordance()
  {
    // The CLI's failure-rendering pipeline must, for a RuntimeError.InvariantViolated,
    // emit the "this is a bug in Flowthru" affordance. We exercise this by hand-
    // constructing a Failed StepResult with an InvariantViolated and rendering it
    // through the same Classifier+Formatter the CLI uses.
    var error = new RuntimeError.InvariantViolated(
      "preflight.missing-check",
      "expected pre-flight invariant did not fire"
    );

    var report = RuntimeErrorClassifier.Classify(error);
    var rendered = ConsoleErrorFormatter.Format(report);

    Assert.That(rendered, Does.Contain(FlowthruDiagnosticCodes.RuntimeInvariantViolated),
      "InvariantViolated should render with the FT4004 code.");
    Assert.That(rendered, Does.Contain("bug in Flowthru"),
      "InvariantViolated must render the 'file an issue' affordance — that's the "
      + "whole point of distinguishing it from user-actionable errors.");
  }

  [Test]
  public async Task Cancellation_PropagatesAsOperationCanceledException_NoFrameworkBugFraming()
  {
    // Mirrors the quarantined RuntimeErrorReporting cancellation case: caller-
    // initiated cancellation propagates as OperationCanceledException and must
    // NOT render as a framework bug. This is the regression guard for the
    // service-level safety net: a user pressing Ctrl-C (or a host applying its
    // own deadline) is a user-requested abort, not a Flowthru bug.
    var consoleOut = new StringWriter();
    var consoleErr = new StringWriter();
    Console.SetOut(consoleOut);
    Console.SetError(consoleErr);

    using var cts = new CancellationTokenSource();
    cts.Cancel();

    Exception? thrown = null;
    try
    {
      await FlowthruCli.RunStandaloneAsync(
        Array.Empty<string>(),
        services => services.AddFlowthru(b =>
        {
          b.RegisterCatalog(_ => new TestCatalog());
          b.RegisterFlow<TestCatalog>("cancel", catalog =>
          {
            catalog.Input.Save(7).Run().GetAwaiter().GetResult();
            return FlowBuilder.CreateFlow("cancel", p =>
              p.AddStep<int, int>("noop", x => x, catalog.Input, catalog.Output)
            );
          });
        }),
        cancellationToken: cts.Token
      );
    }
    catch (Exception ex)
    {
      thrown = ex;
    }

    Assert.That(thrown, Is.InstanceOf<OperationCanceledException>(),
      "Caller-initiated cancellation should propagate as OperationCanceledException, "
      + "not be wrapped into a framework-bug error.");
    Assert.That(consoleErr.ToString(), Does.Not.Contain("bug in Flowthru"),
      "Caller-initiated cancellation is NOT a Flowthru bug — the renderer must "
      + "not emit the 'file an issue' affordance.");
  }
}
