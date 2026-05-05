"""OpenAI Whisper transcription service.

Plain Python class — no Flowthru imports, no preflight probe method.
Preflight checks (model name validity, cache directory writability,
free-disk-space headroom) live in the integrator's
``python.RegisterService`` closure on the C# side (see ``Program.cs``).
"""

from __future__ import annotations

from .diarization_config import DiarizationConfig


class WhisperTranscriber:
    """Whisper transcription via the openai-whisper Python package."""

    def __init__(self, config: DiarizationConfig | None = None):
        self.config = config or DiarizationConfig.load()
        self._whisper_model = None  # lazy

    def transcribe(self, audio_bytes: bytes):
        if self._whisper_model is None:
            import whisper
            self._whisper_model = whisper.load_model(self.config.whisper_model)
        raise NotImplementedError("Body omitted — focus is the service API.")
