using System.Runtime.CompilerServices;
using Flowthru.Extensions.Python.Runtime;

namespace Flowthru.Extensions.Python.Tests;

/// <summary>
/// Module initializer that sets PYTHONNET_PYDLL before Python.NET's static constructors run.
/// </summary>
/// <remarks>
/// Module initializers run before any other code in the assembly, including static constructors.
/// This ensures PYTHONNET_PYDLL is set before Python.NET attempts to load the Python shared library.
/// </remarks>
internal static class PythonTestModuleInitializer
{
  [ModuleInitializer]
  public static void Initialize()
  {
    var logPath = Path.Combine(Path.GetTempPath(), "flowthru-python-test-init.log");
    try
    {
      File.AppendAllText(logPath, $"[{DateTime.Now:O}] Module initializer started\n");
      File.AppendAllText(
        logPath,
        $"[{DateTime.Now:O}] Current directory: {Directory.GetCurrentDirectory()}\n"
      );

      var options = new PythonRuntimeOptions();
      var pythonDll = PythonEnvironmentResolver.ResolvePythonDll(options);

      File.AppendAllText(logPath, $"[{DateTime.Now:O}] Resolved Python DLL: {pythonDll}\n");
      File.AppendAllText(logPath, $"[{DateTime.Now:O}] DLL Exists: {File.Exists(pythonDll)}\n");

      Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", pythonDll);

      var envCheck = Environment.GetEnvironmentVariable("PYTHONNET_PYDLL");
      File.AppendAllText(logPath, $"[{DateTime.Now:O}] PYTHONNET_PYDLL set to: {envCheck}\n");

      Console.WriteLine($"[ModuleInitializer] Set PYTHONNET_PYDLL={pythonDll}");
    }
    catch (Exception ex)
    {
      File.AppendAllText(logPath, $"[{DateTime.Now:O}] Exception: {ex}\n");
      Console.WriteLine($"[ModuleInitializer] Failed to resolve Python DLL: {ex.Message}");
      // Don't throw here - let individual tests fail with clearer messages
    }
  }
}
