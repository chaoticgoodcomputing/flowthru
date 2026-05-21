using System.Diagnostics;

namespace Flowthru.Tests.Kits.Prelude;

/// <summary>
/// Named singletons for the external dependencies Flowthru tests can opt
/// into. Backends declare requirements via
/// <see cref="IResourceBackend{TScope}.RequiredCapabilities"/>; the laws
/// kit checks them in <c>OneTimeSetUp</c> and yields <em>Inconclusive</em>
/// when a dependency is absent.
/// </summary>
/// <remarks>
/// <para>
/// New capabilities are added when a real consumer needs them. There are
/// deliberately no placeholders here — an unused capability is YAGNI.
/// </para>
/// <para>
/// Each capability's <c>IsAvailable</c> probe is wrapped in a
/// <see cref="Lazy{T}"/> so it runs at most once per test process.
/// Probes are independent of the bash post-install dependency checks
/// (<c>scripts/post-install/dependencies/</c>); the two layers serve
/// different audiences — install-time vs. test-time — and a developer
/// may add a dependency between the two.
/// </para>
/// </remarks>
public static class TestCapabilities
{
  private static readonly Lazy<bool> _hasDocker = new(
    () => CommandExists("docker"),
    LazyThreadSafetyMode.ExecutionAndPublication
  );

  /// <summary>
  /// Docker CLI on <c>PATH</c>. Required by any backend that uses
  /// <c>Testcontainers</c> to spin up a real database, message broker,
  /// or service for integration coverage.
  /// </summary>
  public static TestCapability Docker { get; } = new(
    Name: "docker",
    IsAvailable: () => _hasDocker.Value,
    MissingMessage:
      "Docker is required for this backend. " +
      "Install: https://docs.docker.com/get-docker/"
  );

  /// <summary>
  /// Probes whether <paramref name="command"/> resolves on the current
  /// <c>PATH</c> by running <c>which</c> (POSIX) or <c>where</c>
  /// (Windows). Returns <c>false</c> on any failure — the caller's
  /// <see cref="TestCapability"/> wrapper turns that into an
  /// Inconclusive verdict with an install hint.
  /// </summary>
  private static bool CommandExists(string command)
  {
    try
    {
      var isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
      var psi = new ProcessStartInfo
      {
        FileName = isWindows ? "where" : "which",
        Arguments = command,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true,
      };
      using var proc = Process.Start(psi);
      if (proc is null) return false;
      proc.WaitForExit(2_000);
      return proc.ExitCode == 0;
    }
    catch
    {
      return false;
    }
  }
}
