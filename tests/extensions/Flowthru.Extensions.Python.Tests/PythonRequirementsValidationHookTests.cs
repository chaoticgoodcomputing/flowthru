using System.Collections.Immutable;
using Flowthru.Data.Catalog;
using Flowthru.Flow;
using Flowthru.Prelude;
using Flowthru.Step.Python;
using Flowthru.Step.Python.Internal;
using Flowthru.Validation.PreFlight;
using Flowthru.Validation.PreFlight.Python;

namespace Flowthru.Extensions.Python.Tests;

/// <summary>
/// Tests for <see cref="PythonRequirementsValidationHook"/> — the
/// pre-flight enforcement of the Python requirements algebra. Uses
/// stub <see cref="IInstalledPackageProbe"/> + stub capabilities to
/// exercise the hook's logic without spawning a real Python
/// subprocess.
/// </summary>
[TestFixture]
[Category("Python")]
public class PythonRequirementsValidationHookTests
{
  [Test]
  public async Task NoRequirementsDeclared_PassesWithoutProbing()
  {
    var probe = new RecordingProbe(returnValue: null);
    var hook = NewHook(
      probe,
      capabilities: Array.Empty<IPythonCapability>(),
      launcher: new DirectPythonLauncher()
    );

    var result = await Validate(hook);

    Assert.That(result.IsValid, Is.True);
    Assert.That(probe.CallCount, Is.EqualTo(0),
      "Hook must not probe the venv when nothing was declared.");
  }

  [Test]
  public async Task AllRequirementsSatisfied_PassesValidation()
  {
    var probe = new RecordingProbe(returnValue: ImmutableDictionary.CreateRange(
      StringComparer.OrdinalIgnoreCase,
      new[]
      {
        KeyValuePair.Create("pyarrow", "15.0.0"),
        KeyValuePair.Create("accelerate", "0.31.0"),
      }
    ));
    var hook = NewHook(
      probe,
      capabilities: new[] { new StubCapability(("pyarrow", ">=14", "base")) },
      launcher: new StubLauncher(("accelerate", ">=0.30", "AccelerateLauncher"))
    );

    var result = await Validate(hook);

    Assert.That(result.IsValid, Is.True);
  }

  [Test]
  public async Task MissingPackage_EmitsFtpy3011()
  {
    var probe = new RecordingProbe(returnValue: ImmutableDictionary<string, string>.Empty);
    var hook = NewHook(
      probe,
      capabilities: new[] { new StubCapability(("accelerate", ">=0.30", "AccelerateLauncher")) },
      launcher: new DirectPythonLauncher()
    );

    var result = await Validate(hook);

    Assert.That(result.IsValid, Is.False);
    var errors = ExtractPythonErrors(result);
    Assert.That(errors, Has.Count.EqualTo(1));
    var missing = errors[0] as PythonPreFlightError.MissingRequirement;
    Assert.That(missing, Is.Not.Null);
    Assert.That(missing!.Package, Is.EqualTo("accelerate"));
    Assert.That(missing.DiagnosticCode, Is.EqualTo("FTPY3011"));
    Assert.That(missing.Declarers, Has.Count.EqualTo(1));
    Assert.That(missing.Declarers[0], Does.Contain("AccelerateLauncher"));
  }

  [Test]
  public async Task InstalledButWrongVersion_EmitsFtpy3012()
  {
    var probe = new RecordingProbe(returnValue: ImmutableDictionary.CreateRange(
      StringComparer.OrdinalIgnoreCase,
      new[] { KeyValuePair.Create("pyarrow", "13.0.0") }
    ));
    var hook = NewHook(
      probe,
      capabilities: new[] { new StubCapability(("pyarrow", ">=14", "base")) },
      launcher: new DirectPythonLauncher()
    );

    var result = await Validate(hook);

    Assert.That(result.IsValid, Is.False);
    var errors = ExtractPythonErrors(result);
    Assert.That(errors, Has.Count.EqualTo(1));
    var bad = errors[0] as PythonPreFlightError.VersionConstraintNotSatisfied;
    Assert.That(bad, Is.Not.Null);
    Assert.That(bad!.Package, Is.EqualTo("pyarrow"));
    Assert.That(bad.InstalledVersion, Is.EqualTo("13.0.0"));
    Assert.That(bad.DiagnosticCode, Is.EqualTo("FTPY3012"));
  }

  [Test]
  public async Task ConflictingDeclarers_FoldedConstraintNamesAllInDiagnostic()
  {
    // The hook doesn't symbolically detect "unsatisfiable" in slice 2;
    // it emits VersionConstraintNotSatisfied with both contributing
    // clauses in the constraint string and both declarers named.
    var probe = new RecordingProbe(returnValue: ImmutableDictionary.CreateRange(
      StringComparer.OrdinalIgnoreCase,
      new[] { KeyValuePair.Create("pyarrow", "14.5.0") }
    ));
    var hook = NewHook(
      probe,
      capabilities: new[]
      {
        new StubCapability(("pyarrow", ">=15", "BasePythonExtensionCapability")),
        new StubCapability(("pyarrow", "<14", "BadCapability")),
      },
      launcher: new DirectPythonLauncher()
    );

    var result = await Validate(hook);

    Assert.That(result.IsValid, Is.False);
    var errors = ExtractPythonErrors(result);
    Assert.That(errors, Has.Count.EqualTo(1));
    var bad = errors[0] as PythonPreFlightError.VersionConstraintNotSatisfied;
    Assert.That(bad, Is.Not.Null);
    Assert.That(bad!.Declarers, Has.Count.EqualTo(2));
    Assert.That(bad.RequiredConstraint, Does.Contain(">=15"));
    Assert.That(bad.RequiredConstraint, Does.Contain("<14"));
  }

  [Test]
  public async Task ProbeFails_EmitsSingleInspectionFailedRatherThanPerRequirementFanOut()
  {
    var probe = new RecordingProbe(returnValue: null);  // probe couldn't run
    var hook = NewHook(
      probe,
      capabilities: new[]
      {
        new StubCapability(
          ("pyarrow", ">=14", "base"),
          ("flowthru", null, "base"),
          ("accelerate", ">=0.30", "launcher")
        )
      },
      launcher: new DirectPythonLauncher()
    );

    var result = await Validate(hook);

    Assert.That(result.IsValid, Is.False);
    var errors = ExtractPythonErrors(result);
    Assert.That(errors, Has.Count.EqualTo(1),
      "Broken venv must surface as one inspection-failed error, not N per-requirement.");
    Assert.That(errors[0], Is.TypeOf<PythonPreFlightError.ServiceInspectionFailed>());
  }

  [Test]
  public async Task UnparseableInstalledVersion_DoesNotFailValidation()
  {
    // pip reports versions with post/dev/local tails our subset
    // doesn't model — lenient pass so we don't fabricate a failure
    // the user can't act on.
    var probe = new RecordingProbe(returnValue: ImmutableDictionary.CreateRange(
      StringComparer.OrdinalIgnoreCase,
      new[] { KeyValuePair.Create("pyarrow", "1!2.0+local") }
    ));
    var hook = NewHook(
      probe,
      capabilities: new[] { new StubCapability(("pyarrow", ">=14", "base")) },
      launcher: new DirectPythonLauncher()
    );

    var result = await Validate(hook);

    Assert.That(result.IsValid, Is.True);
  }

  // ── Helpers ─────────────────────────────────────────────────────────

  private static PythonRequirementsValidationHook NewHook(
    IInstalledPackageProbe probe,
    IEnumerable<IPythonCapability> capabilities,
    IPythonLauncher launcher
  ) => new(probe, capabilities, launcher);

  private static async Task<Validated<PreFlightError, FlowUnit>> Validate(
    PythonRequirementsValidationHook hook
  )
  {
    var flow = BuildEmptyFlow();
    var io = hook.Validate(flow);
    var effResult = await io.Run();
    if (effResult is EffResult<Validated<PreFlightError, FlowUnit>>.Success ok) return ok.Value;
    Assert.Fail("Hook FlowIO did not resolve to Success");
    return default!;
  }

  private static BuiltFlow BuildEmptyFlow() =>
    FlowBuilder.CreateFlow("requirements-test", b =>
    {
      var input = ItemFactory.Singleton.Memory<int>("input");
      var output = ItemFactory.Singleton.Memory<int>("output");
      b.AddStep<int, int>("plain", x => x + 1, input, output);
    });

  private static IReadOnlyList<PythonPreFlightError> ExtractPythonErrors(
    Validated<PreFlightError, FlowUnit> result
  ) => result switch
  {
    Validated<PreFlightError, FlowUnit>.Invalid invalid => invalid.Errors
      .OfType<PreFlightError.External>()
      .Select(ext => ext.Cause)
      .OfType<PythonPreFlightError>()
      .ToList(),
    _ => Array.Empty<PythonPreFlightError>(),
  };

  // ── Stubs ───────────────────────────────────────────────────────────

  private sealed class RecordingProbe : IInstalledPackageProbe
  {
    private readonly ImmutableDictionary<string, string>? _returnValue;
    public int CallCount { get; private set; }

    public RecordingProbe(ImmutableDictionary<string, string>? returnValue)
    {
      _returnValue = returnValue;
    }

    public ImmutableDictionary<string, string>? TryProbe()
    {
      CallCount++;
      return _returnValue;
    }
  }

  private sealed class StubCapability : IPythonCapability
  {
    public IReadOnlyList<PythonPackageRequirement> Requirements { get; }

    public StubCapability(params (string Package, string? Constraint, string Reason)[] reqs)
    {
      Requirements = reqs
        .Select(r => new PythonPackageRequirement(r.Package, r.Constraint, r.Reason))
        .ToList();
    }
  }

  private sealed class StubLauncher : IPythonLauncher
  {
    public IReadOnlyList<PythonPackageRequirement> Requirements { get; }

    public StubLauncher(params (string Package, string? Constraint, string Reason)[] reqs)
    {
      Requirements = reqs
        .Select(r => new PythonPackageRequirement(r.Package, r.Constraint, r.Reason))
        .ToList();
    }

    public System.Diagnostics.ProcessStartInfo Build(
      string pyExe,
      string workerScript,
      IReadOnlyDictionary<string, string> envVars
    ) => throw new NotSupportedException("StubLauncher does not Build.");
  }
}
