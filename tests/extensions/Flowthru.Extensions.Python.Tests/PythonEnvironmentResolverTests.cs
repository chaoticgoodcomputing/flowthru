using Flowthru.Step.Python;
using Flowthru.Step.Python.Internal;

namespace Flowthru.Extensions.Python.Tests;

/// <summary>
/// Pins the venv-resolution priority chain inside
/// <see cref="PythonEnvironmentResolver"/>. The resolver is purely
/// filesystem + environment driven, so all branches can be exercised
/// deterministically by scaffolding temp directories — no Python
/// subprocess is spawned by these tests.
/// </summary>
/// <remarks>
/// <para>
/// The "uv sync actually executes" branch of
/// <see cref="PythonEnvironmentResolver.EnsureVenvViaUv(string, string)"/>
/// is deliberately not covered here: it shells out to <c>uv</c> on PATH
/// and would make CI depend on a Python toolchain. Every other branch —
/// including the early-return path where <c>pyvenv.cfg</c> already
/// exists — is reachable without spawning <c>uv</c>.
/// </para>
/// </remarks>
[TestFixture]
[Category("Python")]
public class PythonEnvironmentResolverTests
{
  private string _tempRoot = null!;
  private string? _originalVirtualEnv;

  [SetUp]
  public void SetUp()
  {
    _tempRoot = Path.Combine(Path.GetTempPath(), "flowthru-pyenv-" + Path.GetRandomFileName());
    Directory.CreateDirectory(_tempRoot);

    // Snapshot VIRTUAL_ENV so the env-var fallback tests can mutate it
    // without polluting sibling tests in this fixture or the broader run.
    _originalVirtualEnv = Environment.GetEnvironmentVariable("VIRTUAL_ENV");
    Environment.SetEnvironmentVariable("VIRTUAL_ENV", null);
  }

  [TearDown]
  public void TearDown()
  {
    Environment.SetEnvironmentVariable("VIRTUAL_ENV", _originalVirtualEnv);

    if (Directory.Exists(_tempRoot))
    {
      try
      {
        Directory.Delete(_tempRoot, recursive: true);
      }
      catch
      {
        // Best-effort cleanup — temp files are OS-reclaimed eventually.
      }
    }
  }

  // ── Helpers ───────────────────────────────────────────────────────────

  /// <summary>
  /// Mirrors <c>PythonEnvironmentResolver.FindPythonExeInVenv</c>'s platform
  /// branching so the test scaffolding lays down a python exe at exactly the
  /// path the resolver will probe.
  /// </summary>
  private static string PlatformPythonExePath(string venvDir) =>
    OperatingSystem.IsWindows()
      ? Path.Combine(venvDir, "Scripts", "python.exe")
      : Path.Combine(venvDir, "bin", "python");

  private static string PlatformDefaultPythonName() =>
    OperatingSystem.IsWindows() ? "python" : "python3";

  /// <summary>
  /// Scaffolds <paramref name="venvDir"/> with a stub python exe at the
  /// platform-appropriate sub-path. Content is irrelevant — the resolver
  /// only checks <see cref="File.Exists(string)"/>.
  /// </summary>
  private static string MakeVenvWithPythonExe(string venvDir)
  {
    var exePath = PlatformPythonExePath(venvDir);
    Directory.CreateDirectory(Path.GetDirectoryName(exePath)!);
    File.WriteAllText(exePath, string.Empty);
    return exePath;
  }

  // ── ResolvePythonExe: VenvPath branch ─────────────────────────────────

  [Test]
  public void ResolvePythonExe_VenvPathWithPythonExe_ReturnsThatExe()
  {
    var venvDir = Path.Combine(_tempRoot, "venv-with-exe");
    Directory.CreateDirectory(venvDir);
    var expected = MakeVenvWithPythonExe(venvDir);

    var opts = new PythonRuntimeOptions { VenvPath = venvDir };
    var result = PythonEnvironmentResolver.ResolvePythonExe(opts);

    Assert.That(result, Is.EqualTo(expected));
  }

  [Test]
  public void ResolvePythonExe_VenvPathEmptyDir_NoUvFiles_FallsThroughChain()
  {
    // Empty venv dir, no pyproject.toml / uv.lock alongside, no VIRTUAL_ENV
    // → resolver must fall all the way through to the platform default.
    var emptyVenv = Path.Combine(_tempRoot, "empty-venv");
    Directory.CreateDirectory(emptyVenv);

    var opts = new PythonRuntimeOptions { VenvPath = emptyVenv };
    var result = PythonEnvironmentResolver.ResolvePythonExe(opts);

    // The resolver MAY find a venv adjacent to AppContext.BaseDirectory
    // (the test host's bin/) if a stray pyproject.toml + uv.lock + .venv
    // happens to live there. Accept either: the platform default OR an
    // absolute path that points at a real python exe.
    if (Path.IsPathRooted(result))
    {
      Assert.That(File.Exists(result), Is.True,
        "Resolver returned a rooted path; that path should refer to a real exe.");
    }
    else
    {
      Assert.That(result, Is.EqualTo(PlatformDefaultPythonName()));
    }
  }

  [Test]
  public void ResolvePythonExe_VenvPathNonexistentDir_FallsThroughChain()
  {
    var nonexistent = Path.Combine(_tempRoot, "does-not-exist");
    // Intentionally do not create it.

    var opts = new PythonRuntimeOptions { VenvPath = nonexistent };
    var result = PythonEnvironmentResolver.ResolvePythonExe(opts);

    if (Path.IsPathRooted(result))
    {
      Assert.That(File.Exists(result), Is.True);
    }
    else
    {
      Assert.That(result, Is.EqualTo(PlatformDefaultPythonName()));
    }
  }

  // ── ResolvePythonExe: VIRTUAL_ENV fallback ───────────────────────────

  [Test]
  public void ResolvePythonExe_NullVenvPath_VirtualEnvSet_ReturnsExeFromVirtualEnv()
  {
    var virtualEnvDir = Path.Combine(_tempRoot, "venv-from-envvar");
    Directory.CreateDirectory(virtualEnvDir);
    var expected = MakeVenvWithPythonExe(virtualEnvDir);

    Environment.SetEnvironmentVariable("VIRTUAL_ENV", virtualEnvDir);

    var opts = new PythonRuntimeOptions { VenvPath = null };
    var result = PythonEnvironmentResolver.ResolvePythonExe(opts);

    // The VIRTUAL_ENV branch is consulted only AFTER the AppContext.BaseDirectory
    // uv-sync probe. If the test host's base dir doesn't carry pyproject.toml +
    // uv.lock + .venv (the normal case for a unit-test bin/) the VIRTUAL_ENV
    // branch wins. Assert either the env-var path won OR — if a base-dir venv
    // shadows it — a rooted real path was returned.
    if (result == expected)
    {
      Assert.Pass();
    }
    else
    {
      Assert.That(Path.IsPathRooted(result), Is.True,
        "Result should be either the VIRTUAL_ENV exe or a base-dir-resolved real exe.");
      Assert.That(File.Exists(result), Is.True);
    }
  }

  [Test]
  public void ResolvePythonExe_NullVenvPath_VirtualEnvEmpty_DoesNotConsultEnvVar()
  {
    // Empty/missing VIRTUAL_ENV must not be probed — set to "" explicitly.
    Environment.SetEnvironmentVariable("VIRTUAL_ENV", string.Empty);

    var opts = new PythonRuntimeOptions { VenvPath = null };
    var result = PythonEnvironmentResolver.ResolvePythonExe(opts);

    if (Path.IsPathRooted(result))
    {
      Assert.That(File.Exists(result), Is.True);
    }
    else
    {
      Assert.That(result, Is.EqualTo(PlatformDefaultPythonName()));
    }
  }

  // ── ResolvePythonExe: final fallback ─────────────────────────────────

  [Test]
  public void ResolvePythonExe_AllMissing_ReturnsPlatformDefault()
  {
    // VIRTUAL_ENV is already cleared by SetUp.
    var opts = new PythonRuntimeOptions { VenvPath = null };
    var result = PythonEnvironmentResolver.ResolvePythonExe(opts);

    // Caveat: the resolver also probes AppContext.BaseDirectory for a
    // uv-managed venv. The test bin/ dir typically has none, so we expect
    // the platform default. If a CI host stages a real .venv next to the
    // test binaries, accept a rooted real path instead.
    if (Path.IsPathRooted(result))
    {
      Assert.That(File.Exists(result), Is.True,
        "If a rooted path is returned, it must point at a real exe.");
    }
    else
    {
      Assert.That(result, Is.EqualTo(PlatformDefaultPythonName()));
    }
  }

  // ── ResolveModuleSearchPaths ─────────────────────────────────────────

  [Test]
  public void ResolveModuleSearchPaths_NonEmptyOptions_ReturnsThatList()
  {
    var opts = new PythonRuntimeOptions
    {
      ModuleSearchPaths = { "/a", "/b" },
    };

    var result = PythonEnvironmentResolver.ResolveModuleSearchPaths(opts);

    Assert.That(result, Is.EqualTo(new[] { "/a", "/b" }));
  }

  [Test]
  public void ResolveModuleSearchPaths_EmptyOptions_ReturnsBaseDirectorySingleton()
  {
    var opts = new PythonRuntimeOptions();
    var result = PythonEnvironmentResolver.ResolveModuleSearchPaths(opts);

    Assert.That(result, Has.Count.EqualTo(1));
    Assert.That(result[0], Is.EqualTo(AppContext.BaseDirectory));
  }

  [Test]
  public void ResolveModuleSearchPaths_ReturnsSameInstanceWhenPopulated()
  {
    // Confirms the resolver doesn't defensively copy — callers see the
    // exact list the options bag owns. This is a behavioural pin: if a
    // future refactor introduces a copy, the assertion will catch it
    // and the change can be reviewed deliberately.
    var opts = new PythonRuntimeOptions
    {
      ModuleSearchPaths = { "/only" },
    };

    var result = PythonEnvironmentResolver.ResolveModuleSearchPaths(opts);

    Assert.That(result, Is.SameAs(opts.ModuleSearchPaths));
  }

  // ── EnsureVenvViaUv: deterministic branches ──────────────────────────

  [Test]
  public void EnsureVenvViaUv_NoPyprojectNoLock_ReturnsNull()
  {
    var dir = Path.Combine(_tempRoot, "no-uv-files");
    Directory.CreateDirectory(dir);

    var result = PythonEnvironmentResolver.EnsureVenvViaUv(dir);

    Assert.That(result, Is.Null);
  }

  [Test]
  public void EnsureVenvViaUv_PyprojectOnly_NoLock_ReturnsNull()
  {
    var dir = Path.Combine(_tempRoot, "pyproject-only");
    Directory.CreateDirectory(dir);
    File.WriteAllText(Path.Combine(dir, "pyproject.toml"), string.Empty);

    var result = PythonEnvironmentResolver.EnsureVenvViaUv(dir);

    Assert.That(result, Is.Null);
  }

  [Test]
  public void EnsureVenvViaUv_LockOnly_NoPyproject_ReturnsNull()
  {
    var dir = Path.Combine(_tempRoot, "lock-only");
    Directory.CreateDirectory(dir);
    File.WriteAllText(Path.Combine(dir, "uv.lock"), string.Empty);

    var result = PythonEnvironmentResolver.EnsureVenvViaUv(dir);

    Assert.That(result, Is.Null);
  }

  [Test]
  public void EnsureVenvViaUv_PyprojectAndLock_PreExistingPyvenvCfg_ReturnsVenvPathWithoutSpawningUv()
  {
    var dir = Path.Combine(_tempRoot, "ready-venv");
    Directory.CreateDirectory(dir);
    File.WriteAllText(Path.Combine(dir, "pyproject.toml"), string.Empty);
    File.WriteAllText(Path.Combine(dir, "uv.lock"), string.Empty);

    var venvPath = Path.Combine(dir, ".venv");
    Directory.CreateDirectory(venvPath);
    File.WriteAllText(Path.Combine(venvPath, "pyvenv.cfg"), string.Empty);

    // Sentinel value for uvPath — if the resolver tried to actually spawn
    // it, ProcessStartInfo would attempt to launch this nonexistent binary.
    // The "pyvenv.cfg already exists" short-circuit MUST return before
    // hitting that path; otherwise this test would surface an exception
    // bubbling out, or at minimum a different result depending on the OS.
    const string nonExistentUv = "/definitely/not/a/real/uv/binary";
    var result = PythonEnvironmentResolver.EnsureVenvViaUv(dir, nonExistentUv);

    Assert.That(result, Is.EqualTo(venvPath));
  }

  [Test]
  public void EnsureVenvViaUv_ReadyVenv_FeedsResolvePythonExe()
  {
    // End-to-end pin: ResolvePythonExe should consume the venv that
    // EnsureVenvViaUv discovers via the existing-pyvenv.cfg short-circuit
    // when VenvPath points at the project dir (not the venv itself).
    var projectDir = Path.Combine(_tempRoot, "project-with-ready-venv");
    Directory.CreateDirectory(projectDir);
    File.WriteAllText(Path.Combine(projectDir, "pyproject.toml"), string.Empty);
    File.WriteAllText(Path.Combine(projectDir, "uv.lock"), string.Empty);

    var venvPath = Path.Combine(projectDir, ".venv");
    Directory.CreateDirectory(venvPath);
    File.WriteAllText(Path.Combine(venvPath, "pyvenv.cfg"), string.Empty);
    var expectedExe = MakeVenvWithPythonExe(venvPath);

    var opts = new PythonRuntimeOptions
    {
      VenvPath = projectDir,
      UvPath = "/definitely/not/a/real/uv/binary",
    };
    var result = PythonEnvironmentResolver.ResolvePythonExe(opts);

    Assert.That(result, Is.EqualTo(expectedExe));
  }

  // ── EnsureVenvViaUv: actually-shell-out branch ───────────────────────

  [Test]
  [Explicit("Spawns uv sync — requires uv on PATH and network. Run manually.")]
  public void EnsureVenvViaUv_PyprojectAndLock_NoVenv_RunsUvSync()
  {
    // Scaffold a minimal valid uv project. This test is explicit because
    // it requires (a) uv on PATH, (b) network access for the python
    // download, and (c) a real wall-clock budget. Kept here as a manual
    // smoke check rather than skipped silently.
    var dir = Path.Combine(_tempRoot, "real-uv-sync");
    Directory.CreateDirectory(dir);
    File.WriteAllText(Path.Combine(dir, "pyproject.toml"),
      "[project]\nname = \"flowthru-resolver-test\"\nversion = \"0.0.0\"\nrequires-python = \">=3.10\"\n");
    File.WriteAllText(Path.Combine(dir, "uv.lock"), string.Empty);

    var result = PythonEnvironmentResolver.EnsureVenvViaUv(dir);

    // Either uv was available and a venv was produced, OR uv was absent /
    // the lock was malformed and the resolver swallowed the failure to
    // return null. Both are documented contractual outcomes; this test
    // exists for human inspection, not gated assertions.
    Assert.That(result, Is.Null.Or.EqualTo(Path.Combine(dir, ".venv")));
  }
}
