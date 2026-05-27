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
      "transit_dir": "/path/to/per-invocation/dir",
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
- tabular:   file path to Arrow IPC stream bytes on disk
- bytes:     file path to raw bytes on disk
- multi:     JSON array of {"kind": "<type>", "value": "<encoded>"}
- directory: JSON object {"inner_kind": "<type>", "entries": {"<path>": "<encoded>", ...}}
             — represents a Directory<T> where each entry is one file. The Python step
             receives / returns a plain dict[str, T] with paths as keys.
"""

import sys
import os
import json
import importlib
import logging
import tempfile
import threading
import time
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


# ── Rank-aware distributed dispatch (ADR-0014, slice 5) ─────────────────────
#
# When the launcher fans out N python workers (torchrun, accelerate, mpi…),
# only rank 0 owns the parent's stdin/stdout pipe — the Flowthru protocol
# channel. Non-rank-0 workers cannot read the init / invoke messages from
# stdin (the pipe is shared and racy: whichever worker reads first consumes
# the bytes). We coordinate via a session directory in $TMPDIR: rank 0 also
# writes each protocol message to a sequenced file there, and non-rank-0
# workers poll for those files in order.
#
# The session dir name is derived from MASTER_ADDR + MASTER_PORT, both set
# by torchrun and unique per launch on a node. Multi-node setups produce
# the same MASTER_ADDR/PORT across nodes (the rendezvous address), so the
# session dir collides — but rank N>0 on a worker node has access to the
# *local* tmpdir, and rank 0 on the master node writes there; we trust the
# multi-node case to use a shared FS for the session dir (out of scope for
# slice 5; single-node is the slice-5 target).
#
# Worker lifecycle in distributed mode is *single-shot*: after one invoke
# returns, all ranks exit (the executor's worker is dead; the next Invoke
# call would need to re-launch torchrun). torchrun's process model expects
# this anyway — one DDP cycle per launch. Multi-step distributed flows
# would require either re-launching the executor per step or a different
# launcher-aware lifecycle; deferred until a real use case shows up.

_DIST_RANK = int(os.environ.get("RANK", "0"))
_DIST_WORLD_SIZE = int(os.environ.get("WORLD_SIZE", "1"))
_DIST_ENABLED = _DIST_WORLD_SIZE > 1


def _broadcast_session_dir() -> str:
    """Per-launch tmpdir holding sequenced broadcast files. All ranks
    compute the same path because MASTER_ADDR/MASTER_PORT are set
    identically by torchrun across ranks."""
    addr = os.environ.get("MASTER_ADDR", "127.0.0.1")
    port = os.environ.get("MASTER_PORT", "29500")
    pid = os.environ.get("TORCHELASTIC_RUN_ID", os.environ.get("PPID", "0"))
    return os.path.join(
        tempfile.gettempdir(), f"flowthru-bcast-{addr}-{port}-{pid}"
    )


def _broadcast_publish(seq: int, msg: dict) -> None:
    """Rank-0 only: persist the just-received protocol message so
    non-rank-0 ranks can pick it up. Atomic write via rename so polling
    readers never see a half-written file."""
    d = _broadcast_session_dir()
    os.makedirs(d, exist_ok=True)
    final_path = os.path.join(d, f"{seq:04d}.json")
    tmp_path = final_path + ".tmp"
    with open(tmp_path, "w") as f:
        json.dump(msg, f)
    os.replace(tmp_path, final_path)


def _broadcast_receive(seq: int, timeout_seconds: float = 60.0) -> dict:
    """Non-rank-0 only: poll for the sequenced file rank 0 published.
    Times out with a clear error rather than hanging forever — slice-5
    failures should surface, not deadlock."""
    d = _broadcast_session_dir()
    path = os.path.join(d, f"{seq:04d}.json")
    deadline = time.monotonic() + timeout_seconds
    while time.monotonic() < deadline:
        if os.path.exists(path):
            with open(path) as f:
                return json.load(f)
        time.sleep(0.05)
    raise TimeoutError(
        f"flowthru rank {_DIST_RANK}/{_DIST_WORLD_SIZE}: "
        f"timed out waiting for broadcast file {path} after {timeout_seconds}s. "
        f"Rank 0 may have failed before publishing the message."
    )


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
        with open(encoded, "rb") as f:
            return f.read()
    if input_type == "tabular":
        if _flowthru_arrow is None:
            raise RuntimeError(
                "pyarrow / _flowthru_arrow not available. "
                "Ensure pyarrow is listed in pyproject.toml."
            )
        with open(encoded, "rb") as f:
            return _flowthru_arrow.df_from_ipc(f.read())
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

def _encode(output_type: str, value, transit_dir: str, file_prefix: str = "output",
            dtype_spec=None, element_specs=None, directory_spec=None) -> str:
    if output_type == "scalar":
        return json.dumps(value)
    if output_type == "bytes":
        file_path = os.path.join(transit_dir, f"{file_prefix}.bin")
        with open(file_path, "wb") as f:
            f.write(value)
        return file_path
    if output_type == "tabular":
        if _flowthru_arrow is None:
            raise RuntimeError(
                "pyarrow / _flowthru_arrow not available. "
                "Ensure pyarrow is listed in pyproject.toml."
            )
        ipc_bytes = _flowthru_arrow.df_to_ipc(value, dtype_spec)
        file_path = os.path.join(transit_dir, f"{file_prefix}.arrow")
        with open(file_path, "wb") as f:
            f.write(ipc_bytes)
        return file_path
    if output_type == "multi":
        items = list(value) if not isinstance(value, (list, tuple)) else list(value)
        specs = element_specs or [{"kind": "scalar"}] * len(items)
        result = []
        for idx, (item, spec) in enumerate(zip(items, specs)):
            kind = spec.get("kind", "scalar")
            result.append({
                "kind": kind,
                "value": _encode(kind, item, transit_dir,
                                 file_prefix=f"{file_prefix}_{idx}",
                                 dtype_spec=spec.get("dtype_spec")),
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
        entry_dir = os.path.join(transit_dir, f"{file_prefix}_dir")
        os.makedirs(entry_dir, exist_ok=True)
        entries = {
            k: _encode(inner_kind, v, entry_dir, file_prefix=k,
                       dtype_spec=inner_dtype) for k, v in value.items()
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

        transit_dir = msg.get("transit_dir", "")

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
            transit_dir,
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

def _configure_arrow_bridge() -> None:
    """Late-load the Arrow bridge after sys.path is configured. Failure
    is non-fatal — only tabular steps care, and they'll raise at
    invocation time with a clear message."""
    global _flowthru_arrow
    try:
        _flowthru_arrow = importlib.import_module("_flowthru_arrow")
    except ImportError:
        pass


def _dispatch(msg: dict):
    """Route a parsed protocol message to its handler. Shared between
    rank 0 and non-rank-0 dispatch paths so both ranks execute the
    same user-function entry point under torch.distributed."""
    msg_type = msg.get("type")
    if msg_type == "validate":
        return _handle_validate(msg)
    if msg_type == "invoke":
        return _handle_invoke(msg)
    if msg_type == "inspect":
        return _handle_inspect(msg)
    if msg_type == "shutdown":
        return None
    return {"status": "error", "message": f"Unknown message type: {msg_type!r}"}


def _main_single_rank() -> None:
    """Existing single-process path — preserved verbatim for
    backwards compatibility with DirectPythonLauncher and any other
    launcher that doesn't fan out workers."""
    # First line must be the init message
    init_line = sys.stdin.readline()
    if not init_line:
        sys.exit(1)
    _handle_init(json.loads(init_line))

    _configure_arrow_bridge()

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

        if msg.get("type") == "shutdown":
            break

        resp = _dispatch(msg)
        if resp is None:
            break

        sys.stdout.write(json.dumps(resp) + "\n")
        sys.stdout.flush()


def _main_rank_zero_distributed() -> None:
    """Rank 0 of a distributed launch: reads stdin as usual, but
    *additionally* publishes each protocol message to the broadcast
    session dir so non-rank-0 workers can pick it up. Multi-shot —
    the worker stays alive across invokes (and across validate /
    inspect calls), matching the per-FlowthruService executor model.
    Exits cleanly on shutdown, or when stdin closes."""
    seq = 0

    init_line = sys.stdin.readline()
    if not init_line:
        sys.exit(1)
    init_msg = json.loads(init_line)

    # Publish init *first* so non-rank-0 workers don't sit waiting
    # while we initialise Python state locally.
    seq += 1
    _broadcast_publish(seq, init_msg)

    _handle_init(init_msg)
    _configure_arrow_bridge()

    sys.stdout.write(
        json.dumps({
            "status": "ready",
            "python_executable": sys.executable,
            "python_prefix": sys.prefix,
            "sys_path": sys.path,
            "distributed_rank": _DIST_RANK,
            "distributed_world_size": _DIST_WORLD_SIZE,
        })
        + "\n"
    )
    sys.stdout.flush()

    # Multi-shot work loop. Every subsequent stdin message gets
    # broadcast to non-rank-0 *before* local dispatch, so all ranks
    # see the same invoke at compatible times for torch.distributed
    # coordination.
    for line in sys.stdin:
        line = line.strip()
        if not line:
            continue

        try:
            work_msg = json.loads(line)
        except json.JSONDecodeError as exc:
            sys.stdout.write(
                json.dumps({"status": "error", "message": f"JSON parse error: {exc}"}) + "\n"
            )
            sys.stdout.flush()
            continue

        seq += 1
        _broadcast_publish(seq, work_msg)

        if work_msg.get("type") == "shutdown":
            break

        resp = _dispatch(work_msg)
        if resp is not None:
            sys.stdout.write(json.dumps(resp) + "\n")
            sys.stdout.flush()


def _main_non_rank_zero_distributed() -> None:
    """Non-rank-0 worker: doesn't own the parent stdin/stdout. Polls
    the broadcast session dir for each sequenced protocol message,
    dispatches locally so the user function participates in
    torch.distributed coordination, and loops until rank 0 publishes
    a shutdown message. Responses are discarded — only rank 0 talks
    to the .NET host."""
    # Redirect stdout to /dev/null so any user-code print() can't
    # interleave with rank 0's protocol stream on the parent pipe.
    # stderr is fine — the bridge classifier on the C# side handles
    # multiple writers; user errors / framework logs from non-rank-0
    # workers stay visible.
    devnull = open(os.devnull, "w")
    sys.stdout = devnull

    seq = 0

    seq += 1
    init_msg = _broadcast_receive(seq)
    _handle_init(init_msg)
    _configure_arrow_bridge()

    # Multi-shot receive loop. The seq counter must stay in lock-step
    # with rank 0's publisher; once we miss a beat we'd be reading
    # the wrong message for the rest of the session.
    while True:
        seq += 1
        work_msg = _broadcast_receive(seq)

        if work_msg.get("type") == "shutdown":
            break

        # Dispatch — the user function runs identically on all ranks;
        # any torch.distributed coordination (DDP wrap, gradient
        # sync) is its responsibility, not ours. Return value is
        # discarded.
        _dispatch(work_msg)


def main() -> None:
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

    if not _DIST_ENABLED:
        _main_single_rank()
    elif _DIST_RANK == 0:
        _main_rank_zero_distributed()
    else:
        _main_non_rank_zero_distributed()


if __name__ == "__main__":
    main()
