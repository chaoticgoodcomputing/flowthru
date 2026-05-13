using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Apache.Arrow;
using Flowthru.Data.Storage;
using Flowthru.Prelude;
using Flowthru.Step.Python;
using Flowthru.Validation.PreFlight;
using Flowthru.Validation.PreFlight.Python;
using Flowthru.Validation.Runtime;
using Flowthru.Validation.Runtime.Python;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Flowthru.Step.Python.Internal;

/// <summary>
/// Subprocess Python executor — spawns an isolated Python worker process per instance.
/// </summary>
/// <remarks>
/// <para>
/// Each <c>SubprocessPythonExecutor</c> owns one child Python process. Isolation is at the
/// OS process boundary: separate interpreter, <c>sys.modules</c>, venv, and memory space.
/// No Python.NET, no GIL management on the C# side.
/// </para>
/// <para>
/// <strong>Protocol:</strong> newline-delimited JSON over stdin/stdout.
/// Tabular data is exchanged as base64-encoded Apache Arrow IPC buffers.
/// Dtype coercion specs are serialized from <see cref="ArrowSchemaMapper.BuildDtypeSpecDictionary{T}"/>.
/// </para>
/// <para>
/// The worker script (<c>flowthru_worker.py</c>) must be present in
/// <see cref="AppContext.BaseDirectory"/>.
/// </para>
/// </remarks>
public sealed class SubprocessPythonExecutor : IPythonExecutor, IDisposable
{
  private readonly PythonRuntimeOptions _options;
  private readonly IPythonConfigurationFlattener _flattener;
  private readonly ILogger<SubprocessPythonExecutor> _logger;

  private Process? _worker;
  private StreamWriter? _stdin;
  private StreamReader? _stdout;
  private readonly object _lock = new();
  private volatile bool _started;
  private bool _disposed;

  /// <summary>
  /// Initializes a new instance of the <see cref="SubprocessPythonExecutor"/> class with the specified options and logger.
  /// The Python worker process is started lazily upon the first call to <see cref="Invoke{TInput, TOutput}"/> or <see cref="ValidateStep"/>.
  /// </summary>
  /// <param name="options">The Python runtime options.</param>
  /// <param name="flattener">
  /// IConfiguration → env-var bridge. Called at subprocess spawn to populate
  /// the worker's <see cref="ProcessStartInfo.EnvironmentVariables"/> from
  /// the section named in <see cref="PythonRuntimeOptions.ConfigurationSection"/>.
  /// </param>
  /// <param name="logger">The logger instance.</param>
  /// <exception cref="ArgumentNullException"></exception>
  public SubprocessPythonExecutor(
    IOptions<PythonRuntimeOptions> options,
    IPythonConfigurationFlattener flattener,
    ILogger<SubprocessPythonExecutor> logger
  )
  {
    _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    _flattener = flattener ?? throw new ArgumentNullException(nameof(flattener));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  /// <inheritdoc />
  public FlowIO<TOutput> Invoke<TInput, TOutput>(
    string moduleName,
    string functionName,
    TInput input
  ) =>
    FlowIO.LiftAsync<TOutput>(
      ct => Task.FromResult(InvokeCore<TInput, TOutput>(moduleName, functionName, input)),
      source: $"SubprocessPythonExecutor.Invoke[{moduleName}.{functionName}]"
    ).MapError(err => err switch
    {
      RuntimeError.External ext when ext.Cause is InvalidOperationException ioe
        => new RuntimeError.ExtensionError(ClassifyInvokeFailure(moduleName, functionName, ioe.Message)),
      // Any other typed exception escaping LiftAsync is classified by its
      // type/message — most commonly NotSupportedException from
      // ArrowMarshaller. Build a multi-line detail string that carries the
      // exception type name and full message so the user-facing surface
      // (.Message, metadata JSON) shows the real cause, not a generic wrapper.
      RuntimeError.External ext
        => new RuntimeError.ExtensionError(
          ClassifyInvokeFailure(moduleName, functionName, FormatInnerExceptionDetail(ext.Cause))
        ),
      _ => err,
    });

  private TOutput InvokeCore<TInput, TOutput>(string moduleName, string functionName, TInput input)
  {
    EnsureStarted();

    var inputKind = ClassifyType(typeof(TInput));
    var outputKind = ClassifyType(typeof(TOutput));

    var req = new JsonObject
    {
      ["type"] = "invoke",
      ["module"] = moduleName,
      ["function"] = functionName,
      ["input_type"] = inputKind,
      ["input"] = EncodeValue(input!, typeof(TInput), inputKind),
      ["output_type"] = outputKind,
    };

    // Include dtype spec for tabular outputs so the worker can coerce Arrow types
    if (outputKind == "tabular")
    {
      req["output_dtype_spec"] = BuildDtypeSpecJson(typeof(TOutput));
    }
    else if (outputKind == "multi")
    {
      req["output_element_specs"] = BuildMultiElementSpecs(typeof(TOutput));
    }
    else if (outputKind == "directory")
    {
      req["output_directory_spec"] = BuildDirectorySpecJson(typeof(TOutput));
    }

    var resp = SendRequest(req);

    if (resp["status"]?.GetValue<string>() != "ok")
    {
      var msg = resp["message"]?.GetValue<string>() ?? "Unknown error";
      throw new InvalidOperationException(
        $"Python worker error in {moduleName}.{functionName}: {msg}"
      );
    }

    var outputPayload =
      resp["output"]?.GetValue<string>()
      ?? throw new InvalidOperationException("Python worker returned no output.");
    return DecodeValue<TOutput>(outputPayload, typeof(TOutput), outputKind);
  }

  /// <inheritdoc />
  public FlowIO<PythonStepMetadata> ValidateStep(string moduleName, string functionName) =>
    FlowIO.LiftAsync<PythonStepMetadata>(
      ct => Task.FromResult(ValidateStepCore(moduleName, functionName)),
      source: $"SubprocessPythonExecutor.ValidateStep[{moduleName}.{functionName}]"
    ).MapError(err => err switch
    {
      RuntimeError.External ext when ext.Cause is InvalidOperationException ioe
        => new RuntimeError.ExtensionError(ClassifyValidateFailure(moduleName, functionName, ioe.Message)),
      _ => err,
    });

  private PythonStepMetadata ValidateStepCore(string moduleName, string functionName)
  {
    EnsureStarted();

    var req = new JsonObject
    {
      ["type"] = "validate",
      ["module"] = moduleName,
      ["function"] = functionName,
    };

    var resp = SendRequest(req);

    if (resp["status"]?.GetValue<string>() != "ok")
    {
      var msg = resp["message"]?.GetValue<string>() ?? "Unknown error";
      throw new InvalidOperationException(
        $"Python step validation failed for {moduleName}.{functionName}: {msg}"
      );
    }

    // Extract decorator-derived metadata from the validate response.
    // The worker emits inputs / outputs / services from the @step
    // decorator's __flowthru_*__ attributes; missing fields are
    // tolerated (legacy workers without this enhancement still work
    // for the services-only path).
    var inputs = ExtractStringList(resp, "inputs");
    var outputs = ExtractStringList(resp, "outputs");
    var services = ExtractStringList(resp, "services");
    return inputs.Count == 0 && outputs.Count == 0 && services.Count == 0
      ? PythonStepMetadata.Empty
      : new PythonStepMetadata(inputs, outputs, services);
  }

  /// <inheritdoc />
  public FlowIO<Validated<PreFlightError, FlowUnit>> InvokeInspector(
    PythonServiceRegistration registration
  )
  {
    if (registration is null) throw new ArgumentNullException(nameof(registration));

    return FlowIO.LiftAsync<Validated<PreFlightError, FlowUnit>>(
      ct => Task.FromResult(InvokeInspectorCore(registration)),
      source: $"SubprocessPythonExecutor.InvokeInspector[{registration.ServiceClassPath}]"
    ).MapError(err => err switch
    {
      RuntimeError.External ext when ext.Cause is InvalidOperationException ioe
        => new RuntimeError.ExtensionError(new PythonRuntimeError.WorkerCrashed(ioe.Message)),
      _ => err,
    });
  }

  private Validated<PreFlightError, FlowUnit> InvokeInspectorCore(
    PythonServiceRegistration registration
  )
  {
    EnsureStarted();

    var req = new JsonObject
    {
      ["type"] = "inspect",
      ["service_module"] = registration.ServiceModule,
      ["service_class"] = registration.ServiceClass,
      ["inspector_module"] = registration.InspectorModule,
      ["inspector_function"] = registration.InspectorFunction,
    };

    var resp = SendRequest(req);

    if (resp["status"]?.GetValue<string>() != "ok")
    {
      var msg = resp["message"]?.GetValue<string>() ?? "Unknown error";
      return Validated<PreFlightError, FlowUnit>.Fail(
        new PreFlightError.External(new PythonPreFlightError.ServiceInspectionFailed(
          ServiceClassPath: registration.ServiceClassPath,
          Detail: msg
        ))
      );
    }

    if (resp["result"] is not JsonObject result)
    {
      return Validated<PreFlightError, FlowUnit>.Fail(
        new PreFlightError.External(new PythonPreFlightError.ServiceInspectionFailed(
          ServiceClassPath: registration.ServiceClassPath,
          Detail: "Inspector returned a malformed payload (no 'result' object)."
        ))
      );
    }

    return TranslateInspectorResult(result, registration.ServiceClassPath);
  }

  /// <summary>
  /// Translates the worker's <c>{success, source, error_type, message}</c>
  /// dict into a <see cref="Validated{TError, TValue}"/> over
  /// <see cref="PreFlightError"/>. The Python-side <c>error_type</c>
  /// is preserved in the failure's detail string for log readability;
  /// the C# closed sum gets a single
  /// <see cref="PythonPreFlightError.ServiceInspectionFailed"/> case
  /// regardless of the Python-side category, by design — we don't
  /// proliferate categories across the language boundary.
  /// </summary>
  private static Validated<PreFlightError, FlowUnit> TranslateInspectorResult(
    JsonObject result,
    string serviceClassPath
  )
  {
    var success = result["success"]?.GetValue<bool>() ?? false;
    if (success)
    {
      return Validated<PreFlightError, FlowUnit>.Pure(FlowUnit.Default);
    }

    var source = result["source"]?.GetValue<string>();
    if (string.IsNullOrWhiteSpace(source)) source = serviceClassPath;
    var message = result["message"]?.GetValue<string>() ?? "(no message)";
    var errorTypeText = result["error_type"]?.GetValue<string>();

    var detail = string.IsNullOrWhiteSpace(errorTypeText)
      ? message
      : $"[{errorTypeText}] {message}";

    return Validated<PreFlightError, FlowUnit>.Fail(
      new PreFlightError.External(new PythonPreFlightError.ServiceInspectionFailed(
        ServiceClassPath: source!,
        Detail: detail
      ))
    );
  }

  private static List<string> ExtractStringList(JsonObject root, string key)
  {
    if (root[key] is not JsonArray array)
    {
      return new List<string>(capacity: 0);
    }
    var result = new List<string>(capacity: array.Count);
    foreach (var node in array)
    {
      var value = node?.GetValue<string>();
      if (!string.IsNullOrEmpty(value))
      {
        result.Add(value);
      }
    }
    return result;
  }

  // ── Worker lifecycle ──────────────────────────────────────────────────────────────────

  private void EnsureStarted()
  {
    if (_started)
    {
      return;
    }

    lock (_lock)
    {
      if (_started)
      {
        return;
      }

      StartWorker();
      _started = true;
    }
  }

  private void StartWorker()
  {
    var pyExe = PythonEnvironmentResolver.ResolvePythonExe(_options);
    var workerScript = Path.Combine(AppContext.BaseDirectory, "flowthru_worker.py");

    if (!File.Exists(workerScript))
    {
      throw new FileNotFoundException(
        $"flowthru_worker.py not found at '{workerScript}'. Ensure the package was built correctly.",
        workerScript
      );
    }

    _logger.LogDebug("Starting Python worker: {Exe} {Script}", pyExe, workerScript);

    var psi = new ProcessStartInfo
    {
      FileName = pyExe,
      UseShellExecute = false,
      RedirectStandardInput = true,
      RedirectStandardOutput = true,
      RedirectStandardError = false, // let stderr pass through to parent for visibility
      CreateNoWindow = true,
    };
    psi.ArgumentList.Add(workerScript);

    // ── IConfiguration → env-var bridge ──────────────────────────────────
    // Inject the flattened section *before* Process.Start. The parent
    // environment is inherited by default (UseShellExecute=false), so
    // standard variables like PATH and HOME pass through unchanged; the
    // flattener's entries layer on top using .NET's native :→__ rule.
    // Pipeline-side Python code reads them via flowthru.config (or any
    // other env-var consumer of the developer's choice).
    var flattenedEnv = _flattener.Flatten();
    if (flattenedEnv.Count > 0)
    {
      foreach (var (key, value) in flattenedEnv)
      {
        psi.EnvironmentVariables[key] = value;
      }
      _logger.LogDebug(
        "Injected {Count} configuration env var(s) into Python subprocess.",
        flattenedEnv.Count
      );
    }

    _worker =
      Process.Start(psi)
      ?? throw new InvalidOperationException("Failed to start Python worker process.");

    _stdin = _worker.StandardInput;
    _stdin.AutoFlush = true;
    _stdout = _worker.StandardOutput;

    // Build init message: sys.path includes configured search paths + the base directory
    // (so flowthru_worker.py can import _flowthru_arrow from the same directory)
    var sysPaths = PythonEnvironmentResolver
      .ResolveModuleSearchPaths(_options)
      .Concat(new[] { AppContext.BaseDirectory })
      .Distinct()
      .Select(p => (JsonNode?)JsonValue.Create(p))
      .ToArray();

    var init = new JsonObject { ["type"] = "init", ["sys_path"] = new JsonArray(sysPaths) };
    _stdin.WriteLine(init.ToJsonString());

    var readyLine =
      _stdout.ReadLine()
      ?? throw new InvalidOperationException(
        "Python worker exited before sending 'ready' message."
      );

    var readyMsg = JsonNode.Parse(readyLine);
    if (readyMsg?["status"]?.GetValue<string>() != "ready")
    {
      throw new InvalidOperationException(
        $"Python worker did not become ready. Response: {readyLine}"
      );
    }

    var pyExePath = readyMsg?["python_executable"]?.GetValue<string>() ?? "(unknown)";
    var pyPrefix = readyMsg?["python_prefix"]?.GetValue<string>() ?? "(unknown)";
    var sysPathEntries =
      readyMsg
        ?["sys_path"]?.AsArray()
        .Select(n => n?.GetValue<string>())
        .Where(s => s != null)
        .ToList() ?? [];

    _logger.LogInformation(
      "Python worker ready (pid={Pid}). executable={Exe} prefix={Prefix}",
      _worker.Id,
      pyExePath,
      pyPrefix
    );
    _logger.LogDebug("Python worker sys.path: {SysPath}", string.Join(", ", sysPathEntries));
  }

  private JsonObject SendRequest(JsonObject request)
  {
    if (_stdin == null || _stdout == null)
    {
      throw new InvalidOperationException("Python worker is not running.");
    }

    lock (_lock)
    {
      _stdin.WriteLine(request.ToJsonString());

      var responseLine =
        _stdout.ReadLine()
        ?? throw new InvalidOperationException("Python worker closed the connection unexpectedly.");

      return JsonNode.Parse(responseLine) as JsonObject
        ?? throw new InvalidOperationException(
          $"Invalid response from Python worker: {responseLine}"
        );
    }
  }

  // ── Type classification ───────────────────────────────────────────────────────────────

  private static string ClassifyType(Type type)
  {
    if (IsValueTuple(type))
    {
      return "multi";
    }

    if (type == typeof(byte[]))
    {
      return "bytes";
    }

    if (IsDirectoryType(type))
    {
      return "directory";
    }

    if (IsEnumerableSchema(type))
    {
      return "tabular";
    }

    return "scalar";
  }

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

  // ── Dtype spec helpers ────────────────────────────────────────────────────────────────

  /// <summary>
  /// Builds a JSON object containing the dtype spec for a tabular output type.
  /// </summary>
  private static JsonNode BuildDtypeSpecJson(Type collectionType)
  {
    var elemType = collectionType.GetGenericArguments()[0];
    var buildMethod = typeof(ArrowSchemaMapper)
      .GetMethod(nameof(ArrowSchemaMapper.BuildDtypeSpecDictionary))!
      .MakeGenericMethod(elemType);
    var dict = (Dictionary<string, string>)buildMethod.Invoke(null, null)!;
    return JsonNode.Parse(JsonSerializer.Serialize(dict))!;
  }

  /// <summary>
  /// Builds the output_directory_spec object for <see cref="Flowthru.Data.Storage.DirectoryOf{T}"/>
  /// outputs. Tells the worker the inner kind (and dtype spec when inner is tabular) so it can
  /// encode each dict entry correctly.
  /// </summary>
  private static JsonObject BuildDirectorySpecJson(Type directoryType)
  {
    var innerType = directoryType.GetGenericArguments()[0];
    var innerKind = ClassifyType(innerType);
    var spec = new JsonObject { ["inner_kind"] = innerKind };
    if (innerKind == "tabular")
      spec["dtype_spec"] = BuildDtypeSpecJson(innerType);
    return spec;
  }

  /// <summary>
  /// Builds the output_element_specs array for multi-output (ValueTuple) types.
  /// Each entry describes the kind and (for tabular elements) the dtype spec.
  /// </summary>
  private static JsonArray BuildMultiElementSpecs(Type tupleType)
  {
    var elementTypes = tupleType.GetGenericArguments();
    var arr = new JsonArray();
    foreach (var elemType in elementTypes)
    {
      var kind = ClassifyType(elemType);
      var spec = new JsonObject { ["kind"] = kind };
      if (kind == "tabular")
      {
        spec["dtype_spec"] = BuildDtypeSpecJson(elemType);
      }

      arr.Add(spec);
    }
    return arr;
  }

  // ── Encoding ──────────────────────────────────────────────────────────────────────────

  private static string EncodeValue(object value, Type type, string kind) =>
    kind switch
    {
      "scalar" => JsonSerializer.Serialize(value, type),
      "bytes" => Convert.ToBase64String((byte[])value),
      "tabular" => EncodeTabular(value, type),
      "multi" => EncodeMulti(value, type),
      "directory" => EncodeDirectory(value, type),
      _ => throw new NotSupportedException($"Unknown serialization kind: {kind}"),
    };

  private static string EncodeDirectory(object value, Type directoryType)
  {
    var innerType = directoryType.GetGenericArguments()[0];
    var innerKind = ClassifyType(innerType);

    var entries = new JsonObject();
    foreach (var kvp in (System.Collections.IEnumerable)value)
    {
      var keyProp = kvp.GetType().GetProperty("Key")!;
      var valueProp = kvp.GetType().GetProperty("Value")!;
      var key = (string)keyProp.GetValue(kvp)!;
      var inner = valueProp.GetValue(kvp)!;
      entries[key] = EncodeValue(inner, innerType, innerKind);
    }

    return new JsonObject { ["inner_kind"] = innerKind, ["entries"] = entries }.ToJsonString();
  }

  private static string EncodeTabular(object value, Type collectionType)
  {
    var elemType = collectionType.GetGenericArguments()[0];
    var toRecordBatch = typeof(ArrowMarshaller)
      .GetMethod(nameof(ArrowMarshaller.ToRecordBatch))!
      .MakeGenericMethod(elemType);
    var batch = (RecordBatch)InvokeUnwrapping(toRecordBatch, null, new[] { value })!;
    return Convert.ToBase64String(ArrowMarshaller.ToIpcBuffer(batch));
  }

  private static string EncodeMulti(object tuple, Type tupleType)
  {
    if (tuple is not ITuple t)
    {
      throw new InvalidOperationException(
        $"Expected ITuple for multi-I/O, got {tuple.GetType().Name}"
      );
    }

    var elementTypes = tupleType.GetGenericArguments();
    var arr = new JsonArray();
    for (int i = 0; i < t.Length; i++)
    {
      var elemKind = ClassifyType(elementTypes[i]);
      arr.Add(
        new JsonObject
        {
          ["kind"] = elemKind,
          ["value"] = EncodeValue(t[i]!, elementTypes[i], elemKind),
        }
      );
    }
    return arr.ToJsonString();
  }

  // ── Decoding ──────────────────────────────────────────────────────────────────────────

  private static TOutput DecodeValue<TOutput>(string payload, Type type, string kind) =>
    kind switch
    {
      "scalar" => (TOutput)JsonSerializer.Deserialize(payload, type)!,
      "bytes" => (TOutput)(object)Convert.FromBase64String(payload),
      "tabular" => DecodeTabular<TOutput>(payload, type),
      "multi" => DecodeMulti<TOutput>(payload, type),
      "directory" => DecodeDirectory<TOutput>(payload, type),
      _ => throw new NotSupportedException($"Unknown deserialization kind: {kind}"),
    };

  private static TOutput DecodeDirectory<TOutput>(string payload, Type directoryType)
  {
    var innerType = directoryType.GetGenericArguments()[0];
    var envelope = JsonNode.Parse(payload)!.AsObject();
    var innerKind = envelope["inner_kind"]!.GetValue<string>();
    var entriesJson = envelope["entries"]!.AsObject();

    var dictType = typeof(Dictionary<,>).MakeGenericType(typeof(string), innerType);
    var dict = (System.Collections.IDictionary)Activator.CreateInstance(dictType)!;

    var decodeMethod = typeof(SubprocessPythonExecutor)
      .GetMethod(nameof(DecodeValue), BindingFlags.NonPublic | BindingFlags.Static)!
      .MakeGenericMethod(innerType);

    foreach (var kvp in entriesJson)
    {
      var encoded = kvp.Value!.GetValue<string>();
      var decoded = InvokeUnwrapping(decodeMethod, null, new object[] { encoded, innerType, innerKind });
      dict[kvp.Key] = decoded;
    }

    return (TOutput)Activator.CreateInstance(directoryType, dict)!;
  }

  private static TOutput DecodeTabular<TOutput>(string base64, Type collectionType)
  {
    var elemType = collectionType.GetGenericArguments()[0];
    var batch = ArrowMarshaller.FromIpcBuffer(Convert.FromBase64String(base64));
    var fromRecordBatch = typeof(ArrowMarshaller)
      .GetMethod(nameof(ArrowMarshaller.FromRecordBatch))!
      .MakeGenericMethod(elemType);
    return (TOutput)InvokeUnwrapping(fromRecordBatch, null, new object[] { batch })!;
  }

  private static TOutput DecodeMulti<TOutput>(string jsonArray, Type tupleType)
  {
    var elementTypes = tupleType.GetGenericArguments();
    var arr = JsonNode.Parse(jsonArray)!.AsArray();
    var elements = new object?[elementTypes.Length];
    for (int i = 0; i < elementTypes.Length; i++)
    {
      var entry = arr[i]!.AsObject();
      var elemKind = entry["kind"]!.GetValue<string>();
      var elemValue = entry["value"]!.GetValue<string>();
      // Decode each element using its own type — use reflection to call the generic method
      var decodeMethod = typeof(SubprocessPythonExecutor)
        .GetMethod(
          nameof(DecodeValue),
          System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static
        )!
        .MakeGenericMethod(elementTypes[i]);
      elements[i] = InvokeUnwrapping(
        decodeMethod,
        null,
        new object[] { elemValue, elementTypes[i], elemKind }
      );
    }
    return (TOutput)Activator.CreateInstance(tupleType, elements)!;
  }

  // ── Dispose ───────────────────────────────────────────────────────────────────────────

  /// <inheritdoc/>
  public void Dispose()
  {
    if (_disposed)
    {
      return;
    }

    _disposed = true;

    try
    {
      if (_stdin != null && _worker is { HasExited: false })
      {
        try
        {
          _stdin.WriteLine(new JsonObject { ["type"] = "shutdown" }.ToJsonString());
          _stdin.Flush();
        }
        catch
        { /* worker may already be gone */
        }
      }
    }
    finally
    {
      try
      {
        _stdin?.Dispose();
      }
      catch { }

      if (_worker != null)
      {
        try
        {
          _worker.WaitForExit(2000);
        }
        catch { }
        try
        {
          if (!_worker.HasExited)
          {
            _worker.Kill();
          }
        }
        catch { }
        _worker.Dispose();
      }

      _stdin = null;
      _worker = null;
    }
  }

  // ── Typed-error classification ────────────────────────────────────────

  /// <summary>
  /// Map an <c>InvalidOperationException</c> raised inside
  /// <see cref="InvokeCore"/> to a typed
  /// <see cref="PythonRuntimeError"/>. The worker reports failures
  /// via <c>{ status: "error", message }</c>; we use simple message
  /// scanning to pick the most specific case. Unrecognised messages
  /// fall through to <see cref="PythonRuntimeError.WorkerError"/>.
  /// </summary>
  private static PythonRuntimeError ClassifyInvokeFailure(
    string moduleName,
    string functionName,
    string message
  )
  {
    if (LooksLikeMarshalling(message))
      return new PythonRuntimeError.MarshallingFailed(
        Source: $"{moduleName}.{functionName}",
        Detail: message
      );

    return new PythonRuntimeError.WorkerError(moduleName, functionName, message);
  }

  /// <summary>
  /// Map a validate-step failure message to the appropriate typed
  /// <see cref="PythonRuntimeError"/> case. The worker emits distinct
  /// substrings for module-import vs function-not-found vs
  /// decorator-absent, so a small heuristic on message body suffices.
  /// </summary>
  private static PythonRuntimeError ClassifyValidateFailure(
    string moduleName,
    string functionName,
    string message
  )
  {
    if (message.IndexOf("ImportError", StringComparison.OrdinalIgnoreCase) >= 0
      || message.IndexOf("ModuleNotFoundError", StringComparison.OrdinalIgnoreCase) >= 0
      || message.IndexOf("could not be imported", StringComparison.OrdinalIgnoreCase) >= 0)
    {
      return new PythonRuntimeError.ModuleNotFound(moduleName, message);
    }

    if (message.IndexOf("AttributeError", StringComparison.OrdinalIgnoreCase) >= 0
      || message.IndexOf("not found in module", StringComparison.OrdinalIgnoreCase) >= 0
      || message.IndexOf("has no attribute", StringComparison.OrdinalIgnoreCase) >= 0)
    {
      return new PythonRuntimeError.FunctionMissing(moduleName, functionName);
    }

    if (message.IndexOf("@step", StringComparison.OrdinalIgnoreCase) >= 0
      || message.IndexOf("__flowthru_inputs__", StringComparison.Ordinal) >= 0
      || message.IndexOf("decorator", StringComparison.OrdinalIgnoreCase) >= 0)
    {
      return new PythonRuntimeError.DecoratorAbsent(moduleName, functionName);
    }

    return new PythonRuntimeError.WorkerError(moduleName, functionName, message);
  }

  private static bool LooksLikeMarshalling(string message) =>
    message.IndexOf("marshal", StringComparison.OrdinalIgnoreCase) >= 0
      || message.IndexOf("dtype", StringComparison.OrdinalIgnoreCase) >= 0
      || message.IndexOf("Arrow", StringComparison.OrdinalIgnoreCase) >= 0
      || message.IndexOf("base64", StringComparison.OrdinalIgnoreCase) >= 0
      || message.IndexOf("not supported for Arrow", StringComparison.OrdinalIgnoreCase) >= 0;

  /// <summary>
  /// Invoke a <see cref="MethodInfo"/> and, on failure, unwrap the
  /// <see cref="TargetInvocationException"/> envelope so the real
  /// inner exception escapes with its original type, message and
  /// stack trace intact. Without this, every reflection-driven
  /// call site here yields the useless
  /// <c>"Exception has been thrown by the target of an invocation."</c>
  /// at the <see cref="FlowIO{A}.LiftAsync"/> boundary, hiding the
  /// real Arrow marshalling failure from the user.
  /// </summary>
  private static object? InvokeUnwrapping(MethodInfo method, object? target, object?[]? args)
  {
    try
    {
      return method.Invoke(target, args);
    }
    catch (TargetInvocationException tie) when (tie.InnerException is not null)
    {
      ExceptionDispatchInfo.Capture(tie.InnerException).Throw();
      throw; // unreachable
    }
  }

  /// <summary>
  /// Render an exception (and any inner chain) into a single
  /// human-readable detail string. Used when a non-IOE escapes
  /// <see cref="FlowIO{A}.LiftAsync"/> — typically a
  /// <c>NotSupportedException</c> from <see cref="ArrowMarshaller"/>
  /// — so the surfaced <see cref="PythonRuntimeError"/> carries the
  /// real type name and message rather than a generic wrapper.
  /// </summary>
  private static string FormatInnerExceptionDetail(Exception ex)
  {
    var sb = new System.Text.StringBuilder();
    var cursor = ex;
    var depth = 0;
    while (cursor is not null)
    {
      if (depth > 0) sb.Append(" → ");
      sb.Append(cursor.GetType().Name).Append(": ").Append(cursor.Message);
      cursor = cursor.InnerException;
      depth++;
    }
    return sb.ToString();
  }
}
