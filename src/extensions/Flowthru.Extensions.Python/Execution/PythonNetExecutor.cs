using System.Runtime.CompilerServices;
using Flowthru.Extensions.Python.Marshalling;
using Flowthru.Extensions.Python.Runtime;
using Flowthru.Extensions.Python.Validation;
using Microsoft.Extensions.Logging;
using Python.Runtime;
using PythonEngineRuntime = Python.Runtime.Runtime;

namespace Flowthru.Extensions.Python.Execution;

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
    public TOutput Invoke<TInput, TOutput>(string moduleName, string functionName, TInput input)
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
    public void ValidateStep(string moduleName, string functionName)
    {
        _runtime.Initialize();
        PythonStepRegistrationValidator.ValidateRegistration(_runtime, moduleName, functionName);
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
        if (IsEnumerableSchema(type))
        {
            var elemType = type.GetGenericArguments()[0];
            return TabularToPyObject(value, type);
        }
        return ScalarMarshaller.ToPython(value);
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
}
