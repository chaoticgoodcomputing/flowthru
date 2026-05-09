using System.Collections.Generic;
using Flowthru.Step.Python;
using Flowthru.Step.Python;
using Python.Runtime;

namespace Flowthru.Step.Python.Internal;

/// <summary>
/// Performs basic validation checks during Python step registration.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Phase 4 registration-time validation:</strong>
/// Validates that the Python module, function, and @step decorator exist before
/// creating the step wrapper. This provides fast feedback during Flow definition.
/// </para>
/// <para>
/// <strong>Checks performed:</strong>
/// <list type="bullet">
/// <item>Module is importable (exists, no syntax errors)</item>
/// <item>Function exists in module</item>
/// <item>Function has @step decorator with metadata</item>
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
internal static class PythonStepRegistrationValidator
{
  /// <summary>
  /// Validates that a Python step can be registered, and returns metadata
  /// extracted from its <c>@step</c> decorator.
  /// </summary>
  /// <param name="runtime">Python runtime for GIL management</param>
  /// <param name="moduleName">Python module name (e.g., "flows.steps.transform")</param>
  /// <param name="functionName">Python function name (e.g., "encode_species")</param>
  /// <returns>
  /// Decorator-derived metadata (currently the list of declared service
  /// dependencies). Empty when no services are declared.
  /// </returns>
  /// <exception cref="InvalidOperationException">
  /// Thrown if module, function, or decorator is missing or invalid
  /// </exception>
  public static PythonStepMetadata ValidateRegistration(
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
          $"Python step registration failed: Module '{moduleName}' not found in sys.path\n\n"
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
            $"Python step registration failed: Function '{functionName}' not found in module '{moduleName}'\n\n"
              + $"Available functions: {string.Join(", ", availableFunctions)}\n\n"
              + "Check:\n"
              + "  - Function name spelling\n"
              + "  - Function is defined at module level (not inside a class)"
          );
        }
      }

      PyObject function = module.GetAttr(functionName);

      // Check 3: @step decorator is present
      if (!function.HasAttr("__flowthru_inputs__") || !function.HasAttr("__flowthru_outputs__"))
      {
        throw new InvalidOperationException(
          $"Python step registration failed: Function '{functionName}' in module '{moduleName}' "
            + "is missing required @step decorator.\n\n"
            + "Add decorator to declare schema contract:\n\n"
            + "  from flowthru import step\n"
            + "  from flowthru_schemas import InputSchema, OutputSchema\n"
            + "  \n"
            + $"  @step(inputs=[InputSchema], outputs=[OutputSchema])\n"
            + $"  def {functionName}(...):\n"
            + "      ...\n\n"
            + "The @step decorator is required for all Python steps in Flows."
        );
      }

      // Basic registration requirements are met. Capture decorator
      // metadata (inputs / outputs / services from
      // __flowthru_inputs__ / __flowthru_outputs__ /
      // __flowthru_services__) for the caller. More thorough
      // validation happens during pre-flight.
      var inputs = ExtractStringAttribute(function, "__flowthru_inputs__");
      var outputs = ExtractStringAttribute(function, "__flowthru_outputs__");
      var services = ExtractServiceList(function);
      return inputs.Count == 0 && outputs.Count == 0 && services.Count == 0
        ? PythonStepMetadata.Empty
        : new PythonStepMetadata(inputs, outputs, services);
    }
  }

  /// <summary>
  /// Read a list-of-strings attribute (e.g. <c>__flowthru_inputs__</c>)
  /// off the decorated Python function. Returns an empty list when
  /// the attribute is missing — though by the time this method is
  /// reached the decorator-presence check above has already verified
  /// the inputs/outputs attributes exist, so missing here implies a
  /// malformed decorator and we let the caller fall through.
  /// </summary>
  private static IReadOnlyList<string> ExtractStringAttribute(PyObject function, string attrName)
  {
    if (!function.HasAttr(attrName)) return Array.Empty<string>();

    using (Py.GIL())
    {
      var attr = function.GetAttr(attrName);
      var result = new List<string>();
      var length = attr.Length();
      for (long i = 0; i < length; i++)
      {
        using var pyIndex = new PyInt(i);
        var item = attr.GetItem(pyIndex);
        var value = item.ToString();
        if (!string.IsNullOrEmpty(value)) result.Add(value);
      }
      return result;
    }
  }

  /// <summary>
  /// Reads <c>__flowthru_services__</c> from the decorated function (a list
  /// of fully-qualified Python class paths). Returns an empty list when the
  /// attribute is missing — pre-existing steps without the decorator's new
  /// <c>services=</c> parameter remain valid.
  /// </summary>
  /// <remarks>
  /// Caller is expected to hold the GIL.
  /// </remarks>
  private static List<string> ExtractServiceList(PyObject function)
  {
    if (!function.HasAttr("__flowthru_services__"))
    {
      return new List<string>(capacity: 0);
    }

    using PyObject raw = function.GetAttr("__flowthru_services__");
    // The decorator stores a Python list — wrap as PyList for IEnumerable
    // support. PyObject.GetAttr returns the base PyObject type, which does
    // not implement IEnumerable directly.
    using var list = new PyList(raw);
    var result = new List<string>(capacity: (int)list.Length());
    foreach (PyObject item in list)
    {
      var value = item.ToString();
      if (!string.IsNullOrEmpty(value))
      {
        result.Add(value);
      }
      item.Dispose();
    }
    return result;
  }
}
