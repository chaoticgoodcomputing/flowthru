using System.Runtime.InteropServices;
using Flowthru.Extensions.Python.Runtime;

namespace Flowthru.Extensions.Python.Tests;

/// <summary>
/// Shared helper for Python test configuration.
/// Ensures all test classes use consistent PythonRuntimeOptions with complete sys.path.
/// </summary>
/// <remarks>
/// <para>
/// Python.NET initializes globally once per process. Whichever test class runs first
/// sets the sys.path for the entire test session. This helper ensures all tests configure
/// the same comprehensive set of paths, regardless of execution order.
/// </para>
/// <para>
/// All test classes should use CreateDefaultOptions() instead of <c>new PythonRuntimeOptions()</c>
/// to ensure consistent configuration.
/// </para>
/// <para>
/// <strong>Python environment strategy:</strong>
/// The test output directory contains <c>pyproject.toml</c> and <c>uv.lock</c> (copied during build).
/// <see cref="PythonRuntime.Initialize"/> automatically runs <c>uv sync --frozen</c> to materialize
/// <c>.venv/</c> in the output directory and adds site-packages to <c>sys.path</c>.
/// No manual venv discovery needed.
/// </para>
/// </remarks>
public static class PythonTestHelper
{
    /// <summary>
    /// Creates PythonRuntimeOptions configured for test execution.
    /// </summary>
    /// <remarks>
    /// Includes paths needed by test modules:
    /// <list type="bullet">
    /// <item>Test output directory (for _flowthru_arrow.py)</item>
    /// <item>_Fixtures subdirectory (for test Python modules)</item>
    /// </list>
    /// Site-packages from .venv is auto-discovered by <see cref="PythonRuntime.Initialize"/>.
    /// </remarks>
    public static PythonRuntimeOptions CreateDefaultOptions()
    {
        var options = new PythonRuntimeOptions();

        // Add test output directory (contains _flowthru_arrow.py)
        options.ModuleSearchPaths.Add(AppContext.BaseDirectory);

        // Add _Fixtures subdirectory (contains test modules: scalar_steps.py, tabular_steps.py, test_module.py)
        var fixturesPath = Path.Combine(AppContext.BaseDirectory, "_Fixtures");
        if (Directory.Exists(fixturesPath))
        {
            options.ModuleSearchPaths.Add(fixturesPath);
        }

        return options;
    }
}
