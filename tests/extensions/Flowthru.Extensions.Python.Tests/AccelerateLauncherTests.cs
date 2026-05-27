using Flowthru.Prelude;
using Flowthru.Step.Python;
using Flowthru.Validation.PreFlight;

namespace Flowthru.Extensions.Python.Tests;

/// <summary>
/// Tests for <see cref="AccelerateLauncher"/> — the HuggingFace
/// meta-launcher. Same probe strategy as
/// <see cref="TorchrunLauncherTests"/>: BinaryPath override against
/// real filesystem paths rather than mocking the subprocess machinery.
/// </summary>
[TestFixture]
[Category("Python")]
public class AccelerateLauncherTests
{
  private static readonly IReadOnlyDictionary<string, string> EmptyEnv =
    new Dictionary<string, string>();

  // ── Build ────────────────────────────────────────────────────────────

  [Test]
  public void Build_UsesAccelerateBinaryFromVenvByDefault()
  {
    var launcher = new AccelerateLauncher { NumProcesses = 2 };
    var psi = launcher.Build("/opt/venv/bin/python", "/some/flowthru_worker.py", EmptyEnv);

    Assert.That(psi.FileName, Is.EqualTo("/opt/venv/bin/accelerate"));
    Assert.That(psi.ArgumentList[0], Is.EqualTo("launch"),
      "accelerate's first arg is the subcommand 'launch'.");
    Assert.That(psi.ArgumentList, Does.Contain("--num_processes=2"));
    Assert.That(psi.ArgumentList, Does.Contain("/some/flowthru_worker.py"));
  }

  [Test]
  public void Build_HonorsBinaryPathOverride()
  {
    var launcher = new AccelerateLauncher
    {
      BinaryPath = "/opt/lab/bin/lab-accelerate",
    };
    var psi = launcher.Build("/opt/venv/bin/python", "/w.py", EmptyEnv);

    Assert.That(psi.FileName, Is.EqualTo("/opt/lab/bin/lab-accelerate"));
  }

  [Test]
  public void Build_OmitsNumProcessesWhenNull()
  {
    // Null NumProcesses → fall back to Accelerate's auto-detection.
    var launcher = new AccelerateLauncher();
    var psi = launcher.Build("/p", "/w.py", EmptyEnv);

    Assert.That(psi.ArgumentList.Any(a => a.StartsWith("--num_processes=")), Is.False);
  }

  [Test]
  public void Build_IncludesConfigFileWhenSet()
  {
    var launcher = new AccelerateLauncher
    {
      ConfigFile = "/etc/accelerate/config.yaml",
    };
    var psi = launcher.Build("/p", "/w.py", EmptyEnv);

    Assert.That(psi.ArgumentList, Does.Contain("--config_file=/etc/accelerate/config.yaml"));
  }

  [Test]
  public void Build_AppliesEnvVarsToPsi()
  {
    var launcher = new AccelerateLauncher();
    var env = new Dictionary<string, string>
    {
      ["FLOWTHRU__X"] = "v",
    };
    var psi = launcher.Build("/p", "/w.py", env);

    Assert.That(psi.EnvironmentVariables["FLOWTHRU__X"], Is.EqualTo("v"));
  }

  // ── Identity ─────────────────────────────────────────────────────────

  [Test]
  public void Identity_DiffersAcrossNumProcesses()
  {
    var auto = new AccelerateLauncher().Identity;
    var two = new AccelerateLauncher { NumProcesses = 2 }.Identity;
    var four = new AccelerateLauncher { NumProcesses = 4 }.Identity;
    Assert.That(auto, Is.Not.EqualTo(two));
    Assert.That(two, Is.Not.EqualTo(four));
  }

  [Test]
  public void Identity_DiffersAcrossConfigFile()
  {
    var a = new AccelerateLauncher { ConfigFile = "/path/a.yaml" }.Identity;
    var b = new AccelerateLauncher { ConfigFile = "/path/b.yaml" }.Identity;
    Assert.That(a, Is.Not.EqualTo(b));
  }

  // ── Requirements ─────────────────────────────────────────────────────

  [Test]
  public void Requirements_DeclareAccelerateWithFloorVersion()
  {
    var launcher = new AccelerateLauncher();
    var req = launcher.Requirements.Single();
    Assert.That(req.Package, Is.EqualTo("accelerate"));
    Assert.That(req.VersionConstraint, Is.EqualTo(">=0.30"));
  }

  [Test]
  public void Requirements_MirrorPythonPackageRequirementAttribute()
  {
    // Drift guard: the [PythonPackageRequirement(...)] on the class
    // (read by the FTPY1501 analyzer) and the runtime Requirements
    // property (read by the FTPY3011 pre-flight hook) must declare
    // the same package + constraint. Slice 3's analyzer would still
    // catch a partial misconfig, but failing here gives a clearer
    // message at unit-test time.
    var attrs = typeof(AccelerateLauncher)
      .GetCustomAttributes(typeof(PythonPackageRequirementAttribute), inherit: false)
      .Cast<PythonPackageRequirementAttribute>()
      .ToList();
    var runtime = new AccelerateLauncher().Requirements;

    Assert.That(attrs, Has.Count.EqualTo(runtime.Count),
      "Same number of declarations on attribute and runtime sides.");
    foreach (var attr in attrs)
    {
      var match = runtime.FirstOrDefault(r => r.Package == attr.Package);
      Assert.That(match, Is.Not.Null, $"Runtime list missing '{attr.Package}'.");
      Assert.That(match!.VersionConstraint, Is.EqualTo(attr.VersionConstraint));
    }
  }

  // ── Probe ────────────────────────────────────────────────────────────

  [Test]
  public void Probe_FailsWhenBinaryMissing()
  {
    var launcher = new AccelerateLauncher
    {
      BinaryPath = "/definitely/not/a/real/path/accelerate",
    };
    var result = launcher.Probe();
    AssertInvalidWithDetail(result, "accelerate binary not found");
  }

  [Test]
  public void Probe_FailsWhenConfigFileMissing()
  {
    if (OperatingSystem.IsWindows()) Assert.Ignore("Probe exercise uses /bin/sh, not present on Windows.");

    var launcher = new AccelerateLauncher
    {
      BinaryPath = "/bin/sh",  // pretend binary
      ConfigFile = "/definitely/not/a/real/path/config.yaml",
    };
    var result = launcher.Probe();
    AssertInvalidWithDetail(result, "config file does not exist");
  }

  [Test]
  public void Probe_PassesWhenBinaryAndConfigBothExist()
  {
    if (OperatingSystem.IsWindows()) Assert.Ignore("Probe exercise uses /bin/sh, not present on Windows.");

    // Use this test file itself as a stand-in for an existing config —
    // we only check existence, not contents.
    var thisFile = new System.Diagnostics.StackTrace(true).GetFrame(0)!.GetFileName()!;
    var launcher = new AccelerateLauncher
    {
      BinaryPath = "/bin/sh",
      ConfigFile = thisFile,
    };
    var result = launcher.Probe();
    Assert.That(result.IsValid, Is.True);
  }

  // ── Helpers ──────────────────────────────────────────────────────────

  private static void AssertInvalidWithDetail(
    Validated<PreFlightError, FlowUnit> result,
    string expectedSubstring
  )
  {
    Assert.That(result.IsValid, Is.False);
    if (result is Validated<PreFlightError, FlowUnit>.Invalid invalid)
    {
      var anyMatch = invalid.Errors
        .OfType<PreFlightError.External>()
        .Any(e => e.Cause.Message.Contains(expectedSubstring));
      Assert.That(anyMatch, Is.True,
        $"Expected at least one error containing '{expectedSubstring}', got: "
          + string.Join("; ", invalid.Errors.Select(e => e.Message)));
    }
    else
    {
      Assert.Fail("Expected Invalid result");
    }
  }
}
