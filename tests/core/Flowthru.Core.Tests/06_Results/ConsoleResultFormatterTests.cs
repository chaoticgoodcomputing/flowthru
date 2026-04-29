using Flowthru.Core.Flows;
using Flowthru.Core.Results;
using Flowthru.Core.Tests.Fixtures;
using Microsoft.Extensions.Logging;

namespace Flowthru.Core.Tests.Results;

/// <summary>
/// Tests for <see cref="ConsoleResultFormatter"/> focusing on the error-reporting
/// section added to failure output (issue URL and classification messaging).
/// </summary>
[TestFixture]
[Category("Results")]
[Category("ConsoleResultFormatter")]
public class ConsoleResultFormatterTests
{
  private ConsoleResultFormatter _formatter = null!;
  private RecordingLogger _logger = null!;

  [SetUp]
  public void SetUp()
  {
    _formatter = new ConsoleResultFormatter();
    _logger = new RecordingLogger();
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Success — no issue URL should appear
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void Format_SuccessResult_DoesNotLogIssueUrl()
  {
    var result = FlowResult.CreateSuccess(
      TimeSpan.FromSeconds(1),
      new Dictionary<string, StepResult>(),
      "MyFlow"
    );

    _formatter.Format(result, _logger);

    Assert.That(
      _logger.Messages,
      Has.None.Contains("github.com/chaoticgoodcomputing/flowthru/issues/new")
    );
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Failure — issue URL must always appear
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void Format_FailureResult_LogsGitHubIssueUrl()
  {
    var result = FlowResult.CreateFailure(
      TimeSpan.FromSeconds(2),
      new InvalidOperationException("unexpected state"),
      flowName: "MyFlow"
    );

    _formatter.Format(result, _logger);

    Assert.That(
      _logger.Messages,
      Has.Some.Contains("github.com/chaoticgoodcomputing/flowthru/issues/new")
    );
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Classification messaging — PossibleFrameworkBug
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void Format_PossibleFrameworkBugFailure_MentionsFrameworkBug()
  {
    // InvalidOperationException is not an external exception type.
    var result = FlowResult.CreateFailure(
      TimeSpan.FromSeconds(1),
      new InvalidOperationException("internal failure"),
      flowName: "MyFlow"
    );

    _formatter.Format(result, _logger);

    var allMessages = string.Join(" ", _logger.Messages);
    Assert.That(allMessages, Does.Contain("Flowthru").IgnoreCase);
  }

  [Test]
  public void Format_PossibleFrameworkBugFailure_LoggedAtErrorLevel()
  {
    var result = FlowResult.CreateFailure(
      TimeSpan.FromSeconds(1),
      new InvalidOperationException("internal failure"),
      flowName: "MyFlow"
    );

    _formatter.Format(result, _logger);

    var issueEntry = _logger.Entries.FirstOrDefault(e =>
      e.Message.Contains("github.com/chaoticgoodcomputing/flowthru/issues/new")
    );
    Assert.That(issueEntry, Is.Not.Null);
    Assert.That(issueEntry!.Level, Is.EqualTo(LogLevel.Error));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Classification messaging — ExternalError
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void Format_ExternalErrorFailure_MentionsExternalFactor()
  {
    var result = FlowResult.CreateFailure(
      TimeSpan.FromSeconds(1),
      new IOException("disk read error"),
      flowName: "MyFlow"
    );

    _formatter.Format(result, _logger);

    var allMessages = string.Join(" ", _logger.Messages);
    Assert.That(allMessages, Does.Contain("external").IgnoreCase);
  }

  [Test]
  public void Format_ExternalErrorFailure_IssueUrlLoggedAtWarningLevel()
  {
    var result = FlowResult.CreateFailure(
      TimeSpan.FromSeconds(1),
      new IOException("disk read error"),
      flowName: "MyFlow"
    );

    _formatter.Format(result, _logger);

    // Classification message for external errors is Warning, not Error.
    var classificationEntry = _logger.Entries.FirstOrDefault(e =>
      e.Level == LogLevel.Warning && e.Message.Contains("external")
    );
    Assert.That(classificationEntry, Is.Not.Null);
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Issue URL contains step name when failure is step-scoped
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void Format_StepScopedFailure_IssueUrlContainsStepName()
  {
    var failedStep = StepResult.CreateFailure(
      "ComputeFeatures",
      TimeSpan.FromMilliseconds(500),
      new InvalidOperationException("step blew up")
    );

    var result = FlowResult.CreateFailure(
      TimeSpan.FromSeconds(1),
      failedStep.Exception!,
      stepResults: new Dictionary<string, StepResult> { ["ComputeFeatures"] = failedStep },
      flowName: "MyFlow"
    );

    _formatter.Format(result, _logger);

    var urlEntry = _logger.Messages.FirstOrDefault(m =>
      m.Contains("github.com/chaoticgoodcomputing/flowthru/issues/new")
    );
    Assert.That(urlEntry, Is.Not.Null);
    Assert.That(urlEntry, Does.Contain(Uri.EscapeDataString("ComputeFeatures")));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // FlowExecutionEscapedException — the escape-path framing must fire
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void Format_FlowExecutionEscapedException_EmitsIssueUrlAtErrorLevel()
  {
    // Real-world scenario this guards against: a step throws an exception
    // that propagates past Flow's structured-failure boundary (e.g., a
    // cancellation cascade leaks out of the executor). The service-level
    // wrap converts it to a FlowExecutionEscapedException-wrapped FlowResult
    // before formatting. Even if the inner exception is allowlisted (here,
    // TaskCanceledException), the framing must still fire — the escape
    // itself is the framework-bug signal.
    var escaped = new FlowExecutionEscapedException(
      "Flow execution aborted by an unexpected cancellation.",
      new TaskCanceledException()
    );
    var result = FlowResult.CreateFailure(TimeSpan.FromSeconds(2), escaped, flowName: "MyFlow");

    _formatter.Format(result, _logger);

    var issueEntry = _logger.Entries.FirstOrDefault(e =>
      e.Message.Contains("github.com/chaoticgoodcomputing/flowthru/issues/new")
    );
    Assert.That(
      issueEntry,
      Is.Not.Null,
      "Issue URL must appear in output when a FlowExecutionEscapedException is in play"
    );
    Assert.That(
      issueEntry!.Level,
      Is.EqualTo(LogLevel.Error),
      "FlowExecutionEscapedException is always classified as a possible framework bug, "
        + "so the framing logs at Error level"
    );
  }
}
