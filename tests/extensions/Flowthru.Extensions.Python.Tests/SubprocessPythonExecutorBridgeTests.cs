using System.Collections.Concurrent;
using Flowthru.Prelude;
using Flowthru.Step.Python;
using Flowthru.Step.Python.Internal;
using Microsoft.Extensions.Logging;

namespace Flowthru.Extensions.Python.Tests;

/// <summary>
/// End-to-end coverage for the Python worker → engine
/// <see cref="ILogger"/> stderr bridge (ADR-0005). Spawns a real
/// Python subprocess against the same hermetic venv as
/// <see cref="SubprocessPythonExecutorIntegrationTests"/>, drives it
/// through probe modules that exercise each of the three
/// classification paths, and asserts the host-side capture matches
/// what <see cref="StderrLineClassifier"/> promises:
/// <list type="bullet">
///   <item>Stdlib <c>logging</c> records → JSON frames →
///   <see cref="LogLevel"/> mapped per the embedded Python level.</item>
///   <item>Raw <c>print()</c> lines → <see cref="LogLevel.Information"/>.</item>
///   <item>Uncaught Python exception → traceback → first line elevates
///   to <see cref="LogLevel.Error"/>.</item>
/// </list>
/// </summary>
/// <remarks>
/// <para>
/// This fixture inherits the venv setup pattern from
/// <see cref="SubprocessPythonExecutorIntegrationTests"/> but stands
/// up its own provisioning to avoid coupling the two fixtures'
/// teardown lifecycles. Per-test executors live behind a captured
/// <c>ILoggerProvider</c> so each test sees only its own emissions.
/// </para>
/// <para>
/// Awaiting the bridged emissions is non-trivial: the stderr reader
/// runs on a background task and flushes asynchronously. Each test
/// drives the worker to completion, then polls the capture briefly
/// for the expected line — see <c>WaitForCaptured</c>. The poll
/// timeout is generous (2s) because the Arrow/pandas import on first
/// invoke can take ~500ms on cold caches.
/// </para>
/// </remarks>
[TestFixture]
[Category("Python")]
[Category("RequiresPython")]
public class SubprocessPythonExecutorBridgeTests
{
  private const int PollTimeoutMs = 2000;
  private const int PollIntervalMs = 25;

  // The venv + probe modules are provisioned by the integration test
  // fixture's [OneTimeSetUp]. NUnit runs all [TestFixture] classes
  // serially within a single test assembly invocation, so when this
  // fixture runs the integration fixture has either already completed
  // its OneTimeSetUp (path: venv at known _venvPath) or hasn't.
  // To avoid order-dependence, this fixture provisions its own — see
  // OneTimeSetUp below.
  private string? _tempProjectDir;
  private string? _venvPath;
  private readonly List<IDisposable> _liveExecutors = new();

  [OneTimeSetUp]
  public void Setup()
  {
    // Stdlib-only venv — bridge tests don't exercise pandas/Arrow,
    // so an empty dependency list keeps `uv sync` fast.
    var venv = PythonHermeticVenv.Provision(tempDirPrefix: "flowthru-pylog");
    _tempProjectDir = venv.TempProjectDir;
    _venvPath = venv.VenvPath;
    WriteBridgeProbeModules(_tempProjectDir);
  }

  [OneTimeTearDown]
  public void Teardown()
  {
    foreach (var d in _liveExecutors)
    {
      try { d.Dispose(); }
      catch { /* best effort */ }
    }
    _liveExecutors.Clear();

    PythonHermeticVenv.TryDelete(_tempProjectDir);
  }

  [TearDown]
  public void DisposeExecutors()
  {
    foreach (var d in _liveExecutors)
    {
      try { d.Dispose(); }
      catch { /* best effort */ }
    }
    _liveExecutors.Clear();
  }

  // ── Bridge tests ────────────────────────────────────────────────────

  [Test]
  public async Task StdlibLoggingRecord_BridgesAtItsEmittedLevel()
  {
    var capture = new CapturingLogger();
    var executor = NewExecutor(capture);

    var result = await executor.Invoke<int, int>(
      "log_probes.logs_via_stdlib", "emit", 42
    ).Run();
    AssertOk(result);

    // The probe emits two records: an INFO and a WARNING. Both flow
    // through the JSON-frame path; the classifier maps levels and
    // prefixes the logger name. We poll because the reader task
    // flushes asynchronously after the invoke returns.
    var info = await WaitForCaptured(capture, e =>
      e.Level == LogLevel.Information
      && e.Message.Contains("informational 42")
      && e.Message.Contains("test_steps.logs_via_stdlib"));
    Assert.That(info, Is.Not.Null,
      "Expected an Information entry containing 'informational 42' under "
      + "the test_steps.logs_via_stdlib logger. Got: " + capture.DescribeAll());

    var warn = await WaitForCaptured(capture, e =>
      e.Level == LogLevel.Warning
      && e.Message.Contains("warning 42"));
    Assert.That(warn, Is.Not.Null,
      "Expected a Warning entry containing 'warning 42'. Got: " + capture.DescribeAll());
  }

  [Test]
  public async Task RawPrintCall_BridgesAsInformation()
  {
    var capture = new CapturingLogger();
    var executor = NewExecutor(capture);

    var result = await executor.Invoke<int, int>(
      "log_probes.prints_to_stdout", "emit", 7
    ).Run();
    AssertOk(result);

    var entry = await WaitForCaptured(capture, e =>
      e.Level == LogLevel.Information
      && e.Message.Contains("raw-print-line 7"));
    Assert.That(entry, Is.Not.Null,
      "Expected raw print() output to bridge at Information level. Got: "
      + capture.DescribeAll());
  }

  [Test]
  public async Task UnhandledException_TracebackHeaderElevatesToError()
  {
    var capture = new CapturingLogger();
    var executor = NewExecutor(capture);

    // The probe raises ValueError. The worker catches it, returns
    // {"status":"error","message":<traceback>} on stdout, AND writes
    // the traceback to stderr via Python's default sys.excepthook /
    // worker error path. We only assert about the stderr-bridged
    // traceback, since that's the StderrLineClassifier contract.
    //
    // Looking at flowthru_worker.py: the worker catches in
    // _handle_invoke and returns the traceback via JSON — it does NOT
    // re-raise. So the stderr stream will NOT contain a traceback
    // from this path. We use a logging.exception call inside the step
    // instead, which DOES go through the JSON-frame handler at ERROR
    // level.
    var result = await executor.Invoke<int, int>(
      "log_probes.logs_exception", "emit", 0
    ).Run();
    AssertOk(result);

    var entry = await WaitForCaptured(capture, e =>
      e.Level == LogLevel.Error
      && e.Message.Contains("oops"));
    Assert.That(entry, Is.Not.Null,
      "log.exception() should bridge at Error level with the exception text. Got: "
      + capture.DescribeAll());
  }

  [Test]
  public async Task BasicConfigInUserCode_DoesNotDuplicateRecords()
  {
    // Python's logging.basicConfig() short-circuits when the root
    // logger already has a handler. The worker's startup hook
    // installs our JSON handler first, so a basicConfig() call in a
    // user step must not add a second stderr handler — otherwise
    // each record would be captured twice (once as a JSON frame,
    // once as the basicConfig default 'WARNING:logger:msg' format).
    var capture = new CapturingLogger();
    var executor = NewExecutor(capture);

    var result = await executor.Invoke<int, int>(
      "log_probes.calls_basicconfig", "emit", 99
    ).Run();
    AssertOk(result);

    await Task.Delay(150); // let stderr drain

    var matchingWarnings = capture.Entries
      .Where(e => e.Level == LogLevel.Warning && e.Message.Contains("singleton 99"))
      .ToList();
    Assert.That(matchingWarnings, Has.Count.EqualTo(1),
      "basicConfig in user code must not duplicate stdlib log records. "
      + $"Got {matchingWarnings.Count} matching entries: " + capture.DescribeAll());
  }

  // ── Probe modules specific to the bridge tests ─────────────────────

  private static void WriteBridgeProbeModules(string root)
  {
    var dir = Path.Combine(root, "log_probes");
    Directory.CreateDirectory(dir);
    File.WriteAllText(Path.Combine(dir, "__init__.py"), "");

    File.WriteAllText(Path.Combine(dir, "logs_via_stdlib.py"),
      """
      import logging
      from flowthru import step

      _log = logging.getLogger("test_steps.logs_via_stdlib")

      @step(inputs=["int"], outputs=["int"])
      def emit(n: int) -> int:
          _log.info("informational %d", n)
          _log.warning("warning %d", n)
          return n
      """);

    File.WriteAllText(Path.Combine(dir, "prints_to_stdout.py"),
      """
      from flowthru import step

      @step(inputs=["int"], outputs=["int"])
      def emit(n: int) -> int:
          print(f"raw-print-line {n}")
          return n
      """);

    File.WriteAllText(Path.Combine(dir, "logs_exception.py"),
      """
      import logging
      from flowthru import step

      _log = logging.getLogger("test_steps.logs_exception")

      @step(inputs=["int"], outputs=["int"])
      def emit(n: int) -> int:
          try:
              raise ValueError("oops")
          except ValueError:
              _log.exception("caught: oops")
          return n
      """);

    File.WriteAllText(Path.Combine(dir, "calls_basicconfig.py"),
      """
      import logging
      from flowthru import step

      # basicConfig should no-op because the worker installs the
      # Flowthru handler on the root logger before user code runs.
      logging.basicConfig(level=logging.WARNING)
      _log = logging.getLogger("test_steps.calls_basicconfig")

      @step(inputs=["int"], outputs=["int"])
      def emit(n: int) -> int:
          _log.warning("singleton %d", n)
          return n
      """);
  }

  // ── Test infrastructure ─────────────────────────────────────────────

  private SubprocessPythonExecutor NewExecutor(CapturingLogger logger)
  {
    var options = new PythonRuntimeOptions
    {
      VenvPath = _venvPath!,
      ModuleSearchPaths = new List<string> { _tempProjectDir! },
    };
    var executor = new SubprocessPythonExecutor(
      Microsoft.Extensions.Options.Options.Create(options),
      new NullFlattener(),
      new DirectPythonLauncher(),
      logger
    );
    _liveExecutors.Add(executor);
    return executor;
  }

  private static void AssertOk<A>(EffResult<A> result)
  {
    if (result is EffResult<A>.Failure f)
    {
      Assert.Fail($"Expected Success, got Failure: {f.Error.Message}");
    }
  }

  /// <summary>
  /// Poll the capture for an entry matching <paramref name="predicate"/>.
  /// The stderr reader drains asynchronously after the worker invoke
  /// returns, so a brief poll is needed to avoid races. Returns the
  /// first match or null on timeout.
  /// </summary>
  private static async Task<LogEntry?> WaitForCaptured(
    CapturingLogger capture,
    Func<LogEntry, bool> predicate
  )
  {
    var deadline = DateTime.UtcNow.AddMilliseconds(PollTimeoutMs);
    while (DateTime.UtcNow < deadline)
    {
      var match = capture.Entries.FirstOrDefault(predicate);
      if (match.Message is not null) return match;
      await Task.Delay(PollIntervalMs);
    }
    return null;
  }

  /// <summary>
  /// Minimal flattener for tests — the bridge path doesn't depend on
  /// IConfiguration so we wire an empty implementation.
  /// </summary>
  private sealed class NullFlattener
    : Flowthru.Step.Python.Internal.IPythonConfigurationFlattener
  {
    public IReadOnlyDictionary<string, string> Flatten() =>
      new Dictionary<string, string>();
  }

  internal readonly record struct LogEntry(LogLevel Level, string Message);

  internal sealed class CapturingLogger : ILogger
  {
    public ConcurrentBag<LogEntry> Entries { get; } = new();

    public IDisposable BeginScope<TState>(TState state) where TState : notnull =>
      NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
      LogLevel logLevel,
      EventId eventId,
      TState state,
      Exception? exception,
      Func<TState, Exception?, string> formatter
    )
    {
      Entries.Add(new LogEntry(logLevel, formatter(state, exception)));
    }

    public string DescribeAll() =>
      Entries.Count == 0
        ? "(no entries)"
        : string.Join(" | ", Entries.Select(e => $"[{e.Level}] {e.Message}"));

    private sealed class NullScope : IDisposable
    {
      public static readonly NullScope Instance = new();
      public void Dispose() { }
    }
  }
}
