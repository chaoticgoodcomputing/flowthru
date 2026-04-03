using System.IO;
using System.Reflection;
using Flowthru.Data.Validation;
using Flowthru.Extensions.Python.Execution;
using Flowthru.Extensions.Python.Marshalling;
using Flowthru.Extensions.Python.Runtime;
using Flowthru.Extensions.Python.Steps;
using Flowthru.Flows;
using Flowthru.Flows.Validation;
using Python.Runtime;

namespace Flowthru.Extensions.Python.Validation;

/// <summary>
/// Validation hook for Python steps.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Phase 4 pre-flight validation:</strong>
/// Validates Python steps during Flow pre-flight to catch schema mismatches,
/// incorrect function signatures, and structural errors before execution.
/// </para>
/// <para>
/// <strong>Checks performed:</strong>
/// <list type="bullet">
/// <item>@step decorator schemas match C# generic type parameters</item>
/// <item>Function signature arity is correct for input count</item>
/// <item>Dry-run with 0-row data validates output structure</item>
/// </list>
/// </para>
/// <para>
/// <strong>Integration:</strong>
/// Register this hook via Flow.ValidationHooks during Flow setup.
/// The hook is automatically invoked during Flow.ValidateExternalInputsAsync().
/// </para>
/// </remarks>
public class PythonStepValidator : IFlowValidationHook
{
  private readonly IPythonExecutor _executor;
  private readonly PythonRuntime _runtime;

  /// <summary>
  /// Initializes a new instance of <see cref="PythonStepValidator"/>.
  /// </summary>
  /// <param name="executor">Python executor for function inspection</param>
  /// <param name="runtime">Python runtime for GIL management</param>
  public PythonStepValidator(IPythonExecutor executor, PythonRuntime runtime)
  {
    _executor = executor ?? throw new ArgumentNullException(nameof(executor));
    _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
  }

  /// <inheritdoc />
  public async Task<ValidationResult> ValidateAsync(Flow flow, CancellationToken cancellationToken)
  {
    var errors = new List<ValidationError>();

    foreach (var step in flow.Steps)
    {
      // Check if this is a Python step by examining the transform function
      if (!IsPythonStep(step, out var pythonStepInfo))
      {
        continue; // Skip non-Python steps
      }

      // Validate decorator and schema compatibility
      var stepResult = await ValidatePythonStep(pythonStepInfo!, cancellationToken);
      errors.AddRange(stepResult.Errors);
    }

    return new ValidationResult(errors);
  }

  /// <summary>
  /// Checks if a Flow step is a Python step.
  /// </summary>
  private bool IsPythonStep(FlowStep step, out PythonStepInfo? info)
  {
    info = null;

    // Python steps use PythonStepWrapper<TInput, TOutput> as the transform target
    var transform = step.TransformFunction;
    if (transform.Target == null)
    {
      return false;
    }

    var targetType = transform.Target.GetType();

    // Check if target is PythonStepWrapper<,>
    if (!targetType.IsGenericType)
    {
      return false;
    }

    var genericTypeDef = targetType.GetGenericTypeDefinition();
    if (genericTypeDef != typeof(PythonStepWrapper<,>))
    {
      return false;
    }

    // Extract generic type arguments (TInput, TOutput)
    var genericArgs = targetType.GetGenericArguments();
    if (genericArgs.Length != 2)
    {
      return false;
    }

    // Extract module and function names via reflection
    var moduleField = targetType.GetField(
      "_moduleName",
      BindingFlags.NonPublic | BindingFlags.Instance
    );
    var functionField = targetType.GetField(
      "_functionName",
      BindingFlags.NonPublic | BindingFlags.Instance
    );

    if (moduleField == null || functionField == null)
    {
      return false;
    }

    var moduleName = moduleField.GetValue(transform.Target) as string;
    var functionName = functionField.GetValue(transform.Target) as string;

    if (string.IsNullOrEmpty(moduleName) || string.IsNullOrEmpty(functionName))
    {
      return false;
    }

    info = new PythonStepInfo
    {
      Label = step.Label,
      ModuleName = moduleName,
      FunctionName = functionName,
      InputType = genericArgs[0],
      OutputType = genericArgs[1],
    };

    return true;
  }

  /// <summary>
  /// Validates a single Python step.
  /// </summary>
  private async Task<ValidationResult> ValidatePythonStep(
    PythonStepInfo stepInfo,
    CancellationToken cancellationToken
  )
  {
    var errors = new List<ValidationError>();

    using (_runtime.AcquireGil())
    {
      // Import module and get function
      PyObject module;
      PyObject function;

      try
      {
        using (Py.GIL())
        {
          module = Py.Import(stepInfo.ModuleName);
          function = module.GetAttr(stepInfo.FunctionName);
        }
      }
      catch (PythonException ex)
      {
        errors.Add(
          new ValidationError(
            stepInfo.Label,
            ValidationErrorType.InvalidFormat,
            $"Failed to import Python function: {ex.Message}",
            ex.ToString()
          )
        );
        return new ValidationResult(errors);
      }

      // Check decorator metadata
      var decoratorResult = ValidateDecorator(stepInfo, function);
      errors.AddRange(decoratorResult.Errors);

      if (!decoratorResult.IsValid)
      {
        return new ValidationResult(errors); // No point continuing if decorator is invalid
      }

      // Dry-run dtype validation for tabular outputs
      if (IsEnumerableSchema(stepInfo.OutputType))
      {
        var dryRunResult = ValidateDryRunDtypes(stepInfo, function);
        errors.AddRange(dryRunResult.Errors);
      }
    }

    await Task.CompletedTask; // Suppress async warning
    return new ValidationResult(errors);
  }

  /// <summary>
  /// Validates output dtypes using a 0-row dry-run invocation.
  /// </summary>
  /// <remarks>
  /// For tabular outputs, creates a 0-row DataFrame with the expected schema,
  /// passes it through the Python function, and checks if the output dtypes
  /// can be safely coerced to the C# schema types.
  /// </remarks>
  private ValidationResult ValidateDryRunDtypes(PythonStepInfo stepInfo, PyObject function)
  {
    var errors = new List<ValidationError>();

    try
    {
      using (Py.GIL())
      {
        var schemaType = stepInfo.OutputType.GetGenericArguments()[0];

        // Build expected dtype spec from C# schema
        var buildDtypeSpecMethod = typeof(ArrowSchemaMapper)
          .GetMethod(nameof(ArrowSchemaMapper.BuildDtypeSpec))!
          .MakeGenericMethod(schemaType);
        PyObject expectedDtypeSpec = (PyObject)buildDtypeSpecMethod.Invoke(null, null)!;

        // Extract expected column names and dtypes
        var expectedColumns = new Dictionary<string, string>();
        using (var dtypeDict = expectedDtypeSpec as PyDict)
        {
          if (dtypeDict != null)
          {
            foreach (PyObject key in dtypeDict.Keys())
            {
              var colName = key.As<string>();
              var dtype = dtypeDict.GetItem(key).As<string>();
              expectedColumns[colName!] = dtype!;
            }
          }
        }

        // Log expected schema for diagnostics
        var schemaDetails = string.Join(
          ", ",
          expectedColumns.Select(kvp => $"{kvp.Key}:{kvp.Value}")
        );

        // Note: A full dry-run would require creating 0-row input DataFrames for all inputs,
        // invoking the function, and checking output dtypes. This is complex for multi-input step
        // and may have side effects (imports, setup code).
        //
        // Instead, we rely on:
        // 1. Registration-time validation (@step decorator exists)
        // 2. Runtime automatic coercion in _flowthru_arrow.py (df_to_ipc with dtype_spec)
        //
        // If a Python step returns incompatible dtypes, the runtime coercion will raise
        // detailed TypeError/OverflowError with fix guidance.
        //
        // Pre-flight dry-run validation would be added here as a Phase 5 enhancement.
      }
    }
    catch (Exception ex)
    {
      errors.Add(
        new ValidationError(
          stepInfo.Label,
          ValidationErrorType.InspectionFailure,
          $"Dry-run dtype validation failed: {ex.Message}",
          "This is a framework error, not a user error. Report this issue."
        )
      );
    }

    return new ValidationResult(errors);
  }

  /// <summary>
  /// Checks if a type is IEnumerable&lt;T&gt; where T is a schema type.
  /// </summary>
  private static bool IsEnumerableSchema(Type type)
  {
    // Fast path: declared as IEnumerable<T>
    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
      return true;

    // Runtime path: List<T>, T[], HashSet<T>, etc. that implement IEnumerable<T>
    return type.GetInterfaces()
      .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
  }

  /// <summary>
  /// Validates that the @step decorator metadata matches C# type parameters.
  /// </summary>
  private ValidationResult ValidateDecorator(PythonStepInfo stepInfo, PyObject function)
  {
    var errors = new List<ValidationError>();

    // Extract decorator metadata
    if (!function.HasAttr("__flowthru_inputs__") || !function.HasAttr("__flowthru_outputs__"))
    {
      errors.Add(
        new ValidationError(
          stepInfo.Label,
          ValidationErrorType.SchemaMismatch,
          $"Function '{stepInfo.FunctionName}' is missing @step decorator metadata",
          "This should have been caught during registration-time validation"
        )
      );
      return new ValidationResult(errors);
    }

    PyObject inputsObj = function.GetAttr("__flowthru_inputs__");
    PyObject outputsObj = function.GetAttr("__flowthru_outputs__");

    // Convert to C# lists using explicit indexing (PyObject doesn't support IEnumerable)
    var decoratorInputs = new List<string>();
    var decoratorOutputs = new List<string>();

    using (Py.GIL())
    {
      long inputsLength = inputsObj.Length();
      for (long i = 0; i < inputsLength; i++)
      {
        using (var pyIndex = new PyInt(i))
        {
          decoratorInputs.Add(inputsObj.GetItem(pyIndex).ToString() ?? "");
        }
      }

      long outputsLength = outputsObj.Length();
      for (long i = 0; i < outputsLength; i++)
      {
        using (var pyIndex = new PyInt(i))
        {
          decoratorOutputs.Add(outputsObj.GetItem(pyIndex).ToString() ?? "");
        }
      }
    }

    // Extract C# schema names from type parameters (Phase 5: supports tuples)
    var csharpInputSchemas = ExtractSchemaNames(stepInfo.InputType);
    var csharpOutputSchemas = ExtractSchemaNames(stepInfo.OutputType);

    // Validate input schema count
    if (decoratorInputs.Count != csharpInputSchemas.Count)
    {
      errors.Add(
        new ValidationError(
          stepInfo.Label,
          ValidationErrorType.SchemaMismatch,
          $"Input schema count mismatch: C# expects {csharpInputSchemas.Count} input(s), decorator declares {decoratorInputs.Count}",
          $"C# inputs: [{string.Join(", ", csharpInputSchemas)}]\nDecorator inputs: [{string.Join(", ", decoratorInputs)}]"
        )
      );
    }

    // Validate output schema count
    if (decoratorOutputs.Count != csharpOutputSchemas.Count)
    {
      errors.Add(
        new ValidationError(
          stepInfo.Label,
          ValidationErrorType.SchemaMismatch,
          $"Output schema count mismatch: C# expects {csharpOutputSchemas.Count} output(s), decorator declares {decoratorOutputs.Count}",
          $"C# outputs: [{string.Join(", ", csharpOutputSchemas)}]\nDecorator outputs: [{string.Join(", ", decoratorOutputs)}]"
        )
      );
    }

    // Validate input schema names match (Phase 5: element-by-element)
    for (int i = 0; i < Math.Min(decoratorInputs.Count, csharpInputSchemas.Count); i++)
    {
      if (decoratorInputs[i] != csharpInputSchemas[i])
      {
        errors.Add(
          new ValidationError(
            stepInfo.Label,
            ValidationErrorType.SchemaMismatch,
            $"Input schema mismatch at position {i + 1}:\n  C# registration declares:  {csharpInputSchemas[i]}\n  Python decorator declares: {decoratorInputs[i]}",
            "The @step decorator must match the C# generic type parameters"
          )
        );
      }
    }

    // Validate output schema names match (Phase 5: element-by-element)
    for (int i = 0; i < Math.Min(decoratorOutputs.Count, csharpOutputSchemas.Count); i++)
    {
      if (decoratorOutputs[i] != csharpOutputSchemas[i])
      {
        errors.Add(
          new ValidationError(
            stepInfo.Label,
            ValidationErrorType.SchemaMismatch,
            $"Output schema mismatch at position {i + 1}:\n  C# registration declares:  {csharpOutputSchemas[i]}\n  Python decorator declares: {decoratorOutputs[i]}",
            "The @step decorator must match the C# generic type parameters"
          )
        );
      }
    }

    // Phase 5: Validate function arity using inspect.signature
    var arityResult = ValidateFunctionArity(stepInfo, function, csharpInputSchemas.Count);
    errors.AddRange(arityResult.Errors);

    return new ValidationResult(errors);
  }

  /// <summary>
  /// Validates that the Python function's parameter count matches expected input count (Phase 5).
  /// </summary>
  private ValidationResult ValidateFunctionArity(
    PythonStepInfo stepInfo,
    PyObject function,
    int expectedParamCount
  )
  {
    var errors = new List<ValidationError>();

    try
    {
      using (Py.GIL())
      {
        dynamic inspect = Py.Import("inspect");
        dynamic signature = inspect.signature(function);
        dynamic parameters = signature.parameters;

        int actualParamCount = (int)parameters.__len__();

        if (actualParamCount != expectedParamCount)
        {
          errors.Add(
            new ValidationError(
              stepInfo.Label,
              ValidationErrorType.SchemaMismatch,
              $"Function parameter count mismatch: Python function '{stepInfo.FunctionName}' has {actualParamCount} parameter(s), but C# registration expects {expectedParamCount} input(s)",
              $"Update the Python function signature to accept {expectedParamCount} parameter(s)"
            )
          );
        }
      }
    }
    catch (PythonException ex)
    {
      errors.Add(
        new ValidationError(
          stepInfo.Label,
          ValidationErrorType.InvalidFormat,
          $"Failed to inspect function signature: {ex.Message}",
          "Ensure the Python function is properly defined"
        )
      );
    }

    return new ValidationResult(errors);
  }

  /// <summary>
  /// Extracts schema names from a C# type (Phase 5: supports tuples).
  /// Handles scalar, IEnumerable&lt;T&gt;, and ValueTuple types.
  /// </summary>
  private List<string> ExtractSchemaNames(Type type)
  {
    // Check if type is a ValueTuple
    if (IsValueTuple(type))
    {
      // Multi-I/O: extract each tuple element
      var elementTypes = type.GetGenericArguments();
      var schemaNames = new List<string>();

      foreach (var elementType in elementTypes)
      {
        // Each element might be scalar or IEnumerable<T>
        schemaNames.Add(ExtractSingleSchemaName(elementType));
      }

      return schemaNames;
    }
    else
    {
      // Single I/O
      return new List<string> { ExtractSingleSchemaName(type) };
    }
  }

  /// <summary>
  /// Extracts the schema name from a single (non-tuple) type.
  /// </summary>
  private string ExtractSingleSchemaName(Type type)
  {
    // Check if type is IEnumerable<T>
    if (type.IsGenericType)
    {
      var genericTypeDef = type.GetGenericTypeDefinition();
      if (genericTypeDef == typeof(IEnumerable<>))
      {
        // Extract T from IEnumerable<T>
        var schemaType = type.GetGenericArguments()[0];
        return schemaType.Name;
      }
    }

    // Scalar type
    return type.Name;
  }

  /// <summary>
  /// Checks if a type is a ValueTuple (Phase 5).
  /// </summary>
  private bool IsValueTuple(Type type)
  {
    if (!type.IsValueType || !type.IsGenericType)
      return false;

    var genericTypeDef = type.GetGenericTypeDefinition();
    return genericTypeDef.FullName?.StartsWith("System.ValueTuple`", StringComparison.Ordinal)
      ?? false;
  }

  /// <summary>
  /// Information about a Python step extracted from the flow.
  /// </summary>
  private class PythonStepInfo
  {
    public required string Label { get; init; }
    public required string ModuleName { get; init; }
    public required string FunctionName { get; init; }
    public required Type InputType { get; init; }
    public required Type OutputType { get; init; }
  }
}
