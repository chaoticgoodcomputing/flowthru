# DiarizationExample (advanced, Python service preflight)

A speaker-attributed transcription pipeline (Whisper + pyannote) used as the
**design vehicle for Python-side service preflight**. The example is currently
*aspirational*: it scaffolds the API surface as it should look, then identifies
the extension changes needed to make it real.

## What this example proposes

Three new pieces of Python-extension API surface, all modeled on canonical
.NET shapes from `SimpleEffectsExample`:

1. **`python.ConfigurationSection`** — a section name on `UsePython(...)`
   that tells the extension to flatten the host's `IConfiguration` tree
   into environment variables (using .NET's native `:` → `__` rule)
   before spawning the Python subprocess.

2. **`flowthru.config`** — a Python module provided by the extension that
   re-nests those env vars on the Python side and exposes typed accessors
   modelled on `IConfiguration.GetValue<T>(...)`. Includes a `bind(cls,
   section_name)` helper that materializes a frozen dataclass directly
   from a named section. Pipeline code never sees the flattening.

3. **`python.RegisterService(serviceClassPath, cfg => cfg.WithInspector(inspectorModulePath))`** —
   a wiring API that links a plain Python service class to a separate
   Python sidecar inspector module. Mirrors the
   `services.AddFlowthruInspect<TService, TInspector>()` shape from .NET:
   the service is normal client code, and a separately-defined inspector
   carries the validation logic.

## The sidecar pattern, mirrored across the language boundary

The .NET starter `SimpleEffectsExample` documents the canonical shape:
service classes are Flowthru-free, and a separately-defined inspector
implements `IFlowthruInspector<TService>` with the validation logic.

The Python equivalent here uses a two-file convention per service:

```
Services/
├── pyannote_diarizer.py            ← service: Flowthru-free runtime class
└── pyannote_diarizer_inspector.py  ← sidecar: imports flowthru.ValidationResult,
                                                does the actual probing
```

The framework spawns a Python subprocess, constructs the service from the
registered class path, hands it to the inspector's `inspect(svc)` function,
and marshals the returned `ValidationResult` back to C#. Failure halts the
run before any step executes; success allows the flow to proceed and reuses
the same subprocess (and the same constructed service instance) for step
execution.

**This is honest preflight.** The C# closure variant from earlier drafts
could only check the *environment* the service depends on; the Python
sidecar actually imports the service module (catches Python-side import
errors), constructs the service (catches config-wiring errors), and
exercises it. A passing inspector means the service is genuinely callable,
not just that the environment looks plausible.

## Three roles, cleanly separated

| Role                       | Writes                                      | Imports                                     |
| -------------------------- | ------------------------------------------- | ------------------------------------------- |
| Python service developer   | `Services/<name>.py`                        | pyannote — **no Flowthru, no third-party config lib** |
| Python config author       | `Services/diarization_config.py`            | `flowthru.config`, `dataclasses`            |
| Python inspector author    | `Services/<name>_inspector.py`              | `flowthru.ValidationResult`, the service    |
| Python flow developer      | `Flows/.../*.py`                            | `flowthru.step`, plain service classes      |
| Flowthru integrator        | `Program.cs` — `python.RegisterService(...)` | Flowthru, IConfiguration                    |

The Python files have asymmetric coupling. The *service classes* are
fully decoupled — they could be lifted out and used in any Python
codebase. The *config schema* and *inspectors* are intentionally
Flowthru-coupled because they exist to bridge the service into Flowthru's
preflight and configuration contracts. That mirrors .NET's split between
`TimeApiClient` (Flowthru-free) and the inspector closure (uses
`ValidationResult`).

## Why pyannote is the right vehicle

pyannote's runtime failure modes are *strictly less informative* than what
a preflight check can tell you:

| Cause                              | Runtime symptom                                  | Preflight diagnostic                            |
| ---------------------------------- | ------------------------------------------------ | ----------------------------------------------- |
| HuggingFaceToken not configured    | AttributeError deep inside huggingface_hub       | "HuggingFaceToken is not configured..."         |
| Token present but invalid          | 401 from a model-load helper subprocess          | "HuggingFace rejected the token (401)..."       |
| Token valid, terms not accepted    | 403 from the same helper, identical-looking      | "...you have not accepted the model terms..."   |

In all three cases the runtime symptom shows up *after* Whisper has chewed
through the entire audio batch in parallel — because Transcription and
Diarization run side by side. The sidecar inspector at
[pyannote_diarizer_inspector.py](Services/pyannote_diarizer_inspector.py)
distinguishes these by HTTP status code and emits the right diagnostic via
`ValidationResult.failure(...)`.

## How the IConfiguration round-trip works

The extension hides the wire format from pipeline code. Nested .NET
`IConfiguration` becomes flattened env vars on the wire, then re-nests
into a typed Python view on the other end:

```
appsettings.json
└── Diarization
    ├── PyannoteModel: "pyannote/speaker-diarization-3.1"
    ├── WhisperModel: "base.en"
    ├── TargetSampleRate: 16000
    └── HuggingFaceToken: null    ← override in appsettings.Local.json or env
        ↓
.NET IConfiguration keys (host process)
└── "Diarization:PyannoteModel" = "pyannote/speaker-diarization-3.1"
    ↓
Environment variables injected into Python subprocess (.NET native :→__)
└── Diarization__PyannoteModel=pyannote/speaker-diarization-3.1
    ↓
flowthru.config re-nests on the Python side
└── config.get_section("Diarization")["PyannoteModel"]
    ↓
config.bind(DiarizationConfig, "Diarization") → typed dataclass instance
```

Pipeline code only ever interacts with the last stage. The middle stages
(flattening, env-var transport) are extension internals.

The transport stage is also *isolated* from the host environment within
the configured section's namespace. At subprocess spawn the executor
strips any inherited env var matching the section prefix
(`Diarization__*`), then emits IConfiguration-resolved values on top.
The Python side therefore only sees section-namespaced env vars that came
through .NET's config layer — a host shell that exports
`Diarization__HuggingFaceToken=...` reaches Python only if the host's
`ConfigurationBuilder` includes `.AddEnvironmentVariables()`, in which
case .NET's normal precedence rules apply. Anything outside the section
namespace (`PATH`, `HUGGING_FACE_HUB_TOKEN`, GPU detection vars, etc.)
passes through to the subprocess unchanged.

`flowthru.config` provides `IConfiguration`-style accessors —
`get_section(name)`, `get_str(key, default)`, `get_int`, `get_bool`,
`get_float`, `get_list`, plus `_optional_*` variants that return `None`
on absence. The `bind(cls, section)` helper introspects a frozen
dataclass's fields, derives section keys via snake_case → PascalCase,
coerces each value based on the field's type hint, and falls back to
field defaults when the key is absent. Coercion failures raise a
`ConfigCoercionError` (subclass of `ValueError`) that names the offending
key.

```python
# Services/diarization_config.py — project-level, no third-party deps
from dataclasses import dataclass
from flowthru import config

@dataclass(frozen=True)
class DiarizationConfig:
    pyannote_model: str = "pyannote/speaker-diarization-3.1"
    whisper_model: str = "base.en"
    hugging_face_token: str | None = None
    target_sample_rate: int = 16000

    @classmethod
    def load(cls) -> "DiarizationConfig":
        return config.bind(cls, "Diarization")
```

The inspector reads config off the constructed service instance
(`svc.config.hugging_face_token`), so config flows through *one path* —
spawn → env vars → `flowthru.config` re-nests → `bind` materializes the
dataclass → service holds the typed snapshot → inspector reads typed
fields. No third-party config library required.

## API surface walkthrough

### Service (Python, plain class, runtime only)

```python
# Services/pyannote_diarizer.py — no Flowthru imports
from .diarization_config import DiarizationConfig

class PyannoteDiarizer:
    def __init__(self, config: DiarizationConfig | None = None):
        self.config = config or DiarizationConfig.load()  # reads env vars
        self._pipeline = None

    def diarize(self, audio_bytes: bytes): ...
```

### Sidecar inspector (Python, Flowthru-coupled, validation logic)

```python
# Services/pyannote_diarizer_inspector.py
import httpx
from flowthru import ValidationResult, ValidationErrorType
from .pyannote_diarizer import PyannoteDiarizer

def inspect(svc: PyannoteDiarizer) -> ValidationResult:
    if not svc.config.hugging_face_token:
        return ValidationResult.failure(
            source="PyannoteDiarizer",
            error_type=ValidationErrorType.Configuration,
            message="Diarization:HuggingFaceToken is not configured...",
        )

    response = httpx.get(
        f"https://huggingface.co/api/models/{svc.config.pyannote_model}",
        headers={"Authorization": f"Bearer {svc.config.hugging_face_token}"},
        timeout=10.0,
    )
    if response.status_code == 401:
        return ValidationResult.failure(
            source="PyannoteDiarizer",
            error_type=ValidationErrorType.Forbidden,
            message="HuggingFace rejected the token (401)...",
        )
    if response.status_code == 403:
        return ValidationResult.failure(
            source="PyannoteDiarizer",
            error_type=ValidationErrorType.Forbidden,
            message="Token is valid but you have not accepted the model terms...",
        )
    return ValidationResult.success()
```

### Step (Python, declares the dependency by class reference)

```python
# Flows/Diarization/Steps/diarize.py
from flowthru import step
from Services import PyannoteDiarizer

@step(
    inputs=["NormalizedAudio"],
    outputs="DiarizationSegmentSchema",
    services=[PyannoteDiarizer],
)
def diarize(clips: dict[str, bytes],
            diarizer: PyannoteDiarizer) -> pd.DataFrame:
    ...
```

### Integration (C#, minimal — just declares the linkage)

```csharp
// Program.cs
flowthru.UsePython(python =>
{
    python.ConfigurationSection = "Diarization";

    python.RegisterService("Services.pyannote_diarizer.PyannoteDiarizer",
        svc => svc.WithInspector("Services.pyannote_diarizer_inspector"));

    python.RegisterService("Services.whisper_transcriber.WhisperTranscriber",
        svc => svc.WithInspector("Services.whisper_transcriber_inspector"));

    python.RegisterService("Services.ffmpeg_normalizer.FfmpegNormalizer",
        svc => svc.WithInspector("Services.ffmpeg_normalizer_inspector"));
});
```

The C# side carries no probing logic at all — it just identifies which
inspector module corresponds to which service. The first argument is the
Python class's *defining*-module path (`__module__ + "." + __qualname__`),
which is what the `@step(services=[…])` decorator records on each step;
both ends must use that form rather than any re-export path.

By convention the inspector module exports an `inspect` function; an
explicit override is available (`.WithInspector(module, "validate")`) for
projects that prefer a different naming convention.

## Structure

```
DiarizationExample/
├── pyproject.toml                          # ffmpeg-python, whisper, pyannote, httpx — no config lib
├── Program.cs                              # registers flows + python.RegisterService linkage
├── appsettings.json                        # Diarization:* config section
├── Data/
│   ├── _01_Raw/Catalog.Raw.cs              # Directory<byte[]> of input audio
│   ├── _02_Intermediate/Catalog.*.cs       # Directory<byte[]> of normalized PCM
│   ├── _03_Primary/Catalog.*.cs            # Transcripts + DiarizationTurns parquet
│   ├── _04_Feature/Catalog.*.cs            # AttributedTranscript parquet
│   └── _08_Reporting/Catalog.*.cs          # Directory<byte[]> of rendered Markdown
├── Flows/
│   ├── AudioPreparation/                   # Python — uses FfmpegNormalizer
│   ├── Transcription/                      # Python — uses WhisperTranscriber
│   ├── Diarization/                        # Python — uses PyannoteDiarizer
│   ├── Alignment/                          # C# — pure interval math
│   └── Reporting/                          # Python — pure presentation
└── Services/
    ├── diarization_config.py               # frozen dataclass + config.bind()
    ├── pyannote_diarizer.py                # service: plain class
    ├── pyannote_diarizer_inspector.py      # sidecar: validation logic
    ├── whisper_transcriber.py
    ├── whisper_transcriber_inspector.py
    ├── ffmpeg_normalizer.py
    └── ffmpeg_normalizer_inspector.py
```

## Extension changes required

Listed in implementation order. Items 1–3 are pure C# core changes and
ship value before any Python-side work touches them.

1. **`ServiceRef` abstraction in core.** Migrate
   `FlowStep.ServiceDependencies` from `IReadOnlyList<Type>` to
   `IReadOnlyList<ServiceRef>` with `CSharp(Type)` and `Python(string)`
   variants. Source generator emits `CSharp`; Python extension emits
   `Python`. The `StepMetadata.ServiceDependencies: List<string>` layer
   already converts to strings for renderers — no rendering code changes.

2. **Preflight loop dispatches on `ServiceRef` variant.** C# branch is
   today's `AddFlowthruInspect<T>` flow at `Flow.cs:740–793`. Python
   branch consults a new `IPythonServiceInspectorRegistry` populated by
   `python.RegisterService`, spawns the executor with `(serviceModulePath,
   inspectorModulePath, inspectorFunctionName)` and marshals back the
   `ValidationResult`.

3. **`Flowthru.Extensions.Python` Python-side additions:**
   - `flowthru.ValidationResult` and `flowthru.ValidationErrorType` —
     thin dataclasses mirroring the C# shape. Plain data carriers, no
     business logic.
   - `flowthru.config` — typed `IConfiguration`-style accessors over
     re-nested env vars (`get_section`, `get_str`/`int`/`bool`/`float`,
     `_optional_*` variants, `get_list`), plus a `bind(cls, section)`
     helper that introspects frozen dataclass fields and materializes a
     typed instance. Mirrors .NET's `IConfiguration.GetValue<T>(...)`
     and `Bind(...)` semantics.
   - `services=[ClassRef]` parameter on `@step`. Decorator metadata
     extraction in `IPythonExecutor.ValidateStep` adds these to the step's
     service-dependency list as `ServiceRef.Python(...)` entries.

4. **Python extension C#-side additions:**
   - `python.RegisterService(classPath, cfg => cfg.WithInspector(...))`
     API and the supporting registry singleton (DI-backed).
   - `IPythonConfigurationFlattener` (DI-backed) that walks the
     `python.ConfigurationSection` of `IConfiguration` and produces env-var
     pairs at subprocess spawn, using .NET's `:` → `__` rule. **Strip-then-
     emit at spawn:** the executor first removes any inherited env var in
     the section's namespace (e.g. `Diarization__*`), then emits the
     IConfiguration-resolved values. This guarantees Python only sees
     section-namespaced env vars that came through `IConfiguration` — host
     env values bypass the C# config layer if and only if the host has
     opted in via `AddEnvironmentVariables()`. Process-essential vars
     (`PATH`, `HOME`, `LD_LIBRARY_PATH`, etc.) and unrelated vars outside
     the section namespace pass through unchanged.
   - Inspector invocation entrypoint on `IPythonExecutor` —
     `InvokeInspector(serviceModulePath, inspectorModulePath, functionName)`.
     Reuses the existing Arrow IPC channel for the response payload.

5. **`Directory<byte[]>` ↔ `dict[str, bytes]` marshalling** — independent
   prerequisite for any batch binary work, not just this example.

## What this example deliberately does not do

- **Behavioral testing of Python transforms.** That's a separate question
  (a PyTest analog to FUnit), orthogonal to environmental preflight.
  Preflight verifies the service can be constructed and is reachable;
  behavioral testing verifies the transform produces correct output.
- **Cross-clip speaker re-identification.** Speaker IDs are local to each
  clip. Doing better requires cross-corpus speaker embedding clustering.
- **GPU detection.** Each inspector could check for CUDA, but the example
  keeps to CPU so it's reproducible without GPU hardware.

## Running

```bash
cd examples/advanced/DiarizationExample
uv sync
# HuggingFace token can be set in appsettings.Local.json:
#   {"Diarization": {"HuggingFaceToken": "hf_..."}}
# or as the equivalent .NET-convention env var:
export Diarization__HuggingFaceToken=hf_...
dotnet run -- --flow Diarization
```

If the token is missing, the run fails before any Python step executes,
with a diagnostic from the `pyannote_diarizer_inspector.inspect` function
that points at exactly which service rejected the environment and why.
