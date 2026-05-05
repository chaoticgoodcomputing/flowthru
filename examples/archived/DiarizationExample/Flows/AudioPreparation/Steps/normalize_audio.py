"""Resample input audio to 16kHz mono PCM via the FfmpegNormalizer service."""

from flowthru import step
from Services import FfmpegNormalizer


@step(
    inputs=["AudioClips"],
    outputs="NormalizedAudio",
    services=[FfmpegNormalizer],
)
def normalize_audio(
    clips: dict[str, bytes],
    ffmpeg: FfmpegNormalizer,
) -> dict[str, bytes]:
    """Transcode each clip to 16kHz mono WAV, keying outputs by clip_id.

    The `clips` parameter arrives as a `dict[str, bytes]` because the input
    is a `Directory<byte[]>` on the C# side — see the marshaller extension
    notes in the README. Output keys are the same paths with the extension
    swapped to `.wav`.
    """
    return {
        _swap_extension(path, ".wav"): ffmpeg.normalize(audio_bytes)
        for path, audio_bytes in clips.items()
    }


def _swap_extension(path: str, new_ext: str) -> str:
    from pathlib import Path
    return str(Path(path).with_suffix(new_ext))
