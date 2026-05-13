using System.Diagnostics;
using Flowthru.Data.Schema;
using Flowthru.Data.Storage;
using Flowthru.Prelude;
using Flowthru.Step.Python;
using Flowthru.Step.Python.Internal;
using Flowthru.Validation.PreFlight;
using Flowthru.Validation.PreFlight.Python;
using Flowthru.Validation.Runtime;
using Flowthru.Validation.Runtime.Python;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Flowthru.Extensions.Python.Tests;

/// <summary>
/// End-to-end coverage for <see cref="SubprocessPythonExecutor"/> with a real
/// Python subprocess. The fixture provisions a hermetic <c>uv</c>-managed venv
/// once (with <c>pyarrow</c> + <c>pandas</c> + <c>numpy</c>) and writes a small
/// pool of <c>@step</c>-decorated probe modules a single test can target.
/// </summary>
/// <remarks>
/// <para>
/// Gated by <c>Category("RequiresPython")</c> — the regular
/// <c>dotnet test</c> run filters this fixture out unless explicitly
/// requested via <c>--filter Category=RequiresPython</c>. Pre-flight
/// failures during <c>OneTimeSetUp</c> (no <c>uv</c>, package install
/// failure, missing worker script) surface as <c>Assert.Ignore</c> rather
/// than a fixture-wide failure so the rest of the suite is never disturbed.
/// </para>
/// <para>
/// Each test instantiates its own <see cref="SubprocessPythonExecutor"/> and
/// disposes it in <c>TearDown</c> — isolation guarantees no inter-test
/// state and no child-process leaks.
/// </para>
/// </remarks>
[TestFixture]
[Category("Python")]
[Category("RequiresPython")]
public class SubprocessPythonExecutorIntegrationTests
{
  // ── Probe schemas — Arrow-marshalable shapes for tabular round trips ─

  [FlowthruSchema]
  public partial record IntRow
  {
    public required int Id { get; init; }
    public required string Name { get; init; }
  }

  [FlowthruSchema]
  public partial record UnsupportedDecimalRow
  {
    public required int Id { get; init; }
    // Decimal is intentionally not Arrow-marshalable; this exercises the
    // "marshalling failure surfaces unwrapped via FormatInnerExceptionDetail"
    // path. The user must see "NotSupportedException" + property name, not
    // "Exception has been thrown by the target of an invocation."
    public required decimal Amount { get; init; }
  }

  // ── Fixture state ───────────────────────────────────────────────────

  /// <summary>Temp dir holding the test pyflows + venv.</summary>
  private string _tempProjectDir = string.Empty;

  /// <summary>Absolute path to the venv (e.g. /tmp/.../.venv).</summary>
  private string _venvPath = string.Empty;

  /// <summary>Tracks every executor so an [OneTimeTearDown] sweep can dispose any leaks.</summary>
  private readonly List<SubprocessPythonExecutor> _liveExecutors = new();

  // ── One-shot setup: provision venv + write step modules ─────────────

  [OneTimeSetUp]
  public void ProvisionPythonEnvironment()
  {
    // The worker script ships next to the test assembly via the test csproj's
    // <Content Include="...flowthru_worker.py"> entry. If it's missing the
    // copy step did not run — bail out clearly.
    var workerScript = Path.Combine(AppContext.BaseDirectory, "flowthru_worker.py");
    if (!File.Exists(workerScript))
    {
      Assert.Ignore(
        $"flowthru_worker.py not found at '{workerScript}'. " +
        "Check that the test csproj's <Content> items propagated to the output directory."
      );
    }

    _tempProjectDir = Path.Combine(
      Path.GetTempPath(),
      $"flowthru-pytest-{Guid.NewGuid():N}"
    );
    Directory.CreateDirectory(_tempProjectDir);

    // Minimal pyproject.toml — uv lock + sync resolves transitive deps.
    File.WriteAllText(
      Path.Combine(_tempProjectDir, "pyproject.toml"),
      """
      [project]
      name = "flowthru-pytest"
      version = "0.0.1"
      description = "Probe project for SubprocessPythonExecutor integration tests"
      requires-python = ">=3.10"
      dependencies = ["pandas>=2.0.0", "pyarrow>=18.0.0", "numpy>=1.24.0"]
      """
    );

    // `uv lock` produces uv.lock; `uv sync --frozen` materializes .venv/.
    // Both steps are gated — if either fails (no uv on PATH, no network,
    // resolver failure) the whole fixture is ignored rather than failing.
    if (!RunUv("lock", _tempProjectDir, out var lockErr))
    {
      Assert.Ignore($"`uv lock` failed: {lockErr}");
    }

    if (!RunUv("sync --frozen", _tempProjectDir, out var syncErr))
    {
      Assert.Ignore($"`uv sync --frozen` failed: {syncErr}");
    }

    _venvPath = Path.Combine(_tempProjectDir, ".venv");
    if (!Directory.Exists(_venvPath))
    {
      Assert.Ignore($"venv was not created at '{_venvPath}' after uv sync.");
    }

    WriteProbeModules(_tempProjectDir);
  }

  [OneTimeTearDown]
  public void TeardownPythonEnvironment()
  {
    // Defensive sweep — every per-test [TearDown] should already have
    // disposed its executor, but if a test bailed early we still want
    // to release the child process.
    foreach (var executor in _liveExecutors)
    {
      try { executor.Dispose(); }
      catch { /* best effort */ }
    }
    _liveExecutors.Clear();

    if (!string.IsNullOrEmpty(_tempProjectDir) && Directory.Exists(_tempProjectDir))
    {
      try { Directory.Delete(_tempProjectDir, recursive: true); }
      catch { /* swallow — temp dir cleanup is advisory */ }
    }
  }

  // ── Per-test executor lifecycle ─────────────────────────────────────

  private SubprocessPythonExecutor CreateExecutor(
    PythonRuntimeOptions? overrideOptions = null
  )
  {
    var options = overrideOptions ?? new PythonRuntimeOptions
    {
      VenvPath = _venvPath,
      ModuleSearchPaths = new List<string> { _tempProjectDir },
    };
    var executor = new SubprocessPythonExecutor(
      Options.Create(options),
      new NullFlattener(),
      NullLogger<SubprocessPythonExecutor>.Instance
    );
    _liveExecutors.Add(executor);
    return executor;
  }

  [TearDown]
  public void DisposeExecutors()
  {
    foreach (var executor in _liveExecutors)
    {
      try { executor.Dispose(); }
      catch { /* best effort */ }
    }
    _liveExecutors.Clear();
  }

  // ── Lifecycle tests ─────────────────────────────────────────────────

  [Test]
  public async Task Invoke_FirstCall_StartsWorkerSuccessfully()
  {
    var executor = CreateExecutor();
    var io = executor.Invoke<int, string>("test_steps.scalar_echo", "echo", 42);
    var result = await io.Run();

    AssertSuccess(result, out var value);
    Assert.That(value, Is.EqualTo("answer: 42"));
  }

  [Test]
  public async Task Invoke_SecondCall_ReusesRunningWorker()
  {
    var executor = CreateExecutor();

    var first = await executor.Invoke<int, string>("test_steps.scalar_echo", "echo", 1).Run();
    AssertSuccess(first, out _);

    // No way to read the internal _worker handle without reflection, but if
    // a fresh worker were spawned the second invocation would re-pay the
    // import-cache miss — what we really care about is correctness +
    // idempotency at the public surface.
    var second = await executor.Invoke<int, string>("test_steps.scalar_echo", "echo", 2).Run();
    AssertSuccess(second, out var v);
    Assert.That(v, Is.EqualTo("answer: 2"));
  }

  [Test]
  public async Task Dispose_TerminatesWorker_AndSubsequentInvokeReportsBrokenPipe()
  {
    var executor = CreateExecutor();
    var first = await executor.Invoke<int, string>("test_steps.scalar_echo", "echo", 1).Run();
    AssertSuccess(first, out _);

    executor.Dispose();
    // Drop from live-list since we explicitly disposed.
    _liveExecutors.Remove(executor);

    // Post-dispose Invoke should fail (broken stdin/stdout pipes). The exact
    // surface depends on whether the worker's pipe is half-closed or fully
    // dead by the time we ask — we just assert that the call surfaces as a
    // Failure, not a hung or successful operation.
    var afterDispose = await executor.Invoke<int, string>("test_steps.scalar_echo", "echo", 3).Run();
    Assert.That(afterDispose, Is.InstanceOf<EffResult<string>.Failure>(),
      "Invoke after Dispose must fail — the subprocess and pipes are gone.");
  }

  [Test]
  public void Dispose_IsIdempotent()
  {
    var executor = CreateExecutor();
    _liveExecutors.Remove(executor);

    Assert.DoesNotThrow(() => executor.Dispose());
    Assert.DoesNotThrow(() => executor.Dispose(),
      "A second Dispose must be a no-op — the executor short-circuits on _disposed.");
    Assert.DoesNotThrow(() => executor.Dispose());
  }

  // ── Invoke happy paths ──────────────────────────────────────────────

  [Test]
  public async Task Invoke_Scalar_RoundTripsThroughJson()
  {
    var executor = CreateExecutor();
    var io = executor.Invoke<int, string>("test_steps.scalar_echo", "echo", 7);
    var result = await io.Run();
    AssertSuccess(result, out var value);
    Assert.That(value, Is.EqualTo("answer: 7"));
  }

  [Test]
  public async Task Invoke_Tabular_RoundTripsThroughArrowIpc()
  {
    var executor = CreateExecutor();
    var rows = new[]
    {
      new IntRow { Id = 1, Name = "alpha" },
      new IntRow { Id = 2, Name = "beta"  },
      new IntRow { Id = 3, Name = "gamma" },
    };

    var io = executor.Invoke<IEnumerable<IntRow>, IEnumerable<IntRow>>(
      "test_steps.tabular_increment", "bump", rows
    );
    var result = await io.Run();
    AssertSuccess(result, out var output);

    var materialized = output.ToList();
    Assert.That(materialized.Select(r => r.Id), Is.EqualTo(new[] { 2, 3, 4 }));
    Assert.That(materialized.Select(r => r.Name), Is.EqualTo(new[] { "alpha", "beta", "gamma" }));
  }

  [Test]
  public async Task Invoke_MultiOutputTuple_DecodesBothElements()
  {
    var executor = CreateExecutor();
    var rows = new[]
    {
      new IntRow { Id = 10, Name = "x" },
      new IntRow { Id = 20, Name = "y" },
    };

    var io = executor.Invoke<IEnumerable<IntRow>, (int, IEnumerable<IntRow>)>(
      "test_steps.tabular_count_and_pass", "count_and_pass", rows
    );
    var result = await io.Run();
    AssertSuccess(result, out var output);

    Assert.That(output.Item1, Is.EqualTo(2), "Count element of the tuple must be 2.");
    var passed = output.Item2.ToList();
    Assert.That(passed.Count, Is.EqualTo(2));
    Assert.That(passed[0].Name, Is.EqualTo("x"));
  }

  [Test]
  public async Task Invoke_Bytes_RoundTripsThroughBase64()
  {
    var executor = CreateExecutor();
    var input = new byte[] { 1, 2, 3, 4, 5 };

    var io = executor.Invoke<byte[], byte[]>("test_steps.bytes_reverse", "rev", input);
    var result = await io.Run();
    AssertSuccess(result, out var output);

    Assert.That(output, Is.EqualTo(new byte[] { 5, 4, 3, 2, 1 }));
  }

  [Test]
  public async Task Invoke_DirectoryOf_RoundTripsThroughDirectoryProtocol()
  {
    var executor = CreateExecutor();
    var dir = new DirectoryOf<IEnumerable<IntRow>>(new Dictionary<string, IEnumerable<IntRow>>
    {
      ["a.csv"] = new[] { new IntRow { Id = 1, Name = "one" } },
      ["b.csv"] = new[] { new IntRow { Id = 2, Name = "two" } },
    });

    var io = executor.Invoke<DirectoryOf<IEnumerable<IntRow>>, DirectoryOf<IEnumerable<IntRow>>>(
      "test_steps.directory_passthrough", "passthrough", dir
    );
    var result = await io.Run();
    AssertSuccess(result, out var output);

    Assert.That(output.Count, Is.EqualTo(2));
    Assert.That(output.Keys, Is.EquivalentTo(new[] { "a.csv", "b.csv" }));
    Assert.That(output["a.csv"].Single().Id, Is.EqualTo(1));
    Assert.That(output["b.csv"].Single().Name, Is.EqualTo("two"));
  }

  // ── ValidateStep ────────────────────────────────────────────────────

  [Test]
  public async Task ValidateStep_DecoratedFunction_ReturnsMetadata()
  {
    var executor = CreateExecutor();
    var io = executor.ValidateStep("test_steps.decorated", "transform");
    var result = await io.Run();
    AssertSuccess(result, out var meta);

    Assert.That(meta.Inputs, Is.EqualTo(new[] { "InputSchema" }));
    Assert.That(meta.Outputs, Is.EqualTo(new[] { "OutputSchema" }));
    Assert.That(meta.Services, Is.EqualTo(new[] { "Services.MyService" }));
  }

  [Test]
  public async Task ValidateStep_MissingModule_SurfacesAsModuleNotFound()
  {
    var executor = CreateExecutor();
    var io = executor.ValidateStep("test_steps.does_not_exist", "anything");
    var result = await io.Run();
    var error = AssertExtensionFailure<PythonStepMetadata>(result);
    Assert.That(error, Is.InstanceOf<PythonRuntimeError.ModuleNotFound>(),
      $"Expected ModuleNotFound; got {error.GetType().Name}: {error.Message}");
  }

  [Test]
  public async Task ValidateStep_MissingFunction_SurfacesAsFunctionMissing()
  {
    var executor = CreateExecutor();
    var io = executor.ValidateStep("test_steps.decorated", "no_such_function");
    var result = await io.Run();
    var error = AssertExtensionFailure<PythonStepMetadata>(result);
    Assert.That(error, Is.InstanceOf<PythonRuntimeError.FunctionMissing>(),
      $"Expected FunctionMissing; got {error.GetType().Name}: {error.Message}");
  }

  [Test]
  public async Task ValidateStep_UndecoratedFunction_SurfacesAsDecoratorAbsent()
  {
    var executor = CreateExecutor();
    var io = executor.ValidateStep("test_steps.undecorated", "plain_fn");
    var result = await io.Run();
    var error = AssertExtensionFailure<PythonStepMetadata>(result);
    Assert.That(error, Is.InstanceOf<PythonRuntimeError.DecoratorAbsent>(),
      $"Expected DecoratorAbsent; got {error.GetType().Name}: {error.Message}");
  }

  // ── InvokeInspector ─────────────────────────────────────────────────

  [Test]
  public async Task InvokeInspector_SuccessfulInspector_ReturnsValidatedPure()
  {
    var executor = CreateExecutor();
    var registration = new PythonServiceRegistration(
      ServiceClassPath: "test_services.ok_service.OkService",
      InspectorModule: "test_services.ok_service_inspector",
      InspectorFunction: "inspect"
    );

    var io = executor.InvokeInspector(registration);
    var result = await io.Run();
    AssertSuccess(result, out var validated);

    Assert.That(validated, Is.InstanceOf<Validated<PreFlightError, FlowUnit>.Valid>());
  }

  [Test]
  public async Task InvokeInspector_FailingInspector_SurfacesAsServiceInspectionFailed()
  {
    var executor = CreateExecutor();
    var registration = new PythonServiceRegistration(
      ServiceClassPath: "test_services.broken_service.BrokenService",
      InspectorModule: "test_services.broken_service_inspector",
      InspectorFunction: "inspect"
    );

    var io = executor.InvokeInspector(registration);
    var result = await io.Run();
    AssertSuccess(result, out var validated);

    Assert.That(validated, Is.InstanceOf<Validated<PreFlightError, FlowUnit>.Invalid>());
    var invalid = (Validated<PreFlightError, FlowUnit>.Invalid)validated;
    Assert.That(invalid.Errors[0], Is.InstanceOf<PreFlightError.External>());
    var external = (PreFlightError.External)invalid.Errors[0];
    Assert.That(external.Cause, Is.InstanceOf<PythonPreFlightError.ServiceInspectionFailed>());
  }

  // ── Error paths through Invoke ──────────────────────────────────────

  [Test]
  public async Task Invoke_PythonRaisesException_SurfacesAsWorkerError()
  {
    var executor = CreateExecutor();
    var io = executor.Invoke<int, int>("test_steps.raises", "boom", 0);
    var result = await io.Run();
    var error = AssertExtensionFailure<int>(result);

    Assert.That(error, Is.InstanceOf<PythonRuntimeError.WorkerError>(),
      $"Expected WorkerError; got {error.GetType().Name}: {error.Message}");
    var werr = (PythonRuntimeError.WorkerError)error;
    Assert.That(werr.PythonMessage, Does.Contain("ValueError").Or.Contain("intentional boom"),
      "WorkerError must propagate the Python-side traceback so the user can locate the failure.");
  }

  [Test]
  public async Task Invoke_CSharpMarshallingFailure_SurfacesNotSupportedWithDetail()
  {
    var executor = CreateExecutor();
    var rows = new[] { new UnsupportedDecimalRow { Id = 1, Amount = 9.99m } };

    // This blows up inside ArrowMarshaller.ToRecordBatch — BEFORE the worker
    // sees anything. The regression we're guarding here is the
    // TargetInvocationException unwrap path: the user must see
    // "NotSupportedException" + the offending property/type, not the generic
    // "Exception has been thrown by the target of an invocation." wrapper.
    var io = executor.Invoke<IEnumerable<UnsupportedDecimalRow>, IEnumerable<UnsupportedDecimalRow>>(
      "test_steps.tabular_identity", "identity", rows
    );
    var result = await io.Run();
    var failure = (EffResult<IEnumerable<UnsupportedDecimalRow>>.Failure)result;

    Assert.That(failure.Error.Message, Does.Not.Contain("Exception has been thrown by the target of an invocation"),
      "The MapError pipeline must unwrap reflection's TargetInvocationException so the real cause surfaces.");
    Assert.That(failure.Error.Message, Does.Contain("Amount").Or.Contain("Decimal"),
      "The offending property/type must be named in the user-facing message.");
  }

  // ── Helpers ─────────────────────────────────────────────────────────

  /// <summary>
  /// Runs <c>uv</c> with the given args in the given directory and captures
  /// stderr for diagnostic surfacing. Returns true on exit-code 0.
  /// </summary>
  private static bool RunUv(string args, string workingDirectory, out string stderr)
  {
    try
    {
      var psi = new ProcessStartInfo
      {
        FileName = "uv",
        Arguments = args,
        WorkingDirectory = workingDirectory,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true,
      };
      using var process = Process.Start(psi);
      if (process == null) { stderr = "Process.Start returned null"; return false; }
      stderr = process.StandardError.ReadToEnd();
      _ = process.StandardOutput.ReadToEnd();
      process.WaitForExit(120_000);
      return process.HasExited && process.ExitCode == 0;
    }
    catch (Exception ex)
    {
      stderr = ex.Message;
      return false;
    }
  }

  /// <summary>
  /// Writes the probe step + service modules into the temp project dir so
  /// the executor can import them through the configured ModuleSearchPaths.
  /// </summary>
  private static void WriteProbeModules(string root)
  {
    var stepsDir = Path.Combine(root, "test_steps");
    Directory.CreateDirectory(stepsDir);
    File.WriteAllText(Path.Combine(stepsDir, "__init__.py"), "");

    File.WriteAllText(Path.Combine(stepsDir, "scalar_echo.py"),
      """
      from flowthru import step

      @step(inputs=["int"], outputs=["str"])
      def echo(n: int) -> str:
          return f"answer: {n}"
      """);

    File.WriteAllText(Path.Combine(stepsDir, "tabular_increment.py"),
      """
      import pandas as pd
      from flowthru import step

      @step(inputs=["IntRow"], outputs=["IntRow"])
      def bump(df: pd.DataFrame) -> pd.DataFrame:
          out = df.copy()
          out["Id"] = out["Id"] + 1
          return out
      """);

    File.WriteAllText(Path.Combine(stepsDir, "tabular_count_and_pass.py"),
      """
      import pandas as pd
      from flowthru import step

      @step(inputs=["IntRow"], outputs=["int", "IntRow"])
      def count_and_pass(df: pd.DataFrame):
          return len(df), df
      """);

    File.WriteAllText(Path.Combine(stepsDir, "bytes_reverse.py"),
      """
      from flowthru import step

      @step(inputs=["bytes"], outputs=["bytes"])
      def rev(data: bytes) -> bytes:
          return bytes(reversed(data))
      """);

    File.WriteAllText(Path.Combine(stepsDir, "directory_passthrough.py"),
      """
      from flowthru import step

      @step(inputs=["IntRow"], outputs=["IntRow"])
      def passthrough(entries: dict) -> dict:
          # entries is dict[str, pd.DataFrame] — pass through verbatim.
          return entries
      """);

    File.WriteAllText(Path.Combine(stepsDir, "tabular_identity.py"),
      """
      import pandas as pd
      from flowthru import step

      @step(inputs=["Unsupported"], outputs=["Unsupported"])
      def identity(df: pd.DataFrame) -> pd.DataFrame:
          return df
      """);

    File.WriteAllText(Path.Combine(stepsDir, "decorated.py"),
      """
      from flowthru import step

      @step(
          inputs=["InputSchema"],
          outputs=["OutputSchema"],
          services=["Services.MyService"],
      )
      def transform(x):
          return x
      """);

    File.WriteAllText(Path.Combine(stepsDir, "undecorated.py"),
      """
      def plain_fn(x):
          return x
      """);

    File.WriteAllText(Path.Combine(stepsDir, "raises.py"),
      """
      from flowthru import step

      @step(inputs=["int"], outputs=["int"])
      def boom(n: int) -> int:
          raise ValueError("intentional boom for test coverage")
      """);

    // Service probes for InvokeInspector. Each "service" is a zero-arg class;
    // the matching "_inspector" module exports an `inspect(svc)` function
    // that returns flowthru.ValidationResult.
    var svcDir = Path.Combine(root, "test_services");
    Directory.CreateDirectory(svcDir);
    File.WriteAllText(Path.Combine(svcDir, "__init__.py"), "");

    File.WriteAllText(Path.Combine(svcDir, "ok_service.py"),
      """
      class OkService:
          def __init__(self):
              pass
      """);

    File.WriteAllText(Path.Combine(svcDir, "ok_service_inspector.py"),
      """
      from flowthru import ValidationResult

      def inspect(svc):
          return ValidationResult.success()
      """);

    File.WriteAllText(Path.Combine(svcDir, "broken_service.py"),
      """
      class BrokenService:
          def __init__(self):
              pass
      """);

    File.WriteAllText(Path.Combine(svcDir, "broken_service_inspector.py"),
      """
      from flowthru import ValidationResult, ValidationErrorType

      def inspect(svc):
          return ValidationResult.failure(
              source="BrokenService",
              error_type=ValidationErrorType.Misconfigured,
              message="probe says the service is unhealthy",
          )
      """);
  }

  /// <summary>
  /// Unwraps an <c>EffResult.Success</c> or fails the test with a readable
  /// description of the captured <see cref="RuntimeError"/>.
  /// </summary>
  private static void AssertSuccess<A>(EffResult<A> result, out A value)
  {
    if (result is EffResult<A>.Success s)
    {
      value = s.Value;
      return;
    }
    var failure = (EffResult<A>.Failure)result;
    Assert.Fail($"Expected Success, got Failure: {failure.Error.Message}");
    value = default!;
  }

  /// <summary>
  /// Asserts the result is a Failure carrying a
  /// <see cref="RuntimeError.ExtensionError"/> whose inner cause is a
  /// <see cref="PythonRuntimeError"/>, and returns the inner cause for
  /// further assertions.
  /// </summary>
  private static PythonRuntimeError AssertExtensionFailure<A>(EffResult<A> result)
  {
    var failure = result as EffResult<A>.Failure;
    Assert.That(failure, Is.Not.Null, $"Expected Failure; got {result.GetType().Name}");
    Assert.That(failure!.Error, Is.InstanceOf<RuntimeError.ExtensionError>(),
      $"Expected ExtensionError wrapper; got {failure.Error.GetType().Name}: {failure.Error.Message}");
    var ext = (RuntimeError.ExtensionError)failure.Error;
    Assert.That(ext.Cause, Is.InstanceOf<PythonRuntimeError>(),
      $"Expected PythonRuntimeError inner; got {ext.Cause.GetType().Name}");
    return (PythonRuntimeError)ext.Cause;
  }

  /// <summary>
  /// Stub flattener for tests that don't need IConfiguration → env-var
  /// bridging. Returns empty so the production
  /// <see cref="PythonConfigurationFlattener"/>'s IConfiguration dep
  /// doesn't have to be wired up here.
  /// </summary>
  private sealed class NullFlattener : IPythonConfigurationFlattener
  {
    private static readonly IReadOnlyDictionary<string, string> _empty =
      new Dictionary<string, string>(StringComparer.Ordinal);
    public IReadOnlyDictionary<string, string> Flatten() => _empty;
  }
}
