"""Compute a normalized coherence score for every legal (inp, out) Block pairing.

The score measures structural alignment between the two weight matrices, independent of
their individual magnitudes:

    CoherenceScore(inp, out) = ||W_out @ W_inp||_F / (||W_out||_F * ||W_inp||_F)

This is the Frobenius norm of the product normalized by the product of the individual
norms — analogous to cosine similarity but in matrix space.  A low score means
W_out approximately inverts W_inp (the residual coupling signal left by training).

Using the raw ||W_out @ W_inp||_F directly contaminates the cost matrix with weight
magnitude: a piece whose weights happen to be 2× larger will score high in every row
regardless of structural compatibility, drowning the signal the Hungarian solver needs.
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
        DataFrame with [InpPieceIndex, OutPieceIndex, CoherenceScore].
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

    # Pre-compute per-piece Frobenius norms for the denominator
    inp_norms = {idx: float(torch.norm(w, p="fro")) for idx, w in inp_weights.items()}
    out_norms = {idx: float(torch.norm(w, p="fro")) for idx, w in out_weights.items()}

    rows = []
    with torch.no_grad():
        for inp_idx in inp_indices:
            W_inp = inp_weights[inp_idx]  # (96, 48)
            for out_idx in out_indices:
                if (inp_idx, out_idx) not in legal_set:
                    continue
                W_out = out_weights[out_idx]  # (48, 96)
                product_norm = float(torch.norm(W_out @ W_inp, p="fro"))
                denom = inp_norms[inp_idx] * out_norms[out_idx]
                coherence = product_norm / denom if denom > 0 else product_norm
                rows.append({
                    "InpPieceIndex": inp_idx,
                    "OutPieceIndex": out_idx,
                    "CoherenceScore": coherence,
                })

    logger.info(f"[compute_pairing_scores] Computed {len(rows)} scores")
    return pd.DataFrame(rows, columns=["InpPieceIndex", "OutPieceIndex", "CoherenceScore"])
