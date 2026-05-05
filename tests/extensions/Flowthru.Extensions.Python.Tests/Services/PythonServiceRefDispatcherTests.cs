using Flowthru.Core.Data.Validation;
using Flowthru.Core.Effects;
using Flowthru.Extensions.Python.Execution;
using Flowthru.Extensions.Python.Runtime;
using Flowthru.Extensions.Python.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Flowthru.Extensions.Python.Tests.Services;

/// <summary>
/// Tests for <see cref="PythonServiceRefDispatcher"/> — the bridge between
/// the Core preflight loop and the Python extension's inspector registry.
/// Uses a fake <see cref="IPythonExecutor"/> to verify dispatch without
/// spawning a real Python subprocess.
/// </summary>
[TestFixture]
[Category("Python")]
[Category("Services")]
public class PythonServiceRefDispatcherTests
{
  // ── Variant matching ────────────────────────────────────────────────

  [Test]
  public void CanHandle_PythonRef_ReturnsTrue()
  {
    var dispatcher = BuildDispatcher(out _, out _);
    Assert.That(dispatcher.CanHandle(ServiceRef.OfPython("Services.X.Y")), Is.True);
  }

  [Test]
  public void CanHandle_CSharpRef_ReturnsFalse()
  {
    // CSharp refs are handled by the Core preflight loop directly, not
    // routed through dispatchers. The Python dispatcher must report it
    // doesn't handle them so the loop's "first match wins" iteration
    // doesn't attribute a Python lookup to a C# service.
    var dispatcher = BuildDispatcher(out _, out _);
    Assert.That(dispatcher.CanHandle(ServiceRef.Of<IDummyService>()), Is.False);
  }

  // ── Registered: forwards to executor ────────────────────────────────

  [Test]
  public async Task InspectAsync_RegisteredServicePath_DispatchesToExecutor()
  {
    var fakeExecutor = new RecordingExecutor(
      result: ValidationResult.Success()
    );

    var dispatcher = BuildDispatcher(
      out _,
      executor: fakeExecutor,
      configure: opts => opts.RegisterService(
        "Services.pyannote_diarizer.PyannoteDiarizer",
        svc => svc.WithInspector("Services.pyannote_diarizer_inspector")
      )
    );

    var result = await dispatcher.InspectAsync(
      ServiceRef.OfPython("Services.pyannote_diarizer.PyannoteDiarizer"),
      CancellationToken.None
    );

    Assert.Multiple(() =>
    {
      Assert.That(fakeExecutor.InvokeInspectorCallCount, Is.EqualTo(1));
      Assert.That(
        fakeExecutor.LastRegistration!.ServiceClassPath,
        Is.EqualTo("Services.pyannote_diarizer.PyannoteDiarizer")
      );
      Assert.That(
        fakeExecutor.LastRegistration.InspectorModule,
        Is.EqualTo("Services.pyannote_diarizer_inspector")
      );
      Assert.That(result.IsValid, Is.True);
    });
  }

  [Test]
  public async Task InspectAsync_RegisteredService_ReturnsExecutorResultUnchanged()
  {
    // The dispatcher must not transform success/failure results — the
    // executor (and the Python inspector behind it) is the canonical
    // source of truth for the inspection outcome. Failures with their
    // specific source/error_type/message must round-trip intact.
    var failure = ValidationResult.Failure(
      catalogKey: "PyannoteDiarizer",
      errorType: ValidationErrorType.InspectionFailure,
      message: "HuggingFace token missing",
      details: "PythonErrorType=Configuration"
    );
    var fakeExecutor = new RecordingExecutor(result: failure);

    var dispatcher = BuildDispatcher(
      out _,
      executor: fakeExecutor,
      configure: opts => opts.RegisterService(
        "Services.X.Y",
        svc => svc.WithInspector("inspector")
      )
    );

    var result = await dispatcher.InspectAsync(
      ServiceRef.OfPython("Services.X.Y"),
      CancellationToken.None
    );

    Assert.Multiple(() =>
    {
      Assert.That(result.IsValid, Is.False);
      Assert.That(result.Errors.Single().Message, Is.EqualTo("HuggingFace token missing"));
    });
  }

  // ── Unregistered: non-fatal ─────────────────────────────────────────

  [Test]
  public async Task InspectAsync_UnregisteredService_ReturnsSuccessWithoutCallingExecutor()
  {
    // Mirrors the C#-side behaviour from Flow.InspectStepServicesAsync
    // when no IFlowthruInspector<T> is registered: missing inspectors are
    // non-fatal warnings, the run continues. Otherwise, every non-Python
    // service in a flow that the user happens not to have wired would
    // halt preflight.
    var fakeExecutor = new RecordingExecutor(result: ValidationResult.Success());

    var dispatcher = BuildDispatcher(
      out _,
      executor: fakeExecutor,
      configure: _ => { /* no registrations */ }
    );

    var result = await dispatcher.InspectAsync(
      ServiceRef.OfPython("Services.NeverRegistered"),
      CancellationToken.None
    );

    Assert.Multiple(() =>
    {
      Assert.That(result.IsValid, Is.True);
      Assert.That(fakeExecutor.InvokeInspectorCallCount, Is.EqualTo(0));
    });
  }

  // ── Mismatched variant: fail loudly ─────────────────────────────────

  [Test]
  public void InspectAsync_CSharpRefAfterCanHandleViolation_Throws()
  {
    // CanHandle's contract gates this method; passing a non-Python ref is
    // a caller bug. Throwing keeps the contract honest rather than
    // silently returning Success and masking the bug.
    var dispatcher = BuildDispatcher(out _, out _);
    Assert.ThrowsAsync<ArgumentException>(async () =>
      await dispatcher.InspectAsync(
        ServiceRef.Of<IDummyService>(),
        CancellationToken.None
      )
    );
  }

  // ── Test doubles + helpers ──────────────────────────────────────────

  public interface IDummyService { }

  /// <summary>
  /// Fake <see cref="IPythonExecutor"/> that records what
  /// <see cref="InvokeInspector"/> was called with. The other interface
  /// methods are unused for these tests and throw to surface accidental
  /// invocations as test bugs.
  /// </summary>
  private sealed class RecordingExecutor : IPythonExecutor
  {
    private readonly ValidationResult _result;

    public int InvokeInspectorCallCount { get; private set; }
    public PythonServiceRegistration? LastRegistration { get; private set; }

    public RecordingExecutor(ValidationResult result) => _result = result;

    public ValidationResult InvokeInspector(PythonServiceRegistration registration)
    {
      InvokeInspectorCallCount++;
      LastRegistration = registration;
      return _result;
    }

    public TOutput Invoke<TInput, TOutput>(string moduleName, string functionName, TInput input) =>
      throw new InvalidOperationException(
        "Invoke should not be called by PythonServiceRefDispatcher tests."
      );

    public PythonStepMetadata ValidateStep(string moduleName, string functionName) =>
      throw new InvalidOperationException(
        "ValidateStep should not be called by PythonServiceRefDispatcher tests."
      );
  }

  private static PythonServiceRefDispatcher BuildDispatcher(
    out PythonServiceInspectorRegistry registry,
    out RecordingExecutor recordingExecutor
  )
  {
    var executor = new RecordingExecutor(ValidationResult.Success());
    var dispatcher = BuildDispatcher(out registry, executor, _ => { });
    recordingExecutor = executor;
    return dispatcher;
  }

  private static PythonServiceRefDispatcher BuildDispatcher(
    out PythonServiceInspectorRegistry registry,
    RecordingExecutor executor,
    Action<PythonRuntimeOptions> configure
  )
  {
    var options = new PythonRuntimeOptions();
    configure(options);
    registry = new PythonServiceInspectorRegistry(Options.Create(options));
    return new PythonServiceRefDispatcher(
      registry,
      executor,
      NullLogger<PythonServiceRefDispatcher>.Instance
    );
  }
}
