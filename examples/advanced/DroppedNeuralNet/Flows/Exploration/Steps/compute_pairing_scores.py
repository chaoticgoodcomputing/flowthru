"""Compute ||W_out @ W_inp||_F for every legal (inp, out) Block pairing.

Lower values indicate that the out layer approximates the inverse of the inp layer —
the residual coupling signal left in the weights by the training objective.
"""
import io
import logging
import pandas as pd
import torch
from flowthru import step

logger = logging.getLogger(__name__)


@step(
    inputs=["PieceMetadata", "PieceBlob", "BlockCandidate"],
    outputs=["PairingScore"],
)
def compute_pairing_scores(
    piece_metadata: pd.DataFrame,
    pieces: pd.DataFrame,
    legal_pairings: pd.DataFrame,
) -> pd.DataFrame:
    """Compute the Frobenius norm of W_out @ W_inp for every legal pairing.

    Args:
        piece_metadata: Structural metadata for all pieces (LayerType, dims).
        pieces: Raw byte blobs indexed by PieceIndex.
        legal_pairings: Dimension-valid (InpPieceIndex, OutPieceIndex) candidates.

    Returns:
        DataFrame with [InpPieceIndex, OutPieceIndex, ProductNorm].
    """
    blob_by_index: dict[int, bytes] = {
        int(r["PieceIndex"]): r["Data"] for _, r in pieces.iterrows()
    }

    # Restrict to the legal pairing set
    legal_set = set(
        zip(
            legal_pairings["InpPieceIndex"].astype(int),
            legal_pairings["OutPieceIndex"].astype(int),
        )
    )

    inp_indices = (
        piece_metadata[piece_metadata["LayerType"] == "BlockInp"]["PieceIndex"]
        .astype(int)
        .tolist()
    )
    out_indices = (
        piece_metadata[piece_metadata["LayerType"] == "BlockOut"]["PieceIndex"]
        .astype(int)
        .tolist()
    )

    logger.info(
        f"[compute_pairing_scores] {len(inp_indices)} inp × {len(out_indices)} out = "
        f"{len(legal_set)} legal pairs"
    )

    # Pre-load weight matrices (CPU, no grad)
    def load_weight(piece_idx: int) -> torch.Tensor:
        sd = torch.load(
            io.BytesIO(blob_by_index[piece_idx]),
            weights_only=True,
            map_location=torch.device("cpu"),
        )
        return sd["weight"]

    inp_weights = {idx: load_weight(idx) for idx in inp_indices}  # each (96, 48)
    out_weights = {idx: load_weight(idx) for idx in out_indices}  # each (48, 96)

    rows = []
    with torch.no_grad():
        for inp_idx in inp_indices:
            W_inp = inp_weights[inp_idx]  # (96, 48)
            for out_idx in out_indices:
                if (inp_idx, out_idx) not in legal_set:
                    continue
                W_out = out_weights[out_idx]  # (48, 96)
                product = W_out @ W_inp      # (48, 48)
                norm = float(torch.norm(product, p="fro"))
                rows.append({
                    "InpPieceIndex": inp_idx,
                    "OutPieceIndex": out_idx,
                    "ProductNorm": norm,
                })

    logger.info(f"[compute_pairing_scores] Computed {len(rows)} scores")
    return pd.DataFrame(rows, columns=["InpPieceIndex", "OutPieceIndex", "ProductNorm"])
