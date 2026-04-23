using System.Web;
using Flowthru.Core.Flows;
using Flowthru.Core.Results;

namespace Flowthru.Tests.Results;

/// <summary>
/// Tests for <see cref="GitHubIssueUrlBuilder"/>.
/// </summary>
[TestFixture]
[Category("Results")]
[Category("GitHubIssueUrlBuilder")]
public class GitHubIssueUrlBuilderTests
{
  private static RuntimeErrorReport BuildReport(
    Exception? exception = null,
    ErrorClassification classification = ErrorClassification.PossibleFrameworkBug,
    string flowName = "TestFlow",
    string failedStep = "TestStep"
  )
  {
    exception ??= new InvalidOperationException("something broke");
    return new RuntimeErrorReport
    {
      FlowthruVersion = "0.8.0",
      RuntimeVersion = ".NET 10.0",
      OperatingSystem = "Linux 5.15",
      FlowName = flowName,
      FailedStepName = failedStep,
      Exception = exception,
      Classification = classification,
      CompletedSteps = ["StepA", "StepB"],
    };
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Structure
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void Build_AlwaysTargetsCorrectRepository()
  {
    var url = GitHubIssueUrlBuilder.Build(BuildReport());

    Assert.That(url, Does.StartWith("https://github.com/chaoticgoodcomputing/flowthru/issues/new"));
  }

  [Test]
  public void Build_ContainsTitleQueryParam()
  {
    var url = GitHubIssueUrlBuilder.Build(BuildReport());

    Assert.That(url, Does.Contain("title="));
  }

  [Test]
  public void Build_ContainsBodyQueryParam()
  {
    var url = GitHubIssueUrlBuilder.Build(BuildReport());

    Assert.That(url, Does.Contain("body="));
  }

  [Test]
  public void Build_ContainsLabelsQueryParam()
  {
    var url = GitHubIssueUrlBuilder.Build(BuildReport());

    Assert.That(url, Does.Contain("labels="));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Labels
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void Build_PossibleFrameworkBug_UsesBugLabel()
  {
    var url = GitHubIssueUrlBuilder.Build(
      BuildReport(classification: ErrorClassification.PossibleFrameworkBug)
    );

    Assert.That(url, Does.Contain(Uri.EscapeDataString("bug")));
  }

  [Test]
  public void Build_ExternalError_UsesExternalErrorLabel()
  {
    var url = GitHubIssueUrlBuilder.Build(
      BuildReport(classification: ErrorClassification.ExternalError)
    );

    Assert.That(url, Does.Contain(Uri.EscapeDataString("external-error")));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Body content (decoded assertions)
  // ─────────────────────────────────────────────────────────────────────────

  private static string DecodeBody(string url)
  {
    var query = new Uri(url).Query.TrimStart('?');
    foreach (var part in query.Split('&'))
    {
      var eq = part.IndexOf('=');
      if (eq < 0)
        continue;
      var key = Uri.UnescapeDataString(part[..eq]);
      if (key == "body")
        return Uri.UnescapeDataString(part[(eq + 1)..]);
    }
    return string.Empty;
  }

  [Test]
  public void Build_Body_ContainsFlowthruVersion()
  {
    var url = GitHubIssueUrlBuilder.Build(BuildReport());
    var body = DecodeBody(url);

    Assert.That(body, Does.Contain("0.8.0"));
  }

  [Test]
  public void Build_Body_ContainsFlowName()
  {
    var url = GitHubIssueUrlBuilder.Build(BuildReport(flowName: "MySpecialFlow"));
    var body = DecodeBody(url);

    Assert.That(body, Does.Contain("MySpecialFlow"));
  }

  [Test]
  public void Build_Body_ContainsFailedStepName()
  {
    var url = GitHubIssueUrlBuilder.Build(BuildReport(failedStep: "CrunchNumbers"));
    var body = DecodeBody(url);

    Assert.That(body, Does.Contain("CrunchNumbers"));
  }

  [Test]
  public void Build_Body_ContainsExceptionTypeName()
  {
    var url = GitHubIssueUrlBuilder.Build(
      BuildReport(exception: new InvalidOperationException("boom"))
    );
    var body = DecodeBody(url);

    Assert.That(body, Does.Contain("InvalidOperationException"));
  }

  [Test]
  public void Build_Body_ContainsExceptionMessage()
  {
    var url = GitHubIssueUrlBuilder.Build(
      BuildReport(exception: new InvalidOperationException("very specific error message"))
    );
    var body = DecodeBody(url);

    Assert.That(body, Does.Contain("very specific error message"));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // URL length safety
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void Build_WithNormalReport_StaysWithinUrlLimit()
  {
    var url = GitHubIssueUrlBuilder.Build(BuildReport());

    Assert.That(url.Length, Is.LessThanOrEqualTo(8000));
  }

  [Test]
  public void Build_WithHugeStackTrace_StaysWithinUrlLimit()
  {
    Exception ex;
    try
    {
      // Generate a real (deep) stack trace via recursion.
      static void Recurse(int depth)
      {
        if (depth == 0)
          throw new InvalidOperationException("deep failure: " + new string('x', 2000));
        Recurse(depth - 1);
      }
      Recurse(200);
      ex = new InvalidOperationException("fallback"); // unreachable
    }
    catch (Exception caught)
    {
      ex = caught;
    }

    var report = BuildReport(exception: ex);
    var url = GitHubIssueUrlBuilder.Build(report);

    Assert.That(url.Length, Is.LessThanOrEqualTo(8000));
  }

  [Test]
  public void Build_WithHugeExceptionMessage_StaysWithinUrlLimit()
  {
    var ex = new InvalidOperationException(new string('z', 10_000));
    var url = GitHubIssueUrlBuilder.Build(BuildReport(exception: ex));

    Assert.That(url.Length, Is.LessThanOrEqualTo(8000));
  }
}
