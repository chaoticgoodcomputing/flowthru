using System.Diagnostics;
using System.Text;

namespace Flowthru.Tests.Templates.Infrastructure;

/// <summary>
/// Executes template-generated projects by spawning external dotnet processes.
/// </summary>
public sealed class TemplateTestRunner
{
  private readonly TemplateProject _project;

  /// <summary>
  /// Initializes a new instance of the <see cref="TemplateTestRunner"/> class.
  /// </summary>
  /// <param name="project">The template project configuration to test.</param>
  public TemplateTestRunner(TemplateProject project)
  {
    _project = project ?? throw new ArgumentNullException(nameof(project));
  }

  /// <summary>
  /// Finds the workspace root by walking up until nx.json is found.
  /// </summary>
  private static string GetWorkspaceRoot()
  {
    var dir = Directory.GetCurrentDirectory();
    while (dir != null)
    {
      if (File.Exists(Path.Combine(dir, "nx.json")))
      {
        return dir;
      }

      dir = Directory.GetParent(dir)?.FullName;
    }

    throw new InvalidOperationException("Could not find workspace root (nx.json not found)");
  }

  /// <summary>
  /// Writes a NuGet.Config into the generated project directory that pins
  /// Flowthru* packages to the local dist/packages feed, with nuget.org as
  /// fallback for everything else.
  /// </summary>
  private static void WriteLocalNuGetConfig(string projectDir)
  {
    var workspaceRoot = GetWorkspaceRoot();
    var localFeedPath = Path.Combine(workspaceRoot, "dist", "packages");

    // Place an isolated packages cache beside all generated projects so NuGet
    // never hits the global cache. Without this, a version already in
    // ~/.nuget/packages/ (e.g. from a prior NuGet.org publish) satisfies the
    // restore from cache before source mapping can redirect it to the local
    // dist/packages feed, causing the test to build against the stale package.
    var localPackagesFolder = Path.Combine(Path.GetDirectoryName(projectDir)!, ".nuget-packages");

    var nugetConfig = $"""
      <?xml version="1.0" encoding="utf-8"?>
      <configuration>
        <config>
          <add key="globalPackagesFolder" value="{localPackagesFolder}" />
        </config>
        <packageSources>
          <add key="local" value="{localFeedPath}" />
          <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
        </packageSources>
        <packageSourceMapping>
          <packageSource key="local">
            <package pattern="Flowthru*" />
          </packageSource>
          <packageSource key="nuget.org">
            <package pattern="*" />
          </packageSource>
        </packageSourceMapping>
      </configuration>
      """;

    File.WriteAllText(Path.Combine(projectDir, "NuGet.Config"), nugetConfig);
  }

  /// <summary>
  /// Generates the project from template, restores dependencies, and executes the pipeline.
  /// </summary>
  /// <returns>The result of the test run.</returns>
  public async Task<TemplateTestResult> GenerateAndRunAsync()
  {
    var stopwatch = Stopwatch.StartNew();
    Exception? exception = null;
    string? diagnosticMessage = null;
    int exitCode = -1;
    string? stdOut = null;
    string? stdErr = null;

    try
    {
      // Ensure the parent directory exists
      var parentDir = Path.GetDirectoryName(_project.GeneratedPath);
      if (string.IsNullOrEmpty(parentDir))
      {
        throw new InvalidOperationException($"Invalid generated path: {_project.GeneratedPath}");
      }

      if (!Directory.Exists(parentDir))
      {
        Directory.CreateDirectory(parentDir);
      }

      // Clean up any existing project directory
      if (Directory.Exists(_project.GeneratedPath))
      {
        Directory.Delete(_project.GeneratedPath, recursive: true);
      }

      // Step 1: Generate project from template
      var generateResult = await RunProcessAsync(
        "dotnet",
        $"new {_project.StarterName} --name {_project.ProjectName}",
        workingDirectory: parentDir
      );

      if (generateResult.exitCode != 0)
      {
        diagnosticMessage = "Failed to generate project from template";
        exitCode = generateResult.exitCode;
        stdOut = generateResult.stdOut;
        stdErr = generateResult.stdErr;
        return CreateResult(
          false,
          exitCode,
          stdOut,
          stdErr,
          stopwatch.Elapsed,
          null,
          diagnosticMessage
        );
      }

      // Inject NuGet.Config so Flowthru* resolves from local dist/packages feed
      WriteLocalNuGetConfig(_project.GeneratedPath);

      // Step 2: Restore dependencies
      var restoreResult = await RunProcessAsync(
        "dotnet",
        "restore",
        workingDirectory: _project.GeneratedPath
      );

      if (restoreResult.exitCode != 0)
      {
        diagnosticMessage = "Failed to restore NuGet packages";
        exitCode = restoreResult.exitCode;
        stdOut = restoreResult.stdOut;
        stdErr = restoreResult.stdErr;
        return CreateResult(
          false,
          exitCode,
          stdOut,
          stdErr,
          stopwatch.Elapsed,
          null,
          diagnosticMessage
        );
      }

      // Step 3: Build project
      var buildResult = await RunProcessAsync(
        "dotnet",
        "build --no-restore",
        workingDirectory: _project.GeneratedPath
      );

      if (buildResult.exitCode != 0)
      {
        diagnosticMessage = "Failed to build project";
        exitCode = buildResult.exitCode;
        stdOut = buildResult.stdOut;
        stdErr = buildResult.stdErr;
        return CreateResult(
          false,
          exitCode,
          stdOut,
          stdErr,
          stopwatch.Elapsed,
          null,
          diagnosticMessage
        );
      }

      // Step 4: Run pipeline (or all pipelines if PipelineName is null) in dry-run mode
      var runCommand =
        _project.PipelineName != null
          ? $"run --no-build -- {_project.PipelineName} --dry-run"
          : "run --no-build -- --dry-run";

      var runResult = await RunProcessAsync(
        "dotnet",
        runCommand,
        workingDirectory: _project.GeneratedPath
      );

      exitCode = runResult.exitCode;
      stdOut = runResult.stdOut;
      stdErr = runResult.stdErr;

      // Success is determined solely by exit code
      var success = exitCode == 0;

      return CreateResult(
        success,
        exitCode,
        stdOut,
        stdErr,
        stopwatch.Elapsed,
        null,
        diagnosticMessage
      );
    }
    catch (Exception ex)
    {
      exception = ex;
      diagnosticMessage = $"Exception during test execution: {ex.Message}";
      return CreateResult(
        false,
        exitCode,
        stdOut,
        stdErr,
        stopwatch.Elapsed,
        exception,
        diagnosticMessage
      );
    }
    finally
    {
      stopwatch.Stop();

      // Clean up generated project on success (keep on failure for debugging)
      try
      {
        if (exitCode == 0 && Directory.Exists(_project.GeneratedPath))
        {
          Directory.Delete(_project.GeneratedPath, recursive: true);
        }
      }
      catch
      {
        // Ignore cleanup errors
      }
    }
  }

  /// <summary>
  /// Runs a process and captures its output.
  /// </summary>
  private static async Task<(int exitCode, string stdOut, string stdErr)> RunProcessAsync(
    string fileName,
    string arguments,
    string workingDirectory
  )
  {
    var startInfo = new ProcessStartInfo
    {
      FileName = fileName,
      Arguments = arguments,
      WorkingDirectory = workingDirectory,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
      CreateNoWindow = true,
    };

    using var process = Process.Start(startInfo);
    if (process == null)
    {
      throw new InvalidOperationException($"Failed to start process: {fileName} {arguments}");
    }

    var stdOutBuilder = new StringBuilder();
    var stdErrBuilder = new StringBuilder();

    process.OutputDataReceived += (_, e) =>
    {
      if (e.Data != null)
      {
        stdOutBuilder.AppendLine(e.Data);
      }
    };

    process.ErrorDataReceived += (_, e) =>
    {
      if (e.Data != null)
      {
        stdErrBuilder.AppendLine(e.Data);
      }
    };

    process.BeginOutputReadLine();
    process.BeginErrorReadLine();

    await process.WaitForExitAsync();

    return (process.ExitCode, stdOutBuilder.ToString(), stdErrBuilder.ToString());
  }

  /// <summary>
  /// Creates a test result.
  /// </summary>
  private TemplateTestResult CreateResult(
    bool success,
    int exitCode,
    string? stdOut,
    string? stdErr,
    TimeSpan duration,
    Exception? exception,
    string? diagnosticMessage
  )
  {
    return new TemplateTestResult
    {
      Project = _project,
      Success = success,
      ExitCode = exitCode,
      StandardOutput = stdOut,
      StandardError = stdErr,
      Duration = duration,
      Exception = exception,
      DiagnosticMessage = diagnosticMessage,
    };
  }
}
