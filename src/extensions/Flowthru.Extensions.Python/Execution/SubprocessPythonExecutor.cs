using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Apache.Arrow;
using Flowthru.Core.Data.Validation;
using Flowthru.Extensions.Python.Marshalling;
using Flowthru.Extensions.Python.Runtime;
using Flowthru.Extensions.Python.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Flowthru.Extensions.Python.Execution;

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
  public TOutput Invoke<TInput, TOutput>(string moduleName, string functionName, TInput input)
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
  public PythonStepMetadata ValidateStep(string moduleName, string functionName)
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

    // Extract the optional "services" array from the validate response.
    // Empty / missing → no service dependencies declared.
    var services = ExtractStringList(resp, "services");
    return services.Count == 0
      ? PythonStepMetadata.Empty
      : new PythonStepMetadata(services);
  }

  /// <inheritdoc />
  public ValidationResult InvokeInspector(PythonServiceRegistration registration)
  {
    if (registration is null)
    {
      throw new ArgumentNullException(nameof(registration));
    }

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
      // The worker raised before/during inspector invocation — surface as a
      // generic InspectionFailure so the preflight loop's diagnostic flow
      // treats it consistently with C#-side inspector exceptions.
      return ValidationResult.Failure(
        catalogKey: registration.ServiceClassPath,
        errorType: ValidationErrorType.InspectionFailure,
        message: $"Python inspector for '{registration.ServiceClassPath}' failed: {msg}"
      );
    }

    if (resp["result"] is not JsonObject result)
    {
      return ValidationResult.Failure(
        catalogKey: registration.ServiceClassPath,
        errorType: ValidationErrorType.InspectionFailure,
        message:
          $"Python inspector for '{registration.ServiceClassPath}' returned a "
          + "malformed payload (no 'result' object)."
      );
    }

    return TranslateInspectorResult(result, registration.ServiceClassPath);
  }

  /// <summary>
  /// Translates the worker's <c>{success, source, error_type, message}</c>
  /// dict into a C# <see cref="ValidationResult"/>. Unknown
  /// <c>error_type</c> strings (the Python side's enum is broader than
  /// C#'s catalog-validation enum) fall through to
  /// <see cref="ValidationErrorType.InspectionFailure"/> with the original
  /// string preserved in the result's <c>Details</c>.
  /// </summary>
  private static ValidationResult TranslateInspectorResult(
    JsonObject result,
    string serviceClassPath
  )
  {
    var success = result["success"]?.GetValue<bool>() ?? false;
    if (success)
    {
      return ValidationResult.Success();
    }

    var source = result["source"]?.GetValue<string>();
    if (string.IsNullOrWhiteSpace(source))
    {
      source = serviceClassPath;
    }
    var message = result["message"]?.GetValue<string>() ?? "(no message)";
    var errorTypeText = result["error_type"]?.GetValue<string>() ?? "InspectionFailure";

    var errorType = Enum.TryParse<ValidationErrorType>(
      errorTypeText,
      ignoreCase: true,
      out var parsed
    )
      ? parsed
      : ValidationErrorType.InspectionFailure;

    // When we fell back to InspectionFailure on an unknown Python-side
    // category, preserve the original string in Details so log readers can
    // still see it ("Forbidden", "Configuration", etc.).
    string? details = errorType == ValidationErrorType.InspectionFailure
      && !string.Equals(errorTypeText, "InspectionFailure", StringComparison.OrdinalIgnoreCase)
      ? $"PythonErrorType={errorTypeText}"
      : null;

    return ValidationResult.Failure(
      catalogKey: source,
      errorType: errorType,
      message: message,
      details: details
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
    type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Flowthru.Core.Data.Directory<>);

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
  /// Builds the output_directory_spec object for <see cref="Flowthru.Core.Data.Directory{T}"/>
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
    var batch = (RecordBatch)toRecordBatch.Invoke(null, new[] { value })!;
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
      var decoded = decodeMethod.Invoke(null, new object[] { encoded, innerType, innerKind });
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
    return (TOutput)fromRecordBatch.Invoke(null, new object[] { batch })!;
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
      elements[i] = decodeMethod.Invoke(
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
}
