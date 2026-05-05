"""Project-level configuration for the Diarization pipeline.

The Flowthru Python extension flattens the host's ``IConfiguration`` tree
into environment variables at subprocess spawn time. ``flowthru.config``
re-nests those env vars (preserving .NET's array semantics for
sequential-integer keys) and exposes typed accessors that mirror
``IConfiguration.GetValue<T>(...)``. The ``bind`` helper materializes a
frozen dataclass directly from a named section.

This module imports ``flowthru.config`` but does **not** depend on
pydantic-settings or any other third-party config library. The Flowthru
Python extension provides everything the ``bind`` helper needs.
"""

from __future__ import annotations

from dataclasses import dataclass

from flowthru import config


@dataclass(frozen=True)
class DiarizationConfig:
    """Typed snapshot of the ``Diarization`` IConfiguration section.

    Field names map to PascalCase keys in the section
    (``pyannote_model`` → ``PyannoteModel``). Defaults declared here are
    used when the corresponding env var is absent at subprocess spawn —
    matching the behavior of ``IConfiguration.GetValue<T>(key, default)``.
    """

    pyannote_model: str = "pyannote/speaker-diarization-3.1"
    whisper_model: str = "base.en"
    hugging_face_token: str | None = None
    target_sample_rate: int = 16000

    @classmethod
    def load(cls) -> "DiarizationConfig":
        """Read the ``Diarization`` section into a typed config instance."""
        return config.bind(cls, "Diarization")
