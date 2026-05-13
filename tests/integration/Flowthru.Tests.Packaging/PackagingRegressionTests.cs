using System.Diagnostics;
using System.Text;

namespace Flowthru.Tests.Packaging;

/// <summary>
/// Regression tests for the packaged Flowthru NuGet consumer path.
///
/// These tests do NOT reference Flowthru in-process. Each writes a small
/// consumer project to a hermetic temp directory, configures NuGet to
/// restore from the local dist/packages feed, and shells out to dotnet
/// build. The point is to exercise the exact code path an end user follows
/// when they `dotnet add package Flowthru` from a fresh project — the only
/// path that catches packaging regressions like "the source generator DLL
/// stopped shipping in 0.17.0".
///
/// Slow by design (~5–15s per test). Tagged [Category("Integration")] so the
/// fast `nx test` loop doesn't pay for it; runs under `tests:test:integration`.
/// </summary>
[TestFixture]
[Category("Integration")]
public class PackagingRegressionTests
{
  private static readonly string _workspaceRoot = FindWorkspaceRoot();
  private static readonly string _localFeed = Path.Combine(_workspaceRoot, "dist", "packages");

  /// <summary>
  /// The user's bug report (Flowthru 0.17.0–0.17.3): types annotated with
  /// `[FlowthruSchema]` failed to compile in downstream projects with CS0311
  /// because `Flowthru.Core.SourceGenerators.dll` was no longer shipped in
  /// the nupkg's `analyzers/dotnet/cs/` slot. This test reconstructs the
  /// exact minimal repro from the bug report and asserts it builds clean
  /// against the local feed.
  /// </summary>
  [Test]
  public async Task FlowthruSchema_OnDownstreamRecord_CompilesAgainstPackedMetaPackage()
  {
    using var sandbox = new ConsumerSandbox(TestContext.CurrentContext.Test);

    var version = sandbox.ResolveFlowthruVersionFromFeed();
    sandbox.WriteNuGetConfig();

    sandbox.WriteFile("Repro.csproj", $"""
      <Project Sdk="Microsoft.NET.Sdk">
        <PropertyGroup>
          <TargetFramework>net10.0</TargetFramework>
          <ImplicitUsings>enable</ImplicitUsings>
          <Nullable>enable</Nullable>
        </PropertyGroup>
        <ItemGroup>
          <PackageReference Include="Flowthru" Version="{version}" />
        </ItemGroup>
      </Project>
      """);

    sandbox.WriteFile("Program.cs", """
      using Flowthru.Data.Catalog;
      using Flowthru.Data.Schema;

      namespace Repro;

      [FlowthruSchema]
      public partial record AtlasPoint
      {
        [SerializedLabel("card_id")] public required Guid CardId { get; init; }
        [SerializedLabel("x")]       public required double X { get; init; }
        [SerializedLabel("y")]       public required double Y { get; init; }
      }

      public partial class Catalog : CatalogAbstract
      {
        private readonly string _basePath;
        public Catalog(string basePath) { _basePath = basePath; }

        public IItem<IEnumerable<AtlasPoint>> AtlasPoints =>
          CreateItem(() => Item.Of<IEnumerable<AtlasPoint>>("AtlasPoints")
            .Json()
            .AtPath($"{_basePath}/_03_Primary/Datasets/atlas-points.json")
            .Build());
      }

      public class Program { public static void Main() { } }
      """);

    var result = await sandbox.RunDotnetAsync("build");

    Assert.That(
      result.ExitCode,
      Is.EqualTo(0),
      $"`dotnet build` against packed Flowthru {version} failed.\n" +
      $"Sandbox preserved at: {sandbox.Dir}\n\n" +
      $"stdout:\n{result.StdOut}\n\nstderr:\n{result.StdErr}"
    );
  }

  private static string FindWorkspaceRoot()
  {
    var dir = TestContext.CurrentContext.TestDirectory;
    while (dir != null)
    {
      if (File.Exists(Path.Combine(dir, "nx.json"))) return dir;
      dir = Directory.GetParent(dir)?.FullName;
    }
    throw new InvalidOperationException("Workspace root (nx.json) not found");
  }

  /// <summary>
  /// One hermetic consumer build per test: scratch directory under
  /// <c>{tempPath}/flowthru-pkg-tests/{testName}-{guid}</c>, isolated
  /// DOTNET_CLI_HOME and NuGet globalPackagesFolder so the test never
  /// touches the developer's global state and parallel test runs can't
  /// collide. Preserved on failure for post-mortem; deleted on success.
  /// </summary>
  private sealed class ConsumerSandbox : IDisposable
  {
    public string Dir { get; }
    private readonly string _packagesDir;
    private readonly string _dotnetHome;
    private bool _success;

    public ConsumerSandbox(TestContext.TestAdapter test)
    {
      var slug = string.Concat(test.Name.Where(c => char.IsLetterOrDigit(c) || c == '_'))
        .Substring(0, Math.Min(40, test.Name.Length));
      Dir = Path.Combine(
        Path.GetTempPath(),
        "flowthru-pkg-tests",
        $"{slug}-{Guid.NewGuid():N}"[..Math.Min(64, slug.Length + 33)]
      );
      _packagesDir = Path.Combine(Dir, ".nuget-packages");
      _dotnetHome = Path.Combine(Dir, ".dotnet-home");
      Directory.CreateDirectory(Dir);
      Directory.CreateDirectory(_packagesDir);
      Directory.CreateDirectory(_dotnetHome);
    }

    public void WriteFile(string relativePath, string content)
    {
      var full = Path.Combine(Dir, relativePath);
      Directory.CreateDirectory(Path.GetDirectoryName(full)!);
      File.WriteAllText(full, content);
    }

    public void WriteNuGetConfig()
    {
      WriteFile("NuGet.Config", $"""
        <?xml version="1.0" encoding="utf-8"?>
        <configuration>
          <config>
            <add key="globalPackagesFolder" value="{_packagesDir}" />
          </config>
          <packageSources>
            <clear />
            <add key="local-flowthru" value="{_localFeed}" />
            <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
          </packageSources>
          <packageSourceMapping>
            <packageSource key="local-flowthru">
              <package pattern="Flowthru*" />
            </packageSource>
            <packageSource key="nuget.org">
              <package pattern="*" />
            </packageSource>
          </packageSourceMapping>
        </configuration>
        """);
    }

    /// <summary>
    /// Returns the version stamp of the <c>Flowthru</c> meta-package nupkg in
    /// the local feed. If none is present the suite has been misconfigured —
    /// project.json declares <c>dependsOn: ["build", "^pack"]</c>, so NX
    /// should have ensured an up-to-date pack before this test runs.
    /// </summary>
    public string ResolveFlowthruVersionFromFeed()
    {
      if (!Directory.Exists(_localFeed))
      {
        throw new InvalidOperationException(
          $"Local feed '{_localFeed}' does not exist. " +
          "Run `nx run flowthru:pack` (or `nx test flowthru.tests.packaging` " +
          "which depends on `^pack`) to populate it."
        );
      }
      var matches = Directory.GetFiles(_localFeed, "Flowthru.*.nupkg")
        .Select(Path.GetFileName)
        .Where(n => n != null && System.Text.RegularExpressions.Regex.IsMatch(
          n, @"^Flowthru\.\d+\.\d+\.\d+.*\.nupkg$"))
        .ToList();
      if (matches.Count == 0)
      {
        throw new InvalidOperationException(
          $"No Flowthru.<version>.nupkg found under {_localFeed}. " +
          "Run `nx run flowthru:pack` to populate it."
        );
      }
      // Filenames look like `Flowthru.0.17.2.nupkg` (or `Flowthru.0.17.2-preview.X.nupkg`).
      var first = matches[0]!;
      var stripped = first.Substring("Flowthru.".Length);
      stripped = stripped.Substring(0, stripped.Length - ".nupkg".Length);
      return stripped;
    }

    public async Task<DotnetResult> RunDotnetAsync(string args)
    {
      var psi = new ProcessStartInfo
      {
        FileName = "dotnet",
        Arguments = args,
        WorkingDirectory = Dir,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true,
      };
      psi.Environment["DOTNET_CLI_HOME"] = _dotnetHome;
      psi.Environment["DOTNET_NOLOGO"] = "1";
      psi.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";

      using var p = Process.Start(psi)
        ?? throw new InvalidOperationException("Failed to start dotnet");
      var stdOut = new StringBuilder();
      var stdErr = new StringBuilder();
      p.OutputDataReceived += (_, e) => { if (e.Data != null) stdOut.AppendLine(e.Data); };
      p.ErrorDataReceived  += (_, e) => { if (e.Data != null) stdErr.AppendLine(e.Data); };
      p.BeginOutputReadLine();
      p.BeginErrorReadLine();
      await p.WaitForExitAsync();

      var result = new DotnetResult(p.ExitCode, stdOut.ToString(), stdErr.ToString());
      if (result.ExitCode == 0) _success = true;
      return result;
    }

    public void Dispose()
    {
      // Preserve the sandbox on failure so the assertion message's path
      // resolves to something inspectable. Clean up on success.
      if (_success)
      {
        try { Directory.Delete(Dir, recursive: true); } catch { /* best-effort */ }
      }
    }
  }

  private readonly record struct DotnetResult(int ExitCode, string StdOut, string StdErr);
}
