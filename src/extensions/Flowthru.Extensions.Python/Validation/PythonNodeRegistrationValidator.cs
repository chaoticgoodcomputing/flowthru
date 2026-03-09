using Flowthru.Extensions.Python.Execution;
using Flowthru.Extensions.Python.Runtime;
using Python.Runtime;

namespace Flowthru.Extensions.Python.Validation;

/// <summary>
/// Performs basic validation checks during Python node registration.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Phase 4 registration-time validation:</strong>
/// Validates that the Python module, function, and @node decorator exist before
/// creating the node wrapper. This provides fast feedback during pipeline definition.
/// </para>
/// <para>
/// <strong>Checks performed:</strong>
/// <list type="bullet">
/// <item>Module is importable (exists, no syntax errors)</item>
/// <item>Function exists in module</item>
/// <item>Function has @node decorator with metadata</item>
/// </list>
/// </para>
/// <para>
/// <strong>More thorough validation occurs at pre-flight:</strong>
/// <list type="bullet">
/// <item>Decorator schemas match C# generic type parameters</item>
/// <item>Function signature arity is correct</item>
/// <item>Dry-run with 0-row DataFrame validates output structure</item>
/// </list>
/// </para>
/// </remarks>
internal static class PythonNodeRegistrationValidator
{
  /// <summary>
  /// Validates that a Python node can be registered.
  /// </summary>
  /// <param name="executor">Python executor for module/function inspection</param>
  /// <param name="runtime">Python runtime for GIL management</param>
  /// <param name="moduleName">Python module name (e.g., "pipelines.nodes.transform")</param>
  /// <param name="functionName">Python function name (e.g., "encode_species")</param>
  /// <exception cref="InvalidOperationException">
  /// Thrown if module, function, or decorator is missing or invalid
  /// </exception>
  public static void ValidateRegistration(
    PythonRuntime runtime,
    string moduleName,
    string functionName
  )
  {
    using (runtime.AcquireGil())
    {
      // Check 1: Module is importable
      PyObject module;
      try
      {
        using (Py.GIL())
        {
          module = Py.Import(moduleName);
        }
      }
      catch (PythonException ex)
      {
        throw new InvalidOperationException(
          $"Python node registration failed: Module '{moduleName}' not found in sys.path\n\n"
            + $"Error: {ex.Message}\n\n"
            + "Check:\n"
            + "  - Module name spelling\n"
            + "  - sys.path configuration in PythonRuntimeOptions\n"
            + "  - File exists and has no syntax errors",
          ex
        );
      }

      // Check 2: Function exists in module
      if (!module.HasAttr(functionName))
      {
        using (Py.GIL())
        {
          // Get available functions for helpful error message
          var dir = module.Dir();
          var availableFunctions = new List<string>();
          foreach (PyObject item in dir)
          {
            var name = item.ToString();
            if (name != null && !name.StartsWith("_"))
            {
              availableFunctions.Add(name);
            }
          }

          throw new InvalidOperationException(
            $"Python node registration failed: Function '{functionName}' not found in module '{moduleName}'\n\n"
              + $"Available functions: {string.Join(", ", availableFunctions)}\n\n"
              + "Check:\n"
              + "  - Function name spelling\n"
              + "  - Function is defined at module level (not inside a class)"
          );
        }
      }

      PyObject function = module.GetAttr(functionName);

      // Check 3: @node decorator is present
      if (!function.HasAttr("__flowthru_inputs__") || !function.HasAttr("__flowthru_outputs__"))
      {
        throw new InvalidOperationException(
          $"Python node registration failed: Function '{functionName}' in module '{moduleName}' "
            + "is missing required @node decorator.\n\n"
            + "Add decorator to declare schema contract:\n\n"
            + "  from flowthru import node\n"
            + "  from flowthru_schemas import InputSchema, OutputSchema\n"
            + "  \n"
            + $"  @node(inputs=[InputSchema], outputs=[OutputSchema])\n"
            + $"  def {functionName}(...):\n"
            + "      ...\n\n"
            + "The @node decorator is required for all Python nodes in Flowthru pipelines."
        );
      }

      // If we got here, basic registration requirements are met
      // More thorough validation happens during pre-flight
    }
  }
}
