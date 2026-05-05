"""Diarize each normalized clip via the PyannoteDiarizer service.

This is the canonical example that motivates Python service preflight.
Without the @service contract, the user discovers a missing HF token, an
unaccepted model license, or a CUDA mismatch *after* Whisper has already
chewed through their entire audio batch — because Transcription and
Diarization run in parallel on the same input. With the @service contract,
the preflight phase rejects the run before any compute is wasted.
"""

import pandas as pd

from flowthru import step
from Services import PyannoteDiarizer


@step(
    inputs=["NormalizedAudio"],
    outputs="DiarizationSegmentSchema",
    services=[PyannoteDiarizer],
)
def diarize(
    clips: dict[str, bytes],
    diarizer: PyannoteDiarizer,
) -> pd.DataFrame:
    """Produce one row per speaker turn per clip."""
    rows = []
    for clip_id, audio_bytes in clips.items():
        for turn in diarizer.diarize(audio_bytes):
            rows.append({
                "clip_id": clip_id,
                "start": turn["start"],
                "end": turn["end"],
                "speaker_id": turn["speaker"],
            })
    return pd.DataFrame(rows, columns=["clip_id", "start", "end", "speaker_id"])
