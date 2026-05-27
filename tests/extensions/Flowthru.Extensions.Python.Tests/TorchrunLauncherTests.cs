using Flowthru.Prelude;
using Flowthru.Step.Python;
using Flowthru.Validation.PreFlight;

namespace Flowthru.Extensions.Python.Tests;

/// <summary>
/// Tests for <see cref="TorchrunLauncher"/> — the PyTorch-native
/// distributed launcher. Probe tests use real filesystem
/// paths (existing /bin/sh vs a fabricated /nonexistent path) so the
/// subprocess machinery is exercised end-to-end without needing a
/// torch venv.
/// </summary>
[TestFixture]
[Category("Python")]
public class TorchrunLauncherTests
{
  private static readonly IReadOnlyDictionary<string, string> EmptyEnv =
    new Dictionary<string, string>();

  // ── Build ────────────────────────────────────────────────────────────

  [Test]
  public void Build_UsesTorchrunBinaryFromVenvByDefault()
  {
    var launcher = new TorchrunLauncher { NProcPerNode = 2 };
    var psi = launcher.Build("/opt/venv/bin/python", "/some/flowthru_worker.py", EmptyEnv);

    Assert.That(psi.FileName, Is.EqualTo("/opt/venv/bin/torchrun"));
    Assert.That(psi.ArgumentList, Does.Contain("--nproc_per_node=2"));
    Assert.That(psi.ArgumentList, Does.Contain("/some/flowthru_worker.py"));
  }

  [Test]
  public void Build_HonorsBinaryPathOverride()
  {
    var launcher = new TorchrunLauncher
    {
      NProcPerNode = 4,
      BinaryPath = "/opt/lab/bin/lab-torchrun",
    };
    var psi = launcher.Build("/opt/venv/bin/python", "/w.py", EmptyEnv);

    Assert.That(psi.FileName, Is.EqualTo("/opt/lab/bin/lab-torchrun"));
  }

  [Test]
  public void Build_OmitsRedirectsFlagWhenNProcPerNodeIsOne()
  {
    // Single-rank torchrun has no non-rank-0 ranks to redirect, so
    // the launcher skips --redirects entirely.
    var launcher = new TorchrunLauncher { NProcPerNode = 1 };
    var psi = launcher.Build("/p", "/w.py", EmptyEnv);

    Assert.That(psi.ArgumentList.Any(a => a.StartsWith("--redirects=")), Is.False);
  }

  [Test]
  public void Build_AutoComputesRedirectsForMultiRank()
  {
    // Slice 5: redirect ranks 1..N-1's stdout+stderr (bitmask 3) to
    // per-rank log files so they can't corrupt rank 0's protocol
    // stream on the parent stdout pipe.
    var launcher = new TorchrunLauncher { NProcPerNode = 3 };
    var psi = launcher.Build("/p", "/w.py", EmptyEnv);

    Assert.That(psi.ArgumentList, Does.Contain("--redirects=1:3,2:3"));
  }

  [Test]
  public void Build_HonorsExplicitRedirectsOverride()
  {
    var launcher = new TorchrunLauncher
    {
      NProcPerNode = 4,
      RedirectsFlag = "1:3,2:3,3:3",
    };
    var psi = launcher.Build("/p", "/w.py", EmptyEnv);

    Assert.That(psi.ArgumentList, Does.Contain("--redirects=1:3,2:3,3:3"));
  }

  [Test]
  public void Build_EmptyRedirectsFlagDisablesArg()
  {
    // Escape hatch: explicit empty string disables --redirects entirely.
    var launcher = new TorchrunLauncher
    {
      NProcPerNode = 4,
      RedirectsFlag = string.Empty,
    };
    var psi = launcher.Build("/p", "/w.py", EmptyEnv);

    Assert.That(psi.ArgumentList.Any(a => a.StartsWith("--redirects=")), Is.False);
  }

  [Test]
  public void Build_AppliesEnvVarsToPsi()
  {
    var launcher = new TorchrunLauncher { NProcPerNode = 1 };
    var env = new Dictionary<string, string>
    {
      ["FLOWTHRU__SOMETHING"] = "value",
    };
    var psi = launcher.Build("/p", "/w.py", env);

    Assert.That(psi.EnvironmentVariables["FLOWTHRU__SOMETHING"], Is.EqualTo("value"));
  }

  [Test]
  public void Build_RedirectsStdinStdoutStderr()
  {
    // Slice 4 still uses the stdio protocol — non-rank-0 stdout
    // interleaving is the documented caveat. But the PSI itself must
    // still redirect all three streams or even rank 0's protocol I/O
    // breaks.
    var launcher = new TorchrunLauncher { NProcPerNode = 1 };
    var psi = launcher.Build("/p", "/w.py", EmptyEnv);

    Assert.That(psi.RedirectStandardInput, Is.True);
    Assert.That(psi.RedirectStandardOutput, Is.True);
    Assert.That(psi.RedirectStandardError, Is.True);
    Assert.That(psi.UseShellExecute, Is.False);
  }

  // ── Identity ─────────────────────────────────────────────────────────

  [Test]
  public void Identity_DiffersAcrossNProcPerNode()
  {
    var one = new TorchrunLauncher { NProcPerNode = 1 }.Identity;
    var two = new TorchrunLauncher { NProcPerNode = 2 }.Identity;
    Assert.That(one, Is.Not.EqualTo(two));
  }

  // ── Requirements ─────────────────────────────────────────────────────

  [Test]
  public void Requirements_DeclareTorch()
  {
    var launcher = new TorchrunLauncher();
    Assert.That(launcher.Requirements.Any(r => r.Package == "torch"), Is.True);
  }

  // ── Probe ────────────────────────────────────────────────────────────

  [Test]
  public void Probe_FailsWhenBinaryMissing()
  {
    var launcher = new TorchrunLauncher
    {
      BinaryPath = "/definitely/not/a/real/path/torchrun",
    };
    var result = launcher.Probe();
    AssertInvalidWithDetail(result, "torchrun binary not found");
  }

  [Test]
  public void Probe_PassesWhenBinaryExists()
  {
    // /bin/sh is universally present on POSIX systems used in this
    // project's CI matrix (Linux, macOS). Skipping the test on
    // Windows where /bin/sh isn't a thing — TorchrunLauncher itself
    // works on Windows but exercising the probe there needs a
    // different stand-in.
    if (OperatingSystem.IsWindows()) Assert.Ignore("Probe exercise uses /bin/sh, not present on Windows.");

    var launcher = new TorchrunLauncher
    {
      NProcPerNode = 1,
      BinaryPath = "/bin/sh",
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
