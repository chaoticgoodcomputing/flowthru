using System.Runtime.CompilerServices;
using Flowthru.Data.Storage;
using Flowthru.Prelude;
using Flowthru.Step.Python;
using Flowthru.Validation.PreFlight;
using Flowthru.Validation.PreFlight.Python;
using Flowthru.Validation.Runtime;
using Flowthru.Validation.Runtime.Python;
using Microsoft.Extensions.Logging;
using Python.Runtime;
using PythonEngineRuntime = Python.Runtime.Runtime;

namespace Flowthru.Step.Python.Internal;

/// <summary>
/// In-process Python executor using Python.NET.
/// </summary>
/// <remarks>
/// <para>
/// Executes Python functions within the same process via Python.NET's embedded runtime.
/// All marshalling (scalar, tabular Arrow IPC, bytes, multi-I/O tuples) is handled
/// internally — callers interact only with strongly-typed C# values.
/// </para>
/// <para>
/// <strong>Isolation caveat:</strong>
/// <c>PythonEngine</c> is process-global. Multiple <c>PythonNetExecutor</c> instances share
/// the same interpreter, <c>sys.modules</c>, and GIL. Use <see cref="SubprocessPythonExecutor"/>
/// when true per-service isolation is required.
/// </para>
/// <para>
/// Thread-safety: GIL acquisition serialises all Python execution.
/// </para>
/// </remarks>
public sealed class PythonNetExecutor : IPythonExecutor
{
  private readonly PythonRuntime _runtime;
  private readonly ILogger<PythonNetExecutor> _logger;
  private readonly Dictionary<string, PyObject> _moduleCache = new();
  private readonly object _cacheLock = new();

  /// <summary>
  /// Initializes a new instance of the <see cref="PythonNetExecutor"/> class with the specified Python runtime and logger.
  /// The Python runtime is initialized upon construction.
  /// </summary>
  /// <param name="runtime">The Python runtime instance.</param>
  /// <param name="logger">The logger instance.</param>
  /// <exception cref="ArgumentNullException"></exception>
  public PythonNetExecutor(PythonRuntime runtime, ILogger<PythonNetExecutor> logger)
  {
    _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    _runtime.Initialize();
  }

  /// <inheritdoc />
  public FlowIO<TOutput> Invoke<TInput, TOutput>(
    string moduleName,
    string functionName,
    TInput input
  ) =>
    FlowIO.LiftAsync<TOutput>(
      ct => Task.FromResult(InvokeCore<TInput, TOutput>(moduleName, functionName, input)),
      source: $"PythonNetExecutor.Invoke[{moduleName}.{functionName}]"
    ).MapError(err => err switch
    {
      RuntimeError.External ext when ext.Cause is PythonException pyx
        => new RuntimeError.ExtensionError(
          new PythonRuntimeError.WorkerError(moduleName, functionName, pyx.Message)
        ),
      _ => err,
    });

  private TOutput InvokeCore<TInput, TOutput>(string moduleName, string functionName, TInput input)
  {
    if (string.IsNullOrWhiteSpace(moduleName))
    {
      throw new ArgumentException("Module name cannot be null or whitespace.", nameof(moduleName));
    }

    if (string.IsNullOrWhiteSpace(functionName))
    {
      throw new ArgumentException(
        "Function name cannot be null or whitespace.",
        nameof(functionName)
      );
    }

    using (_runtime.AcquireGil())
    {
      try
      {
        var inputType = typeof(TInput);
        var outputType = typeof(TOutput);

        bool isInputTuple = IsValueTuple(inputType);
        bool isOutputTuple = IsValueTuple(outputType);

        if (isInputTuple || isOutputTuple)
        {
          return InvokeMulti<TInput, TOutput>(moduleName, functionName, input);
        }

        // DirectoryOf<T> dispatch is uniform: marshal input via MarshalSingleArg (which
        // handles directory + tabular + scalar), invoke, unmarshal via UnmarshalElement
        // (likewise). Routed before the tabular/scalar split so both ends stay simple.
        if (IsDirectoryType(inputType) || IsDirectoryType(outputType))
        {
          return InvokeWithDirectory<TInput, TOutput>(moduleName, functionName, input);
        }

        bool isInputTabular = IsEnumerableSchema(inputType);
        bool isOutputTabular = IsEnumerableSchema(outputType);

        if (isInputTabular && isOutputTabular)
        {
          return InvokeTabular<TInput, TOutput>(moduleName, functionName, input);
        }

        if (!isInputTabular && !isOutputTabular)
        {
          return InvokeScalar<TInput, TOutput>(moduleName, functionName, input);
        }

        return InvokeMixed<TInput, TOutput>(
          moduleName,
          functionName,
          input,
          isInputTabular,
          isOutputTabular
        );
      }
      catch (PythonException ex)
      {
        _logger.LogError(
          ex,
          "Python exception in {Module}.{Function}: {Message}",
          moduleName,
          functionName,
          ex.Message
        );
        throw;
      }
    }
  }

  /// <inheritdoc />
  public FlowIO<PythonStepMetadata> ValidateStep(string moduleName, string functionName) =>
    FlowIO.LiftAsync<PythonStepMetadata>(
      ct =>
      {
        _runtime.Initialize();
        return Task.FromResult(
          PythonStepRegistrationValidator.ValidateRegistration(_runtime, moduleName, functionName)
        );
      },
      source: $"PythonNetExecutor.ValidateStep[{moduleName}.{functionName}]"
    ).MapError(err => err switch
    {
      RuntimeError.External ext when ext.Cause is InvalidOperationException ioe
        => new RuntimeError.ExtensionError(ClassifyValidateFailure(moduleName, functionName, ioe.Message)),
      RuntimeError.External ext when ext.Cause is PythonException pyx
        => new RuntimeError.ExtensionError(new PythonRuntimeError.ModuleNotFound(moduleName, pyx.Message)),
      _ => err,
    });

  /// <inheritdoc />
  public FlowIO<Validated<PreFlightError, FlowUnit>> InvokeInspector(
    PythonServiceRegistration registration
  ) =>
    FlowIO.LiftAsync<Validated<PreFlightError, FlowUnit>>(
      ct => Task.FromResult(InvokeInspectorCore(registration)),
      source: $"PythonNetExecutor.InvokeInspector[{registration.ServiceClassPath}]"
    );

  private Validated<PreFlightError, FlowUnit> InvokeInspectorCore(
    PythonServiceRegistration registration
  )
  {
    if (registration is null)
    {
      throw new ArgumentNullException(nameof(registration));
    }

    _runtime.Initialize();

    using (_runtime.AcquireGil())
    {
      try
      {
        var serviceMod = ImportModule(registration.ServiceModule);
        if (!serviceMod.HasAttr(registration.ServiceClass))
        {
          return Fail(
            registration.ServiceClassPath,
            $"Service class '{registration.ServiceClass}' not found in module "
              + $"'{registration.ServiceModule}'."
          );
        }
        using PyObject serviceCls = serviceMod.GetAttr(registration.ServiceClass);
        using PyObject svc = serviceCls.Invoke();

        var inspectorMod = ImportModule(registration.InspectorModule);
        if (!inspectorMod.HasAttr(registration.InspectorFunction))
        {
          return Fail(
            registration.ServiceClassPath,
            $"Inspector function '{registration.InspectorFunction}' not found in "
              + $"module '{registration.InspectorModule}'."
          );
        }
        using PyObject inspectorFn = inspectorMod.GetAttr(registration.InspectorFunction);

        using PyObject pyResult = inspectorFn.Invoke(svc);
        return TranslatePyValidationResult(pyResult, registration.ServiceClassPath);
      }
      catch (PythonException ex)
      {
        return Fail(
          registration.ServiceClassPath,
          $"Python inspector raised: {ex.Message}"
        );
      }
    }

    static Validated<PreFlightError, FlowUnit> Fail(string serviceClassPath, string detail) =>
      Validated<PreFlightError, FlowUnit>.Fail(
        new PreFlightError.External(new PythonPreFlightError.ServiceInspectionFailed(
          ServiceClassPath: serviceClassPath,
          Detail: detail
        ))
      );
  }

  /// <summary>
  /// Translates the Python-side <c>flowthru.ValidationResult</c> dataclass
  /// into a <see cref="Validated{TError, TValue}"/> over
  /// <see cref="PreFlightError"/>. The Python <c>error_type</c> string is
  /// preserved in the failure detail for log readers; the C# closed sum
  /// gets a single <see cref="PythonPreFlightError.ServiceInspectionFailed"/>
  /// case regardless — categories don't proliferate across the language
  /// boundary.
  /// </summary>
  private static Validated<PreFlightError, FlowUnit> TranslatePyValidationResult(
    PyObject pyResult,
    string serviceClassPath
  )
  {
    if (!pyResult.HasAttr("success"))
    {
      return Validated<PreFlightError, FlowUnit>.Fail(
        new PreFlightError.External(new PythonPreFlightError.ServiceInspectionFailed(
          ServiceClassPath: serviceClassPath,
          Detail: "Inspector returned a value with no 'success' attribute (not a flowthru.ValidationResult)."
        ))
      );
    }

    using PyObject successPy = pyResult.GetAttr("success");
    bool success = successPy.As<bool>();
    if (success)
    {
      return Validated<PreFlightError, FlowUnit>.Pure(FlowUnit.Default);
    }

    string source = pyResult.HasAttr("source")
      ? pyResult.GetAttr("source").As<string>() ?? string.Empty
      : string.Empty;
    if (string.IsNullOrWhiteSpace(source)) source = serviceClassPath;

    string message = pyResult.HasAttr("message")
      ? pyResult.GetAttr("message").As<string>() ?? "(no message)"
      : "(no message)";
    string errorTypeText = pyResult.HasAttr("error_type")
      ? pyResult.GetAttr("error_type").As<string>() ?? string.Empty
      : string.Empty;

    var detail = string.IsNullOrWhiteSpace(errorTypeText)
      ? message
      : $"[{errorTypeText}] {message}";

    return Validated<PreFlightError, FlowUnit>.Fail(
      new PreFlightError.External(new PythonPreFlightError.ServiceInspectionFailed(
        ServiceClassPath: source,
        Detail: detail
      ))
    );
  }

  /// <summary>
  /// Heuristic mapping of validate-step exception messages to typed
  /// <see cref="PythonRuntimeError"/> cases. Mirrors the subprocess
  /// executor's helper of the same name.
  /// </summary>
  private static PythonRuntimeError ClassifyValidateFailure(
    string moduleName,
    string functionName,
    string message
  )
  {
    if (message.IndexOf("not found in sys.path", StringComparison.OrdinalIgnoreCase) >= 0
      || message.IndexOf("ModuleNotFoundError", StringComparison.OrdinalIgnoreCase) >= 0
      || message.IndexOf("ImportError", StringComparison.OrdinalIgnoreCase) >= 0)
    {
      return new PythonRuntimeError.ModuleNotFound(moduleName, message);
    }
    if (message.IndexOf("not found in module", StringComparison.OrdinalIgnoreCase) >= 0
      || message.IndexOf("AttributeError", StringComparison.OrdinalIgnoreCase) >= 0)
    {
      return new PythonRuntimeError.FunctionMissing(moduleName, functionName);
    }
    if (message.IndexOf("@step decorator", StringComparison.OrdinalIgnoreCase) >= 0
      || message.IndexOf("__flowthru_inputs__", StringComparison.Ordinal) >= 0)
    {
      return new PythonRuntimeError.DecoratorAbsent(moduleName, functionName);
    }
    return new PythonRuntimeError.WorkerError(moduleName, functionName, message);
  }

  private PyObject ImportModule(string moduleName)
  {
    // Reuse the existing module cache if entries flow through it. The
    // private cache field is populated by the invoke paths; for inspector
    // invocations we just call Py.Import (which is itself cached by
    // sys.modules and returns the same object on repeat).
    return Py.Import(moduleName);
  }

  // ── Scalar path ──────────────────────────────────────────────────────────────────────

  private TOutput InvokeScalar<TInput, TOutput>(string module, string function, TInput input)
  {
    var pyInput = ScalarMarshaller.ToPython(input);
    var pyResult = InvokeRaw(module, function, pyInput);
    return ScalarMarshaller.FromPython<TOutput>(pyResult);
  }

  // ── Tabular path (Arrow IPC) ──────────────────────────────────────────────────────────

  private TOutput InvokeTabular<TInput, TOutput>(string module, string function, TInput input)
  {
    var inputElementType = typeof(TInput).GetGenericArguments()[0];
    var outputElementType = typeof(TOutput).GetGenericArguments()[0];

    var ipcBuffer = ToIpcBuffer(input!, inputElementType);
    dynamic bridge = Py.Import("_flowthru_arrow");
    PyObject inputDf = bridge.df_from_ipc(ipcBuffer);

    PyObject outputDf = InvokeRaw(module, function, inputDf);

    return (TOutput)FromIpcBuffer(outputDf, outputElementType, bridge)!;
  }

  // ── Mixed path ────────────────────────────────────────────────────────────────────────

  private TOutput InvokeMixed<TInput, TOutput>(
    string module,
    string function,
    TInput input,
    bool isInputTabular,
    bool isOutputTabular
  )
  {
    PyObject pyInput = isInputTabular
      ? TabularToPyObject(input!, typeof(TInput))
      : ScalarMarshaller.ToPython(input);

    var pyResult = InvokeRaw(module, function, pyInput);

    if (isOutputTabular)
    {
      dynamic bridge = Py.Import("_flowthru_arrow");
      return (TOutput)FromIpcBuffer(pyResult, typeof(TOutput).GetGenericArguments()[0], bridge)!;
    }
    return ScalarMarshaller.FromPython<TOutput>(pyResult);
  }

  // ── Multi-I/O path (ValueTuple) ───────────────────────────────────────────────────────

  private TOutput InvokeMulti<TInput, TOutput>(string module, string function, TInput input)
  {
    var inputType = typeof(TInput);
    var outputType = typeof(TOutput);

    object[] inputArgs;
    if (IsValueTuple(inputType))
    {
      var elements = DecomposeTuple(input!);
      inputArgs = MarshalElements(elements);
    }
    else
    {
      inputArgs = new[] { MarshalSingleArg(input!, inputType) };
    }

    var pyResult = InvokeRaw(module, function, inputArgs);

    if (IsValueTuple(outputType))
    {
      return ReconstructTuple<TOutput>(pyResult, outputType);
    }

    return UnmarshalSingle<TOutput>(pyResult);
  }

  // ── Directory path ────────────────────────────────────────────────────────────────────

  private TOutput InvokeWithDirectory<TInput, TOutput>(string module, string function, TInput input)
  {
    var pyInput = (PyObject)MarshalSingleArg(input!, typeof(TInput));
    var pyResult = InvokeRaw(module, function, pyInput);
    return (TOutput)UnmarshalElement(pyResult, typeof(TOutput))!;
  }

  // ── Raw invocation ────────────────────────────────────────────────────────────────────

  private PyObject InvokeRaw(string moduleName, string functionName, params object[] args)
  {
    var module = GetOrImportModule(moduleName);

    if (!module.HasAttr(functionName))
    {
      throw new InvalidOperationException(
        $"Function '{functionName}' not found in module '{moduleName}'"
      );
    }

    dynamic function = module.GetAttr(functionName);
    _logger.LogDebug(
      "Invoking {Module}.{Function} with {ArgCount} arguments",
      moduleName,
      functionName,
      args.Length
    );

    var pyArgs = args.Select(a => a is PyObject p ? p : a.ToPython()).ToArray();
    return function.Invoke(pyArgs);
  }

  private PyObject GetOrImportModule(string moduleName)
  {
    lock (_cacheLock)
    {
      if (_moduleCache.TryGetValue(moduleName, out var cached))
      {
        return cached;
      }
    }

    _logger.LogDebug("Importing Python module: {ModuleName}", moduleName);
    try
    {
      var module = Py.Import(moduleName);
      lock (_cacheLock)
      {
        _moduleCache[moduleName] = module;
      }
      return module;
    }
    catch (PythonException ex)
    {
      throw new InvalidOperationException(
        $"Failed to import module '{moduleName}'. Ensure it is in sys.path and has no syntax errors.",
        ex
      );
    }
  }

  // ── Marshalling helpers ───────────────────────────────────────────────────────────────

  private static bool IsDirectoryType(Type type) =>
    type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Flowthru.Data.Storage.DirectoryOf<>);

  private static bool IsValueTuple(Type type)
  {
    if (!type.IsValueType || !type.IsGenericType)
    {
      return false;
    }

    return type.GetGenericTypeDefinition()
        .FullName?.StartsWith("System.ValueTuple`", StringComparison.Ordinal) ?? false;
  }

  private static bool IsEnumerableSchema(Type type)
  {
    if (type == typeof(string) || type == typeof(byte[]))
    {
      return false;
    }

    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
    {
      return true;
    }

    return type.GetInterfaces()
      .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
  }

  private static object[] DecomposeTuple(object tuple)
  {
    if (tuple is not ITuple t)
    {
      throw new InvalidOperationException($"Expected ITuple but got {tuple.GetType().Name}");
    }

    var elems = new object[t.Length];
    for (int i = 0; i < t.Length; i++)
    {
      elems[i] = t[i]!;
    }

    return elems;
  }

  private static object[] MarshalElements(object[] elements)
  {
    var result = new object[elements.Length];
    for (int i = 0; i < elements.Length; i++)
    {
      result[i] = MarshalSingleArg(elements[i], elements[i].GetType());
    }

    return result;
  }

  private static object MarshalSingleArg(object value, Type type)
  {
    if (IsDirectoryType(type))
      return DirectoryToPyObject(value, type);
    if (IsEnumerableSchema(type))
      return TabularToPyObject(value, type);
    return ScalarMarshaller.ToPython(value);
  }

  /// <summary>
  /// Converts a <see cref="Flowthru.Core.Data.DirectoryOf{T}"/> to a Python <c>dict</c> whose
  /// keys are file paths and whose values are marshalled per the inner type's kind:
  /// scalar/bytes through <see cref="ScalarMarshaller"/>, tabular through Arrow IPC,
  /// directory recursively (though nested directories aren't a typical shape). The Python
  /// step receives a plain <c>dict[str, T]</c>.
  /// </summary>
  private static PyObject DirectoryToPyObject(object value, Type directoryType)
  {
    var innerType = directoryType.GetGenericArguments()[0];

    // Caller owns the returned PyDict; no `using` here. Python.NET decrements the
    // refcount when the wrapper is collected, the same path InvokeRaw uses for any
    // other PyObject argument we return.
    var pyDict = new PyDict();
    foreach (var kvp in (System.Collections.IEnumerable)value)
    {
      var keyProp = kvp.GetType().GetProperty("Key")!;
      var valueProp = kvp.GetType().GetProperty("Value")!;
      var key = (string)keyProp.GetValue(kvp)!;
      var inner = valueProp.GetValue(kvp)!;

      using var pyKey = new PyString(key);
      var pyValue = (PyObject)MarshalSingleArg(inner, innerType);
      pyDict[pyKey] = pyValue;
    }
    return pyDict;
  }

  private static PyObject TabularToPyObject(object value, Type collectionType)
  {
    var elemType = collectionType.GetGenericArguments()[0];
    var ipcBuffer = ToIpcBuffer(value, elemType);
    dynamic bridge = Py.Import("_flowthru_arrow");
    return bridge.df_from_ipc(ipcBuffer);
  }

  private static byte[] ToIpcBuffer(object value, Type elementType)
  {
    var toRecordBatchMethod = typeof(ArrowMarshaller)
      .GetMethod(nameof(ArrowMarshaller.ToRecordBatch))!
      .MakeGenericMethod(elementType);
    var recordBatch = toRecordBatchMethod.Invoke(null, new[] { value })!;
    return ArrowMarshaller.ToIpcBuffer((Apache.Arrow.RecordBatch)recordBatch);
  }

  private static object? FromIpcBuffer(PyObject pyObj, Type elementType, dynamic bridge)
  {
    var buildDtypeMethod = typeof(ArrowSchemaMapper)
      .GetMethod(nameof(ArrowSchemaMapper.BuildDtypeSpec))!
      .MakeGenericMethod(elementType);
    PyObject dtypeSpec = (PyObject)buildDtypeMethod.Invoke(null, null)!;

    byte[] ipcBytes = bridge.df_to_ipc(pyObj, dtypeSpec).As<byte[]>();
    var batch = ArrowMarshaller.FromIpcBuffer(ipcBytes);

    var fromRecordBatchMethod = typeof(ArrowMarshaller)
      .GetMethod(nameof(ArrowMarshaller.FromRecordBatch))!
      .MakeGenericMethod(elementType);
    return fromRecordBatchMethod.Invoke(null, new object[] { batch });
  }

  private TOutput ReconstructTuple<TOutput>(PyObject pyTuple, Type tupleType)
  {
    var elementTypes = tupleType.GetGenericArguments();
    if (!pyTuple.IsIterable() || pyTuple.Length() != elementTypes.Length)
    {
      throw new InvalidOperationException(
        $"Python function returned tuple with {(pyTuple.IsIterable() ? pyTuple.Length() : 0)} elements; expected {elementTypes.Length}"
      );
    }

    var elements = new object?[elementTypes.Length];
    for (int i = 0; i < elementTypes.Length; i++)
    {
      elements[i] = UnmarshalElement(pyTuple.GetItem(new PyInt(i)), elementTypes[i]);
    }

    return (TOutput)Activator.CreateInstance(tupleType, elements)!;
  }

  private static TOutput UnmarshalSingle<TOutput>(PyObject pyObj)
  {
    var type = typeof(TOutput);
    if (IsEnumerableSchema(type))
    {
      var elemType = type.GetGenericArguments()[0];
      dynamic bridge = Py.Import("_flowthru_arrow");
      return (TOutput)FromIpcBuffer(pyObj, elemType, bridge)!;
    }
    return ScalarMarshaller.FromPython<TOutput>(pyObj);
  }

  private static object? UnmarshalElement(PyObject pyObj, Type targetType)
  {
    if (IsDirectoryType(targetType))
      return DirectoryFromPyObject(pyObj, targetType);

    if (IsEnumerableSchema(targetType))
    {
      var elemType = targetType.GetGenericArguments()[0];
      dynamic bridge = Py.Import("_flowthru_arrow");
      return FromIpcBuffer(pyObj, elemType, bridge);
    }

    var fromPythonMethod = typeof(ScalarMarshaller)
      .GetMethod(nameof(ScalarMarshaller.FromPython))!
      .MakeGenericMethod(targetType);
    return fromPythonMethod.Invoke(null, new object[] { pyObj });
  }

  /// <summary>
  /// Converts a Python <c>dict[str, T]</c> back into a <see cref="Flowthru.Core.Data.DirectoryOf{T}"/>.
  /// Each value is unmarshalled via <see cref="UnmarshalElement"/> so the inner kind
  /// (scalar/tabular/directory) is handled uniformly.
  /// </summary>
  private static object DirectoryFromPyObject(PyObject pyObj, Type directoryType)
  {
    var innerType = directoryType.GetGenericArguments()[0];
    var pyDict = new PyDict(pyObj);

    var dictType = typeof(Dictionary<,>).MakeGenericType(typeof(string), innerType);
    var dict = (System.Collections.IDictionary)Activator.CreateInstance(dictType)!;

    foreach (PyObject pyKey in pyDict.Keys())
    {
      var key = pyKey.ToString() ?? throw new InvalidOperationException("Null directory key.");
      var pyValue = pyDict[pyKey];
      var inner = UnmarshalElement(pyValue, innerType);
      dict[key] = inner;
    }

    return Activator.CreateInstance(directoryType, dict)!;
  }
}
