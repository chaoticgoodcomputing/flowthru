"""Render one Markdown transcript per clip from the AttributedTranscript table.

No external services — this is a pure presentation step. Included to show
that not every Python step needs a service dependency, and that the
preflight surface scales down to zero just as cleanly as it scales up.
"""

import pandas as pd

from flowthru import step


@step(
    inputs=["AttributedTranscript"],
    outputs="RenderedTranscripts",
)
def render_transcript(attributed: pd.DataFrame) -> dict[str, bytes]:
    """One Markdown file per clip, keyed by clip_id with .md extension."""
    output: dict[str, bytes] = {}
    for clip_id, group in attributed.groupby("clip_id"):
        lines = [f"# {clip_id}", ""]
        for _, row in group.sort_values("start").iterrows():
            ts = f"[{_fmt(row.start)}–{_fmt(row['end'])}]"
            lines.append(f"**{row.speaker_id}** {ts}: {row.text}")
        output[_md_path(clip_id)] = "\n".join(lines).encode("utf-8")
    return output


def _fmt(seconds: float) -> str:
    m, s = divmod(int(seconds), 60)
    return f"{m:02d}:{s:02d}"


def _md_path(clip_id: str) -> str:
    from pathlib import Path
    return str(Path(clip_id).with_suffix(".md").name)
