"""Transcribe each normalized clip via the WhisperTranscriber service."""

import pandas as pd

from flowthru import step
from Services import WhisperTranscriber


@step(
    inputs=["NormalizedAudio"],
    outputs="TranscriptSegmentSchema",
    services=[WhisperTranscriber],
)
def transcribe(
    clips: dict[str, bytes],
    whisper: WhisperTranscriber,
) -> pd.DataFrame:
    """Produce one row per transcript segment per clip.

    The DataFrame columns must match TranscriptSegmentSchema (clip_id, start,
    end, text). Schema mismatch is caught at preflight by the existing
    decorator-vs-generic-types check, *before* any audio is processed.
    """
    rows = []
    for clip_id, audio_bytes in clips.items():
        for segment in whisper.transcribe(audio_bytes):
            rows.append({
                "clip_id": clip_id,
                "start": segment["start"],
                "end": segment["end"],
                "text": segment["text"],
            })
    return pd.DataFrame(rows, columns=["clip_id", "start", "end", "text"])
