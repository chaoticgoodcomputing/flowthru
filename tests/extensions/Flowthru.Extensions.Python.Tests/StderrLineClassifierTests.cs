using Flowthru.Extensions.Python.Step.Python.Internal;
using Microsoft.Extensions.Logging;

namespace Flowthru.Extensions.Python.Tests;

/// <summary>
/// Unit coverage for <see cref="StderrLineClassifier"/> — the
/// per-line decision point of the Python worker → engine
/// <c>ILogger</c> stderr bridge. Structured frames carry an explicit
/// level; raw lines default to <see cref="LogLevel.Information"/>
/// with a traceback heuristic.
/// </summary>
[TestFixture]
[Category("Python")]
public class StderrLineClassifierTests
{
  // ── Structured-frame path (the `__flowthru_log__:` prefix) ──────────────

  [TestCase("DEBUG", LogLevel.Debug)]
  [TestCase("INFO", LogLevel.Information)]
  [TestCase("WARNING", LogLevel.Warning)]
  [TestCase("ERROR", LogLevel.Error)]
  [TestCase("CRITICAL", LogLevel.Critical)]
  public void StructuredFrame_MapsPythonLevelToILoggerLevel(
    string pythonLevel,
    LogLevel expected
  )
  {
    var frame =
      $"__flowthru_log__:{{\"level\":\"{pythonLevel}\",\"logger\":\"my.module\",\"msg\":\"hello\"}}";

    var (level, _) = StderrLineClassifier.Classify(frame);

    Assert.That(level, Is.EqualTo(expected));
  }

  [Test]
  public void StructuredFrame_RendersLoggerNameAsPrefix()
  {
    var frame = "__flowthru_log__:{\"level\":\"INFO\",\"logger\":\"my.module\",\"msg\":\"hi\"}";

    var (_, message) = StderrLineClassifier.Classify(frame);

    Assert.That(message, Is.EqualTo("[my.module] hi"),
      "Logger names should be surfaced as a bracketed prefix so the host log keeps "
      + "per-module context Python developers rely on.");
  }

  [Test]
  public void StructuredFrame_WithoutLoggerName_RendersMessageOnly()
  {
    var frame = "__flowthru_log__:{\"level\":\"INFO\",\"msg\":\"hi\"}";

    var (_, message) = StderrLineClassifier.Classify(frame);

    Assert.That(message, Is.EqualTo("hi"));
  }

  [Test]
  public void StructuredFrame_WithExcInfo_AppendsTracebackToMessage()
  {
    var frame =
      "__flowthru_log__:{\"level\":\"ERROR\",\"logger\":\"my.module\","
      + "\"msg\":\"oops\",\"exc\":\"Traceback (most recent call last):\\n  ...\"}";

    var (level, message) = StderrLineClassifier.Classify(frame);

    Assert.That(level, Is.EqualTo(LogLevel.Error));
    Assert.That(message, Does.Contain("oops"));
    Assert.That(message, Does.Contain("Traceback (most recent call last):"),
      "exc_info on the Python side carries the formatted traceback; "
      + "the classifier should preserve it so the host log shows the cause.");
  }

  [Test]
  public void MalformedFrame_FallsThroughToInformationDefault()
  {
    // Prefix present but JSON unparseable. Rather than dropping the line,
    // the classifier returns the raw text at the default level so the
    // bridge-bug evidence still reaches the operator.
    var line = "__flowthru_log__:not a json object";

    var (level, message) = StderrLineClassifier.Classify(line);

    Assert.That(level, Is.EqualTo(LogLevel.Information));
    Assert.That(message, Is.EqualTo(line));
  }

  [Test]
  public void UnknownPythonLevel_FallsThroughToInformation()
  {
    // Best-effort observation: a non-canonical level name from a future
    // Python release or a custom logging filter shouldn't crash the
    // bridge.
    var frame = "__flowthru_log__:{\"level\":\"FYI\",\"logger\":\"x\",\"msg\":\"hi\"}";

    var (level, _) = StderrLineClassifier.Classify(frame);

    Assert.That(level, Is.EqualTo(LogLevel.Information));
  }

  // ── Raw-line path (no prefix) ───────────────────────────────────────────

  [Test]
  public void RawPrintLine_DefaultsToInformation()
  {
    var (level, message) = StderrLineClassifier.Classify("Dropped 14 rows");

    Assert.That(level, Is.EqualTo(LogLevel.Information));
    Assert.That(message, Is.EqualTo("Dropped 14 rows"));
  }

  [Test]
  public void TracebackHeader_ElevatesToError()
  {
    // A Python uncaught exception produces a first stderr line of exactly
    // this shape. Elevating to Error makes the failure obvious in the
    // host's log without coordination between the worker and the host.
    var (level, _) = StderrLineClassifier.Classify("Traceback (most recent call last):");

    Assert.That(level, Is.EqualTo(LogLevel.Error));
  }

  [Test]
  public void EmptyLine_DefaultsToInformation()
  {
    var (level, message) = StderrLineClassifier.Classify(string.Empty);

    Assert.That(level, Is.EqualTo(LogLevel.Information));
    Assert.That(message, Is.EqualTo(string.Empty));
  }
}
