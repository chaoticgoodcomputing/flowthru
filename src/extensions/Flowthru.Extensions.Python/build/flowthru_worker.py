"""
Flowthru Python worker process.

Receives newline-delimited JSON requests over stdin and writes responses to stdout.
Spawned by SubprocessPythonExecutor — one process per FlowthruService instance.

Protocol
--------
Init (first message):
    {"type": "init", "sys_path": ["/path1", "/path2"]}
    Response: {"status": "ready"}

Validate:
    {"type": "validate", "module": "...", "function": "..."}
    Response: {"status": "ok", "services": ["module.Class", ...]}
            | {"status": "error", "message": "..."}

Inspect:
    {
      "type": "inspect",
      "service_module": "...",
      "service_class": "...",
      "inspector_module": "...",
      "inspector_function": "inspect"            # optional; default "inspect"
    }
    Response: {"status": "ok", "result": {"success": bool, "source": str,
                                          "error_type": str, "message": str}}
            | {"status": "error", "message": "..."}

Invoke:
    {
      "type": "invoke",
      "module": "...",
      "function": "...",
      "input_type": "scalar" | "tabular" | "bytes" | "multi",
      "input": "<encoded>",
      "output_type": "scalar" | "tabular" | "bytes" | "multi",
      "output_dtype_spec": {"col": "dtype", ...},   // tabular output only
      "output_element_specs": [{"kind": "...", "dtype_spec": {...}}, ...] // multi output only
    }
    Response: {"status": "ok", "output": "<encoded>"} | {"status": "error", "message": "..."}

Shutdown:
    {"type": "shutdown"}
    (no response; process exits)

Encoding
--------
- scalar:    JSON (via json.dumps / json.loads)
- tabular:   base64-encoded Apache Arrow IPC stream bytes
- bytes:     base64-encoded raw bytes
- multi:     JSON array of {"kind": "<type>", "value": "<encoded>"}
- directory: JSON object {"inner_kind": "<type>", "entries": {"<path>": "<encoded>", ...}}
             — represents a Directory<T> where each entry is one file. The Python step
             receives / returns a plain dict[str, T] with paths as keys.
"""

import sys
import json
import base64
import importlib
import logging
import threading
import traceback
import contextlib

# Module cache — avoids re-importing on every invocation
_module_cache: dict = {}

# Loaded lazily after sys.path is configured
_flowthru_arrow = None

# Prefix the C# host (StderrLineClassifier) recognises when bridging
# stderr lines into the engine's shared ILogger. Any stderr line that
# starts with this marker is parsed as a JSON frame
# {"level": "...", "logger": "...", "msg": "..."} and forwarded at the
# embedded level; unmarked lines fall through to LogInformation (with
# a traceback heuristic that elevates to LogError). Keep this in sync
# with StderrLineClassifier.LogFramePrefix on the C# side.
_LOG_FRAME_PREFIX = "__flowthru_log__:"

# Process-wide lock serialising the (write, flush) pair so concurrent
# emitters can't interleave bytes on stderr — the C# reader expects
# one frame per line, and a torn write would surface as a malformed
# JSON parse on the host side. The lock covers our handler's emits;
# user code calling sys.stderr.write() directly still bypasses it
# (documented constraint — Python steps that spawn their own threads
# and write stderr directly are responsible for their own framing).
_STDERR_WRITE_LOCK = threading.Lock()


class _FlowthruJsonLogHandler(logging.Handler):
    """
    Bridges Python `logging` records to the C# host's ILogger by emitting
    a JSON frame on stderr per record. Installed on the root logger at
    worker startup so any user code that uses stdlib `logging`
    (`log.info(...)`, `log.warning(...)`, third-party libraries) flows
    through with its level preserved.

    Thread safety: `logging.Handler.handle()` already wraps each
    `emit()` call in `self.acquire()/release()`, so single-handler
    instances serialise their own emissions. The
    `_STDERR_WRITE_LOCK` additionally protects against torn writes
    when other threads in the worker (e.g. background tasks the user
    step spawned) write to stderr concurrently — only the path
    *through this handler* is protected; direct `sys.stderr.write()`
    calls from user code bypass the lock by design.

    `print()` calls and direct `sys.stderr.write()` calls bypass this
    handler — they reach the C# reader as unprefixed lines and bridge
    at LogInformation by default. See ADR-0005 (shared ILogger) and the
    Python extension's stderr bridge for the full contract.
    """

    def emit(self, record: logging.LogRecord) -> None:
        try:
            frame = {
                "level": record.levelname,
                "logger": record.name,
                "msg": record.getMessage(),
            }
            if record.exc_info:
                frame["exc"] = "".join(traceback.format_exception(*record.exc_info))
            line = _LOG_FRAME_PREFIX + json.dumps(frame) + "\n"
            with _STDERR_WRITE_LOCK:
                sys.stderr.write(line)
                sys.stderr.flush()
        except Exception:
            # A logging failure must never crash the worker. The record
            # is silently dropped — the host still has the raw stderr
            # stream for forensics.
            pass


def _install_log_bridge() -> None:
    """
    Install the Flowthru JSON log handler on the root logger and clear
    any pre-existing handlers so a later `logging.basicConfig(...)` call
    in user code becomes a no-op (basicConfig short-circuits when the
    root logger already has handlers — see Python docs). Root level is
    set to DEBUG so every record reaches our handler; the C# host's
    LogLevel.MinimumLevel decides what ultimately renders.
    """
    root = logging.getLogger()
    root.handlers.clear()
    root.addHandler(_FlowthruJsonLogHandler())
    root.setLevel(logging.DEBUG)


# ---------------------------------------------------------------------------
# Module resolution
# ---------------------------------------------------------------------------

def _get_module(module_name: str):
    if module_name not in _module_cache:
        _module_cache[module_name] = importlib.import_module(module_name)
    return _module_cache[module_name]


# ---------------------------------------------------------------------------
# Init
# ---------------------------------------------------------------------------

def _handle_init(msg: dict) -> None:
    # Preserve the C#-side ordering — the executor sends the user's
    # configured ModuleSearchPaths *first* and the framework's
    # AppContext.BaseDirectory *last*, so user paths must win over
    # framework defaults when Python resolves a module. Walking the
    # list in reverse and inserting at 0 gives us that ordering;
    # iterating forward with `insert(0, ...)` would silently reverse
    # the intent.
    incoming = msg.get("sys_path", [])
    for p in reversed(incoming):
        if p not in sys.path:
            sys.path.insert(0, p)


# ---------------------------------------------------------------------------
# Validate
# ---------------------------------------------------------------------------

def _handle_validate(msg: dict) -> dict:
    try:
        mod = _get_module(msg["module"])
        fn = msg["function"]
        if not hasattr(mod, fn):
            return {
                "status": "error",
                "message": f"Function '{fn}' not found in module '{msg['module']}'",
            }
        func = getattr(mod, fn)
        # Surface the full @step decorator metadata so the C# pre-flight hook
        # can verify schema agreement against the C# generic type parameters
        # AND register declared service dependencies. Missing attributes yield
        # empty lists (a function without @step would have failed the import
        # checks above before reaching here).
        if not hasattr(func, "__flowthru_inputs__") or not hasattr(func, "__flowthru_outputs__"):
            return {
                "status": "error",
                "message": (
                    f"Function '{msg['module']}.{fn}' is missing the @step decorator. "
                    "Decorate the function with @flowthru.step(inputs=[...], outputs=[...])."
                ),
            }
        inputs = list(getattr(func, "__flowthru_inputs__", []))
        outputs = list(getattr(func, "__flowthru_outputs__", []))
        services = list(getattr(func, "__flowthru_services__", []))
        return {
            "status": "ok",
            "inputs": inputs,
            "outputs": outputs,
            "services": services,
        }
    except Exception:
        return {"status": "error", "message": traceback.format_exc()}


# ---------------------------------------------------------------------------
# Inspect (sidecar service preflight)
# ---------------------------------------------------------------------------

def _handle_inspect(msg: dict) -> dict:
    """
    Run a sidecar inspector against a constructed service instance.

    Message shape:
        {
          "type": "inspect",
          "service_module":    "Services.pyannote_diarizer",
          "service_class":     "PyannoteDiarizer",
          "inspector_module":  "Services.pyannote_diarizer_inspector",
          "inspector_function": "inspect"          # optional, default "inspect"
        }

    Response shape on success:
        {"status": "ok", "result": {<ValidationResult.to_dict()>}}

    The service instance is constructed with no arguments — config is
    expected to flow in via env vars (see flowthru.config). The inspector
    function receives the constructed instance and returns a
    flowthru.ValidationResult.
    """
    try:
        service_mod = _get_module(msg["service_module"])
        service_cls_name = msg["service_class"]
        if not hasattr(service_mod, service_cls_name):
            return {
                "status": "error",
                "message": (
                    f"Service class '{service_cls_name}' not found in module "
                    f"'{msg['service_module']}'."
                ),
            }
        service_cls = getattr(service_mod, service_cls_name)
        svc = service_cls()  # zero-arg construction; config from env

        inspector_mod = _get_module(msg["inspector_module"])
        inspector_fn_name = msg.get("inspector_function", "inspect")
        if not hasattr(inspector_mod, inspector_fn_name):
            return {
                "status": "error",
                "message": (
                    f"Inspector function '{inspector_fn_name}' not found in "
                    f"module '{msg['inspector_module']}'."
                ),
            }
        inspector_fn = getattr(inspector_mod, inspector_fn_name)
        result = inspector_fn(svc)

        # Accept either a flowthru.ValidationResult instance (preferred)
        # or a duck-typed object with a to_dict() method, or a raw dict.
        if hasattr(result, "to_dict"):
            payload = result.to_dict()
        elif isinstance(result, dict):
            payload = result
        else:
            return {
                "status": "error",
                "message": (
                    f"Inspector '{msg['inspector_module']}.{inspector_fn_name}' "
                    f"returned {type(result).__name__}; expected ValidationResult."
                ),
            }
        return {"status": "ok", "result": payload}
    except Exception:
        return {"status": "error", "message": traceback.format_exc()}


# ---------------------------------------------------------------------------
# Input decoding
# ---------------------------------------------------------------------------

def _decode(input_type: str, encoded: str):
    if input_type == "scalar":
        return json.loads(encoded)
    if input_type == "bytes":
        return base64.b64decode(encoded)
    if input_type == "tabular":
        if _flowthru_arrow is None:
            raise RuntimeError(
                "pyarrow / _flowthru_arrow not available. "
                "Ensure pyarrow is listed in pyproject.toml."
            )
        return _flowthru_arrow.df_from_ipc(base64.b64decode(encoded))
    if input_type == "multi":
        elements = json.loads(encoded)
        return [_decode(e["kind"], e["value"]) for e in elements]
    if input_type == "directory":
        envelope = json.loads(encoded)
        inner_kind = envelope["inner_kind"]
        return {k: _decode(inner_kind, v) for k, v in envelope["entries"].items()}
    raise ValueError(f"Unknown input_type: {input_type!r}")


# ---------------------------------------------------------------------------
# Output encoding
# ---------------------------------------------------------------------------

def _encode(output_type: str, value, dtype_spec=None, element_specs=None, directory_spec=None) -> str:
    if output_type == "scalar":
        return json.dumps(value)
    if output_type == "bytes":
        return base64.b64encode(value).decode("ascii")
    if output_type == "tabular":
        if _flowthru_arrow is None:
            raise RuntimeError(
                "pyarrow / _flowthru_arrow not available. "
                "Ensure pyarrow is listed in pyproject.toml."
            )
        ipc_bytes = _flowthru_arrow.df_to_ipc(value, dtype_spec)
        return base64.b64encode(ipc_bytes).decode("ascii")
    if output_type == "multi":
        items = list(value) if not isinstance(value, (list, tuple)) else list(value)
        specs = element_specs or [{"kind": "scalar"}] * len(items)
        result = []
        for item, spec in zip(items, specs):
            kind = spec.get("kind", "scalar")
            result.append({
                "kind": kind,
                "value": _encode(kind, item, dtype_spec=spec.get("dtype_spec")),
            })
        return json.dumps(result)
    if output_type == "directory":
        if not isinstance(value, dict):
            raise TypeError(
                f"Expected dict for directory output, got {type(value).__name__}. "
                "Python steps with a Directory<T> output must return dict[str, T]."
            )
        if directory_spec is None:
            raise ValueError("directory output requires directory_spec.")
        inner_kind = directory_spec["inner_kind"]
        inner_dtype = directory_spec.get("dtype_spec")
        entries = {
            k: _encode(inner_kind, v, dtype_spec=inner_dtype) for k, v in value.items()
        }
        return json.dumps({"inner_kind": inner_kind, "entries": entries})
    raise ValueError(f"Unknown output_type: {output_type!r}")


# ---------------------------------------------------------------------------
# Invoke
# ---------------------------------------------------------------------------

def _handle_invoke(msg: dict) -> dict:
    try:
        mod = _get_module(msg["module"])
        func = getattr(mod, msg["function"])

        decoded = _decode(msg["input_type"], msg["input"])

        # Redirect stdout to stderr for the duration of the user call so
        # print() doesn't corrupt the newline-delimited JSON protocol on
        # stdout. Direct redirection (no buffering layer) means each
        # print() line bridges to the host's ILogger interleaved with
        # engine logs — important for failure paths where the step
        # crashes mid-execution and a batched buffer would never flush.
        with contextlib.redirect_stdout(sys.stderr):
            # Unpack multi-input as positional args
            if msg["input_type"] == "multi":
                result = func(*decoded)
            else:
                result = func(decoded)

        encoded = _encode(
            msg["output_type"],
            result,
            dtype_spec=msg.get("output_dtype_spec"),
            element_specs=msg.get("output_element_specs"),
            directory_spec=msg.get("output_directory_spec"),
        )
        return {"status": "ok", "output": encoded}
    except Exception:
        return {"status": "error", "message": traceback.format_exc()}


# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------

def main() -> None:
    global _flowthru_arrow

    # Line-buffer stderr so each log line/print() flushes immediately.
    # Without this, Python buffers stderr when it's a pipe (default
    # behaviour when stderr isn't a tty), and the host sees output in
    # 4KB chunks rather than line-by-line — breaking the interleaving
    # between engine logs and step logs the bridge is designed for.
    try:
        sys.stderr.reconfigure(line_buffering=True)
    except AttributeError:
        # Python < 3.7 lacks reconfigure(); the bridge still works,
        # output just lands in larger batches.
        pass

    # Install the Flowthru log bridge before any user code runs so
    # `logging.basicConfig(...)` in user modules is a no-op (Python's
    # basicConfig short-circuits when the root logger already has
    # handlers).
    _install_log_bridge()

    # First line must be the init message
    init_line = sys.stdin.readline()
    if not init_line:
        sys.exit(1)
    _handle_init(json.loads(init_line))

    # Load Arrow bridge after sys.path is configured
    try:
        _flowthru_arrow = importlib.import_module("_flowthru_arrow")
    except ImportError:
        pass  # tabular steps will raise at invocation time with a clear message

    sys.stdout.write(
        json.dumps({
            "status": "ready",
            "python_executable": sys.executable,
            "python_prefix": sys.prefix,
            "sys_path": sys.path,
        })
        + "\n"
    )
    sys.stdout.flush()

    # Message loop
    for line in sys.stdin:
        line = line.strip()
        if not line:
            continue

        try:
            msg = json.loads(line)
        except json.JSONDecodeError as exc:
            sys.stdout.write(
                json.dumps({"status": "error", "message": f"JSON parse error: {exc}"}) + "\n"
            )
            sys.stdout.flush()
            continue

        msg_type = msg.get("type")

        if msg_type == "shutdown":
            break
        elif msg_type == "validate":
            resp = _handle_validate(msg)
        elif msg_type == "invoke":
            resp = _handle_invoke(msg)
        elif msg_type == "inspect":
            resp = _handle_inspect(msg)
        else:
            resp = {"status": "error", "message": f"Unknown message type: {msg_type!r}"}

        sys.stdout.write(json.dumps(resp) + "\n")
        sys.stdout.flush()


if __name__ == "__main__":
    main()
