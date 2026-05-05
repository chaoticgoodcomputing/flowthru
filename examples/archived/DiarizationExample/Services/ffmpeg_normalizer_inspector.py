"""Sidecar inspector for :class:`FfmpegNormalizer`.

The simplest preflight: the binary exists on PATH and is invokable.
"""

from __future__ import annotations

import shutil
import subprocess

from flowthru import ValidationResult, ValidationErrorType

from .ffmpeg_normalizer import FfmpegNormalizer


def inspect(svc: FfmpegNormalizer) -> ValidationResult:
    ffmpeg_path = shutil.which("ffmpeg")
    if ffmpeg_path is None:
        return ValidationResult.failure(
            source="FfmpegNormalizer",
            error_type=ValidationErrorType.NotFound,
            message=(
                "ffmpeg binary not found on PATH. Install via your system "
                "package manager (e.g. 'apt install ffmpeg' or "
                "'brew install ffmpeg')."
            ),
        )

    try:
        subprocess.run(
            [ffmpeg_path, "-version"],
            capture_output=True, check=True, timeout=5,
        )
    except (subprocess.CalledProcessError, subprocess.TimeoutExpired) as exc:
        return ValidationResult.failure(
            source="FfmpegNormalizer",
            error_type=ValidationErrorType.Misconfigured,
            message=f"ffmpeg at {ffmpeg_path} failed to run: {exc}",
        )

    return ValidationResult.success()
