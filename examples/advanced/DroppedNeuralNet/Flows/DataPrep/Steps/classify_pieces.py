"""Classify each layer piece by inspecting its tensor dimensions."""
import io
import logging
import pandas as pd
import torch
from flowthru import step

logger = logging.getLogger(__name__)

# Dimension constants derived from the Block / LastLayer source:
#   Block.inp  = Linear(in_dim=48,  hidden_dim=96)  → weight shape (96, 48)
#   Block.out  = Linear(in_dim=96,  out_dim=48)     → weight shape (48, 96)
#   LastLayer  = Linear(in_dim=48,  out_dim=1)      → weight shape (1,  48)
_LAYER_TYPE = {
    (96, 48): "BlockInp",
    (48, 96): "BlockOut",
    (1,  48): "Last",
}


@step(inputs=["PieceBlob"], outputs=["PieceMetadata"])
def classify_pieces(pieces: pd.DataFrame) -> pd.DataFrame:
    """Deserialize each blob and assign a LayerType based on weight shape.

    Args:
        pieces: DataFrame with columns [PieceIndex, Data].

    Returns:
        DataFrame with columns [PieceIndex, InputDim, OutputDim, LayerType].
        No blob data is returned — steps needing tensors join against the raw Pieces item.
    """
    logger.info(f"[classify_pieces] Classifying {len(pieces)} pieces")
    rows = []

    for _, row in pieces.iterrows():
        piece_index = int(row["PieceIndex"])
        raw_bytes: bytes = row["Data"]

        state_dict = torch.load(
            io.BytesIO(raw_bytes),
            weights_only=True,
            map_location=torch.device("cpu"),
        )
        weight_shape = tuple(state_dict["weight"].shape)
        out_dim, in_dim = weight_shape

        layer_type = _LAYER_TYPE.get(weight_shape)
        if layer_type is None:
            raise ValueError(
                f"piece_{piece_index}: unexpected weight shape {weight_shape}"
            )

        rows.append({
            "PieceIndex": piece_index,
            "InputDim": in_dim,
            "OutputDim": out_dim,
            "LayerType": layer_type,
        })

    logger.info(
        f"[classify_pieces] Classification complete: "
        + ", ".join(
            f"{v}x {k}"
            for k, v in {
                t: sum(1 for r in rows if r["LayerType"] == t) for t in _LAYER_TYPE.values()
            }.items()
        )
    )
    return pd.DataFrame(rows, columns=["PieceIndex", "InputDim", "OutputDim", "LayerType"])
