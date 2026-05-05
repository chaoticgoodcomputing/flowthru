"""Sidecar inspector for :class:`WhisperTranscriber`.

Verifies the requested model is recognized and the cache directory has
enough headroom to download the weights if they aren't already cached.
"""

from __future__ import annotations

import os
import shutil
from pathlib import Path

from flowthru import ValidationResult, ValidationErrorType

from .whisper_transcriber import WhisperTranscriber


# Approximate sizes published by OpenAI; used to fail at preflight rather
# than mid-download when the disk doesn't have headroom.
_MODEL_SIZES_MB = {
    "tiny": 75, "tiny.en": 75,
    "base": 145, "base.en": 145,
    "small": 480, "small.en": 480,
    "medium": 1500, "medium.en": 1500,
    "large": 3000, "large-v2": 3000, "large-v3": 3000,
}


def inspect(svc: WhisperTranscriber) -> ValidationResult:
    model = svc.config.whisper_model
    if model not in _MODEL_SIZES_MB:
        return ValidationResult.failure(
            source="WhisperTranscriber",
            error_type=ValidationErrorType.Configuration,
            message=(
                f"Unknown Whisper model {model!r}. Valid choices: "
                f"{', '.join(sorted(_MODEL_SIZES_MB))}."
            ),
        )

    cache_dir = Path(
        os.environ.get("XDG_CACHE_HOME", Path.home() / ".cache")
    ) / "whisper"
    if (cache_dir / f"{model}.pt").exists():
        return ValidationResult.success()

    try:
        cache_dir.mkdir(parents=True, exist_ok=True)
    except OSError as exc:
        return ValidationResult.failure(
            source="WhisperTranscriber",
            error_type=ValidationErrorType.IO,
            message=f"Cannot create Whisper cache at {cache_dir}: {exc}",
        )

    free_mb = shutil.disk_usage(cache_dir).free // (1024 * 1024)
    need_mb = _MODEL_SIZES_MB[model] * 2  # 2× headroom for download safety
    if free_mb < need_mb:
        return ValidationResult.failure(
            source="WhisperTranscriber",
            error_type=ValidationErrorType.IO,
            message=(
                f"Whisper model {model!r} needs ~{need_mb}MB free to "
                f"download; only {free_mb}MB available at {cache_dir}."
            ),
        )

    return ValidationResult.success()
