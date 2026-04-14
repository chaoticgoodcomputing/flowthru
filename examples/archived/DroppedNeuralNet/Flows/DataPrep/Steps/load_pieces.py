"""Load all piece_*.pth files from a directory into raw binary blobs."""
import io
import logging
import os
import pandas as pd
from flowthru import step

logger = logging.getLogger(__name__)


@step(inputs=["string"], outputs=["PieceBlob"])
def load_pieces(pieces_dir: str) -> pd.DataFrame:
    """Read every piece_*.pth file and emit one PieceBlob row per file.

    Args:
        pieces_dir: Path to the directory containing piece_0.pth … piece_96.pth.

    Returns:
        DataFrame with columns [PieceIndex, Data] where Data is the raw .pth bytes.
    """
    logger.info(f"[load_pieces] Scanning directory: {pieces_dir}")

    rows = []
    for filename in sorted(os.listdir(pieces_dir)):
        if not (filename.startswith("piece_") and filename.endswith(".pth")):
            continue

        piece_index = int(filename.removeprefix("piece_").removesuffix(".pth"))
        file_path = os.path.join(pieces_dir, filename)

        with open(file_path, "rb") as f:
            data = f.read()

        rows.append({"PieceIndex": piece_index, "Data": data})

    logger.info(f"[load_pieces] Loaded {len(rows)} pieces")
    return pd.DataFrame(rows, columns=["PieceIndex", "Data"])
