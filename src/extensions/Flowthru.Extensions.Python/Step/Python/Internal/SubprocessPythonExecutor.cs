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
/// Tabular data is exchanged as Arrow IPC files on disk; raw byte arrays as
/// binary files. The JSON envelope carries file paths for bulk kinds and
/// inline values for scalars.
/// </para>
/// <para>
/// The worker script (<c>flowthru_worker.py</c>) must be present in
/// <see cref="AppContext.BaseDirectory"/>.
/// </para>
/// </remarks>
public sealed class SubprocessPythonExecutor : IPythonExecutor, IFlowResourceProvider, IDisposable
{
  private readonly PythonRuntimeOptions _options;
  private readonly IPythonConfigurationFlattener _flattener;
  private readonly IPythonLauncher _launcher;
  private readonly ILogger _logger;

  private Process? _worker;
  private StreamWriter? _stdin;
  private StreamReader? _stdout;
  private CancellationTokenSource? _stderrReaderCts;
  private Task? _stderrReaderTask;
  private readonly object _lock = new();
  private volatile bool _started;
  private bool _disposed;

  // Per-flow transit directory for Arrow IPC / raw binary file exchange.
  // Acquired by the FlowResource bracket before pre-flight; released
  // (deleted on success, preserved on failure) after post-run.
  private string? _transitDir;
  private int _invocationCounter;

  // Cached interpreter version string, probed lazily on first
  // GetInterpreterVersion() call. _interpreterVersionProbed is the
  // memoization guard — separate from _interpreterVersion because the
  // probe legitimately returns null (e.g., venv missing) and we still
  // want to avoid re-probing.
  private string? _interpreterVersion;
  private bool _interpreterVersionProbed;
  private readonly object _interpreterVersionLock = new();

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
  /// <param name="launcher">
  /// Strategy that constructs the worker's
  /// <see cref="ProcessStartInfo"/>. Defaults to
  /// <see cref="DirectPythonLauncher"/> in production via the
  /// <c>TryAddSingleton</c> registered by <c>UsePython()</c>; tests
  /// pass an explicit instance.
  /// </param>
  /// <param name="logger">
  /// The engine's shared <see cref="ILogger"/> — registered as a
  /// singleton under category <c>"Flowthru"</c> by
  /// <c>AddFlowthru</c> per ADR-0005. Worker stderr lines bridge
  /// through this logger via <see cref="StderrLineClassifier"/>, so
  /// Python step output appears alongside engine and C# step logs in
  /// the same stream.
  /// </param>
  /// <exception cref="ArgumentNullException"></exception>
  public SubprocessPythonExecutor(
    IOptions<PythonRuntimeOptions> options,
    IPythonConfigurationFlattener flattener,
    IPythonLauncher launcher,
    ILogger logger
  )
  {
    _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    _flattener = flattener ?? throw new ArgumentNullException(nameof(flattener));
    _launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  // ── IFlowResourceProvider ───────────────────────────────────────────────────────────────

  public IFlowResource? FlowResource => Flowthru.Prelude.FlowResource.Make<string>(
    acquire: FlowIO.LiftAsync<string>(ct =>
    {
      var dir = Path.Combine(Path.GetTempPath(), "flowthru", "transit", Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(dir);
      _transitDir = dir;
      _invocationCounter = 0;
      _logger.LogDebug("Transit directory acquired: {TransitDir}", dir);
      return Task.FromResult(dir);
    }, source: "SubprocessPythonExecutor.TransitDir.Acquire"),
    release: (dir, bodyError) => FlowIO.LiftAsync<FlowUnit>(ct =>
    {
      if (bodyError is not null)
      {
        _logger.LogWarning(
          "Transit directory preserved for debugging (flow failed): {TransitDir}", dir);
      }
      else if (Directory.Exists(dir))
      {
        Directory.Delete(dir, recursive: true);
        _logger.LogDebug("Transit directory cleaned up: {TransitDir}", dir);
      }
      _transitDir = null;
      return Task.FromResult(FlowUnit.Default);
    }, source: "SubprocessPythonExecutor.TransitDir.Release")
  );

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

    var invocationId = Interlocked.Increment(ref _invocationCounter);
    var invocationDir = _transitDir is not null
      ? Path.Combine(_transitDir, invocationId.ToString("D4"))
      : Path.Combine(Path.GetTempPath(), "flowthru", "transit", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(invocationDir);

    var inputKind = ClassifyType(typeof(TInput));
    var outputKind = ClassifyType(typeof(TOutput));

    var req = new JsonObject
    {
      ["type"] = "invoke",
      ["module"] = moduleName,
      ["function"] = functionName,
      ["input_type"] = inputKind,
      ["input"] = EncodeValue(input!, typeof(TInput), inputKind, invocationDir, "input"),
      ["output_type"] = outputKind,
      ["transit_dir"] = invocationDir,
    };

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
  internal static Validated<PreFlightError, FlowUnit> TranslateInspectorResult(
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

  internal static List<string> ExtractStringList(JsonObject root, string key)
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

  /// <inheritdoc/>
  public string? GetInterpreterVersion()
  {
    // Double-checked-lock memoization — AddPythonStep calls can race
    // during flow construction, but the probe must run exactly once.
    if (_interpreterVersionProbed) return _interpreterVersion;
    lock (_interpreterVersionLock)
    {
      if (_interpreterVersionProbed) return _interpreterVersion;
      _interpreterVersion = ProbeInterpreterVersion();
      _interpreterVersionProbed = true;
      return _interpreterVersion;
    }
  }

  /// <summary>
  /// One-shot <c>python --version</c> probe. Runs in a short-lived
  /// subprocess so we don't depend on the long-running worker being
  /// started yet (cache plans build at pre-flight, often before any
  /// step actually executes). Returns null on any failure path —
  /// downstream cache logic treats null as "uncacheable".
  /// </summary>
  private string? ProbeInterpreterVersion()
  {
    string pyExe;
    try
    {
      pyExe = PythonEnvironmentResolver.ResolvePythonExe(_options);
    }
    catch
    {
      // Venv not configured / venv path missing. Without the
      // interpreter we can't form a stable identity; treat as
      // uncacheable.
      return null;
    }

    try
    {
      var psi = new ProcessStartInfo
      {
        FileName = pyExe,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true,
      };
      psi.ArgumentList.Add("--version");

      using var proc = Process.Start(psi);
      if (proc is null) return null;

      // `python --version` is sub-millisecond on a warm cache; a
      // five-second ceiling is generous for cold disk reads and
      // virtualised filesystems without ever masking a real hang.
      if (!proc.WaitForExit(TimeSpan.FromSeconds(5)))
      {
        try { proc.Kill(entireProcessTree: true); }
        catch { /* best-effort */ }
        return null;
      }
      if (proc.ExitCode != 0) return null;

      // Python 2 wrote --version to stderr; Python 3 writes to stdout.
      // Concatenate both so the probe survives either convention. Both
      // streams are bounded (the executable prints one short line).
      var stdout = proc.StandardOutput.ReadToEnd().Trim();
      var stderr = proc.StandardError.ReadToEnd().Trim();
      var combined = string.IsNullOrEmpty(stdout) ? stderr : stdout;
      return string.IsNullOrWhiteSpace(combined) ? null : combined;
    }
    catch
    {
      // Permissions, missing binary, missing shared libs — any failure
      // path collapses to null. The cache treats it as cache-miss.
      return null;
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

    _logger.LogDebug(
      "Starting Python worker: {Exe} {Script} via {Launcher}",
      pyExe,
      workerScript,
      _launcher.Identity
    );

    // ── IConfiguration → env-var bridge ──────────────────────────────────
    // Flatten the configured section *before* PSI construction. The
    // parent environment is inherited by default (UseShellExecute=false
    // inside the launcher), so standard variables like PATH and HOME
    // pass through unchanged; the flattener's entries layer on top
    // using .NET's native :→__ rule. Pipeline-side Python code reads
    // them via flowthru.config (or any other env-var consumer of the
    // developer's choice). The launcher owns the final merge — launchers
    // that set their own rank vars (RANK / WORLD_SIZE / etc.) overlay
    // on top of the flattened section without losing either.
    var flattenedEnv = _flattener.Flatten();
    var psi = _launcher.Build(pyExe, workerScript, flattenedEnv);
    if (flattenedEnv.Count > 0)
    {
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

    // Bridge stderr → ILogger. The reader runs for the worker's
    // lifetime; Dispose() cancels the CTS and awaits the task. Doing
    // this before sending the init message ensures any startup
    // diagnostics from the worker (import errors, sys.path issues)
    // are captured rather than buffered.
    _stderrReaderCts = new CancellationTokenSource();
    _stderrReaderTask = Task.Run(
      () => ReadStderrLoopAsync(_worker.StandardError, _stderrReaderCts.Token)
    );

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

  internal static string ClassifyType(Type type)
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

  internal static bool IsDirectoryType(Type type) =>
    type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Flowthru.Data.Storage.DirectoryOf<>);

  internal static bool IsValueTuple(Type type)
  {
    if (!type.IsValueType || !type.IsGenericType)
    {
      return false;
    }

    return type.GetGenericTypeDefinition()
        .FullName?.StartsWith("System.ValueTuple`", StringComparison.Ordinal) ?? false;
  }

  internal static bool IsEnumerableSchema(Type type)
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
  internal static JsonNode BuildDtypeSpecJson(Type collectionType)
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
  internal static JsonObject BuildDirectorySpecJson(Type directoryType)
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
  internal static JsonArray BuildMultiElementSpecs(Type tupleType)
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

  internal static string EncodeValue(
    object value, Type type, string kind, string transitDir, string filePrefix) =>
    kind switch
    {
      "scalar" => JsonSerializer.Serialize(value, type),
      "bytes" => EncodeBytes((byte[])value, transitDir, filePrefix),
      "tabular" => EncodeTabular(value, type, transitDir, filePrefix),
      "multi" => EncodeMulti(value, type, transitDir, filePrefix),
      "directory" => EncodeDirectory(value, type, transitDir, filePrefix),
      _ => throw new NotSupportedException($"Unknown serialization kind: {kind}"),
    };

  internal static string EncodeDirectory(
    object value, Type directoryType, string transitDir, string filePrefix)
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
      var entryDir = Path.Combine(transitDir, $"{filePrefix}_dir");
      Directory.CreateDirectory(entryDir);
      entries[key] = EncodeValue(inner, innerType, innerKind, entryDir, key);
    }

    return new JsonObject { ["inner_kind"] = innerKind, ["entries"] = entries }.ToJsonString();
  }

  internal static string EncodeTabular(
    object value, Type collectionType, string transitDir, string filePrefix)
  {
    var elemType = collectionType.GetGenericArguments()[0];
    var toRecordBatch = typeof(ArrowMarshaller)
      .GetMethod(nameof(ArrowMarshaller.ToRecordBatch))!
      .MakeGenericMethod(elemType);
    var batch = (RecordBatch)InvokeUnwrapping(toRecordBatch, null, new[] { value })!;
    var ipcBytes = ArrowMarshaller.ToIpcBuffer(batch);
    var filePath = Path.Combine(transitDir, $"{filePrefix}.arrow");
    File.WriteAllBytes(filePath, ipcBytes);
    return filePath;
  }

  internal static string EncodeBytes(byte[] value, string transitDir, string filePrefix)
  {
    var filePath = Path.Combine(transitDir, $"{filePrefix}.bin");
    File.WriteAllBytes(filePath, value);
    return filePath;
  }

  internal static string EncodeMulti(
    object tuple, Type tupleType, string transitDir, string filePrefix)
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
          ["value"] = EncodeValue(t[i]!, elementTypes[i], elemKind, transitDir, $"{filePrefix}_{i}"),
        }
      );
    }
    return arr.ToJsonString();
  }

  // ── Decoding ──────────────────────────────────────────────────────────────────────────

  internal static TOutput DecodeValue<TOutput>(string payload, Type type, string kind) =>
    kind switch
    {
      "scalar" => (TOutput)JsonSerializer.Deserialize(payload, type)!,
      "bytes" => (TOutput)(object)File.ReadAllBytes(payload),
      "tabular" => DecodeTabular<TOutput>(payload, type),
      "multi" => DecodeMulti<TOutput>(payload, type),
      "directory" => DecodeDirectory<TOutput>(payload, type),
      _ => throw new NotSupportedException($"Unknown deserialization kind: {kind}"),
    };

  internal static TOutput DecodeDirectory<TOutput>(string payload, Type directoryType)
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

  internal static TOutput DecodeTabular<TOutput>(string filePath, Type collectionType)
  {
    var elemType = collectionType.GetGenericArguments()[0];
    var batch = ArrowMarshaller.FromIpcBuffer(File.ReadAllBytes(filePath));
    var fromRecordBatch = typeof(ArrowMarshaller)
      .GetMethod(nameof(ArrowMarshaller.FromRecordBatch))!
      .MakeGenericMethod(elemType);
    return (TOutput)InvokeUnwrapping(fromRecordBatch, null, new object[] { batch })!;
  }

  internal static TOutput DecodeMulti<TOutput>(string jsonArray, Type tupleType)
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

  /// <summary>
  /// Drain the worker's stderr, line by line, into the engine's
  /// shared <see cref="ILogger"/>. Runs on a background task for the
  /// worker's lifetime; <see cref="Dispose"/> cancels the CTS and
  /// awaits the task.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The classifier (<see cref="StderrLineClassifier"/>) decides each
  /// line's <see cref="LogLevel"/>: structured frames the Python
  /// worker emits via its <c>_FlowthruJsonLogHandler</c> carry the
  /// embedded level; raw <c>print()</c> output defaults to
  /// <see cref="LogLevel.Information"/>; tracebacks elevate to
  /// <see cref="LogLevel.Error"/>.
  /// </para>
  /// <para>
  /// Exceptions raised by the stream (worker exited, pipe broke) are
  /// swallowed — the bridge is best-effort observation, not a hard
  /// dependency of step execution. The reader returns and the task
  /// completes; subsequent <c>Dispose</c> still runs cleanly.
  /// </para>
  /// </remarks>
  private async Task ReadStderrLoopAsync(StreamReader stderr, CancellationToken cancellationToken)
  {
    try
    {
      while (!cancellationToken.IsCancellationRequested)
      {
        var line = await stderr.ReadLineAsync(cancellationToken).ConfigureAwait(false);
        if (line is null)
        {
          // End of stream — worker closed stderr (likely exited).
          break;
        }

        var (level, message) = StderrLineClassifier.Classify(line);
        _logger.Log(level, "{Message}", message);
      }
    }
    catch (OperationCanceledException)
    {
      // Dispose called — expected.
    }
    catch (Exception ex)
    {
      // Defensive: don't let a stderr-read crash escape the background
      // task. Surface it once at Warning so the bridge failure is
      // visible, then exit.
      _logger.LogWarning(ex, "Python stderr reader exited unexpectedly");
    }
  }

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

      // Cancel the stderr reader and wait briefly for it to drain. The
      // reader is best-effort, so we don't propagate exceptions or
      // wait forever — a short timeout matches the worker exit budget
      // above. The reader naturally exits when the worker closes
      // stderr; the cancellation just covers the edge case where the
      // OS holds the pipe open past process exit.
      try
      {
        _stderrReaderCts?.Cancel();
        _stderrReaderTask?.Wait(TimeSpan.FromSeconds(2));
      }
      catch { /* best-effort cleanup */ }
      finally
      {
        _stderrReaderCts?.Dispose();
        _stderrReaderCts = null;
        _stderrReaderTask = null;
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
  internal static PythonRuntimeError ClassifyInvokeFailure(
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
  internal static PythonRuntimeError ClassifyValidateFailure(
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

  internal static bool LooksLikeMarshalling(string message) =>
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
  internal static string FormatInnerExceptionDetail(Exception ex)
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
