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

  private static readonly Lazy<bool> _hasGoogleSheetsCredentials = new(
    HasGoogleSheetsCredentials,
    LazyThreadSafetyMode.ExecutionAndPublication
  );

  /// <summary>
  /// A test spreadsheet id plus a usable Google credential, supplied via the
  /// environment. Required by any backend that drives a real
  /// <c>SheetsService</c> against Google. The probe is env-and-file only — it
  /// never builds a client or triggers an OAuth browser consent (that happens
  /// in the backend's setup, after this gate clears). It is satisfied when
  /// <c>FLOWTHRU_SHEETS_TEST_SPREADSHEET_ID</c> is set <em>and</em> exactly one
  /// of the two credential paths resolves to an existing file:
  /// <list type="bullet">
  ///   <item><c>FLOWTHRU_SHEETS_SA_KEY</c> — a service-account JSON key.</item>
  ///   <item><c>FLOWTHRU_SHEETS_OAUTH_CLIENT_SECRET</c> — an OAuth desktop
  ///     client secret.</item>
  /// </list>
  /// The credential <em>type</em> (SA vs OAuth) is auto-detected by the
  /// consuming backend; the same law suite runs under either.
  /// </summary>
  public static TestCapability GoogleSheetsCredentials { get; } = new(
    Name: "google-sheets-credentials",
    IsAvailable: () => _hasGoogleSheetsCredentials.Value,
    MissingMessage:
      "A test spreadsheet id and a Google credential are required for the live " +
      "Sheets backend. Set FLOWTHRU_SHEETS_TEST_SPREADSHEET_ID plus one of " +
      "FLOWTHRU_SHEETS_SA_KEY (service-account JSON path) or " +
      "FLOWTHRU_SHEETS_OAUTH_CLIENT_SECRET (OAuth desktop client-secret path). " +
      "See tests/extensions/Flowthru.Extensions.Google.Sheets.Tests/README.md."
  );

  /// <summary>
  /// Env-and-file-only probe for <see cref="GoogleSheetsCredentials"/>. A
  /// spreadsheet id must be present, and a credential file path (SA preferred
  /// when both are set) must point at an existing file. Never opens a stream,
  /// builds a client, or consents — keeping the gate cheap and browser-free.
  /// </summary>
  private static bool HasGoogleSheetsCredentials()
  {
    var spreadsheetId = Environment.GetEnvironmentVariable("FLOWTHRU_SHEETS_TEST_SPREADSHEET_ID");
    if (string.IsNullOrWhiteSpace(spreadsheetId)) return false;

    var saKey = Environment.GetEnvironmentVariable("FLOWTHRU_SHEETS_SA_KEY");
    if (!string.IsNullOrWhiteSpace(saKey) && File.Exists(saKey)) return true;

    var oauthSecret = Environment.GetEnvironmentVariable("FLOWTHRU_SHEETS_OAUTH_CLIENT_SECRET");
    if (!string.IsNullOrWhiteSpace(oauthSecret) && File.Exists(oauthSecret)) return true;

    return false;
  }

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
