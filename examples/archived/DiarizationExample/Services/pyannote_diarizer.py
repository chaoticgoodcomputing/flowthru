"""pyannote speaker-diarization service.

Plain Python class — no Flowthru imports, no preflight probe method. The
class only describes "what this service does at runtime"; preflight checks
live in the integrator's ``python.RegisterService`` closure on the C# side
(see ``Program.cs``). This separation mirrors the canonical .NET sidecar
pattern: the service is normal client code, and the Flowthru wiring lives
externally.

Configuration is read from the process environment via the shared
:class:`DiarizationConfig` (see ``diarization_config.py``), which the
Flowthru Python extension populates from the host's ``IConfiguration``
tree at subprocess spawn time.
"""

from __future__ import annotations

from .diarization_config import DiarizationConfig


class PyannoteDiarizer:
    """Speaker diarization using pyannote/speaker-diarization-3.1."""

    def __init__(self, config: DiarizationConfig | None = None):
        self.config = config or DiarizationConfig.load()
        self._pipeline = None  # lazy-loaded on first diarize() call

    def diarize(self, audio_bytes: bytes):
        if self._pipeline is None:
            from pyannote.audio import Pipeline
            self._pipeline = Pipeline.from_pretrained(
                self.config.pyannote_model,
                use_auth_token=self.config.hugging_face_token,
            )
        # Returns list of (start, end, speaker_id) records.
        raise NotImplementedError("Body omitted — focus is the service API.")
