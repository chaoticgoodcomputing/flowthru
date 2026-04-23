using System.Diagnostics;

namespace Flowthru.Tests.Templates.Infrastructure;

/// <summary>
/// Handles installation and uninstallation of Flowthru templates for testing.
/// </summary>
public static class TemplateInstaller
{
  private static readonly object _lock = new();
  private static string? _installedPath = null;

  /// <summary>
  /// Installs the Flowthru template from the specified path.
  /// </summary>
  /// <param name="templatePath">Path to the template directory (examples/starter/).</param>
  /// <exception cref="InvalidOperationException">Thrown if installation fails.</exception>
  public static void Install(string templatePath)
  {
    lock (_lock)
    {
      if (_installedPath == templatePath)
      {
        // Already installed
        return;
      }

      if (_installedPath != null)
      {
        throw new InvalidOperationException(
          $"Template already installed from different path: {_installedPath}"
        );
      }

      var result = RunDotnetCommand($"new install {templatePath}");

      // Exit code 106 means already installed - treat as success
      if (result.exitCode != 0 && result.exitCode != 106)
      {
        throw new InvalidOperationException(
          $"Failed to install template from {templatePath}. Exit code: {result.exitCode}\n{result.output}"
        );
      }

      _installedPath = templatePath;
    }
  }

  /// <summary>
  /// Uninstalls the Flowthru template.
  /// </summary>
  /// <param name="templatePath">Path to the template directory that was installed.</param>
  public static void Uninstall(string templatePath)
  {
    lock (_lock)
    {
      if (_installedPath != templatePath)
      {
        // Not installed or different path
        return;
      }

      var result = RunDotnetCommand($"new uninstall {templatePath}");

      // Don't throw on uninstall failure - best effort cleanup
      if (result.exitCode == 0)
      {
        _installedPath = null;
      }
    }
  }

  /// <summary>
  /// Runs a dotnet CLI command and captures output.
  /// </summary>
  private static (int exitCode, string output) RunDotnetCommand(string arguments)
  {
    var startInfo = new ProcessStartInfo
    {
      FileName = "dotnet",
      Arguments = arguments,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
      CreateNoWindow = true,
    };

    using var process = Process.Start(startInfo);
    if (process == null)
    {
      throw new InvalidOperationException("Failed to start dotnet process");
    }

    var output = process.StandardOutput.ReadToEnd();
    var error = process.StandardError.ReadToEnd();
    process.WaitForExit();

    var combinedOutput = string.IsNullOrEmpty(error) ? output : $"{output}\n{error}";
    return (process.ExitCode, combinedOutput);
  }
}
