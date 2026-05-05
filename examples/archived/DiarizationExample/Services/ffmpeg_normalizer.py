"""ffmpeg-backed audio normalization service.

Plain Python class — no Flowthru imports, no preflight probe method.
Preflight checks (ffmpeg binary on PATH, binary actually invokable) live
in the integrator's ``python.RegisterService`` closure on the C# side
(see ``Program.cs``).
"""

from __future__ import annotations

from .diarization_config import DiarizationConfig


class FfmpegNormalizer:
    """Resamples arbitrary audio to 16kHz mono PCM via ffmpeg."""

    def __init__(self, config: DiarizationConfig | None = None):
        self.config = config or DiarizationConfig.load()

    def normalize(self, audio_bytes: bytes) -> bytes:
        raise NotImplementedError("Body omitted — focus is the service API.")
