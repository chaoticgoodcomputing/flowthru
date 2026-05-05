"""Plain Python service implementations.

Each service in this directory is an ordinary Python class — no Flowthru
imports, no Flowthru-specific decorators, no preflight probe methods.
Construction reads configuration from the process environment via the
shared :class:`DiarizationConfig` (see ``diarization_config.py``).

The Flowthru wiring that adapts these classes to the framework's preflight
contract lives entirely on the C# side, in ``Program.cs``. Each service's
``python.RegisterService(...).InspectWith(...)`` closure does its own
probing — typically C#-side, using ``IConfiguration`` directly, without
spawning a Python subprocess. This mirrors the canonical .NET sidecar
pattern from the ``SimpleEffectsExample`` starter: the service file
describes "what the service does at runtime," and the integrator owns
"how Flowthru verifies the environment is ready."
"""

from .pyannote_diarizer import PyannoteDiarizer
from .whisper_transcriber import WhisperTranscriber
from .ffmpeg_normalizer import FfmpegNormalizer

__all__ = ["PyannoteDiarizer", "WhisperTranscriber", "FfmpegNormalizer"]
