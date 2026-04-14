"""Score every legal (inp, out) Block pairing by SVD subspace alignment.

Theory
------
A trained (inp, out) pair shares a common hidden subspace: the directions in R^96 that
inp writes to (its column space) and the directions that out reads from (its row space)
should overlap substantially.

For a given R (number of top singular vectors to compare):

  W_inp ∈ R^(96×48): inp maps 48-d input → 96-d hidden.
    U_inp, S, Vh = svd(W_inp)          # U_inp ∈ R^(96×48)
    U_inp_top = U_inp[:, :R]           # R principal hidden directions written to

  W_out ∈ R^(48×96): out maps 96-d hidden → 48-d output.
    U, S, Vh_out = svd(W_out)          # Vh_out ∈ R^(48×96)
    V_out_top = Vh_out[:R, :].T        # R principal hidden directions read from

  alignment = ||U_inp_top.T @ V_out_top||_F / sqrt(R)
    → 1.0 means the two R-dim subspaces are identical
    → 0.0 means orthogonal (no shared hidden directions)

  CoherenceScore = 1 - alignment   (lower = better for Hungarian minimization)

No historical data required; this is purely geometric.

Supersedes
----------
compute_activation_scores.py (Pearson correlation, v2) — required a full data pass.
compute_pairing_scores.py (Frobenius of W_out @ W_inp, v0) — scale dominated.
"""
import io
import logging
import pandas as pd
import torch
from flowthru import step

logger = logging.getLogger(__name__)

_R = 8  # top singular vectors to compare; captures most variance cleanly at dim=48


@step(
    inputs=["PieceMetadata", "PieceBlob", "BlockCandidate"],
    outputs=["PairingScore"],
)
def compute_svd_activation_scores(
    piece_metadata: pd.DataFrame,
    pieces: pd.DataFrame,
    legal_pairings: pd.DataFrame,
) -> pd.DataFrame:
    """Score each legal (inp, out) pairing by SVD subspace alignment.

    Measures how much the principal hidden-space directions that inp writes to and
    that out reads from overlap. High overlap → trained pair; low overlap → mismatch.

    Args:
        piece_metadata: Structural metadata for all pieces (LayerType, dims).
        pieces: Raw byte blobs indexed by PieceIndex.
        legal_pairings: Dimension-valid (InpPieceIndex, OutPieceIndex) candidates.

    Returns:
        DataFrame with [InpPieceIndex, OutPieceIndex, CoherenceScore].
        Lower CoherenceScore indicates a more likely trained pair.
    """
    blob_by_index: dict[int, bytes] = {
        int(r["PieceIndex"]): r["Data"] for _, r in pieces.iterrows()
    }

    legal_set: set[tuple[int, int]] = set(
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
        f"[compute_svd_activation_scores] {len(inp_indices)} inp × {len(out_indices)} out = "
        f"{len(legal_set)} legal pairs, R={_R}"
    )

    def load_weight(piece_idx: int, key: str) -> torch.Tensor:
        sd = torch.load(
            io.BytesIO(blob_by_index[piece_idx]),
            weights_only=True,
            map_location=torch.device("cpu"),
        )
        return sd[key]

    # ------------------------------------------------------------------
    # U_inp_top[inp]: (96, R) — top-R left singular vectors of W_inp
    # These span the principal hidden directions that inp writes to.
    # ------------------------------------------------------------------
    U_inp_top: dict[int, torch.Tensor] = {}
    with torch.no_grad():
        for inp_idx in inp_indices:
            W = load_weight(inp_idx, "weight")  # (96, 48)
            U, _, _ = torch.linalg.svd(W, full_matrices=False)  # U: (96, 48)
            U_inp_top[inp_idx] = U[:, :_R]  # (96, R)

    # ------------------------------------------------------------------
    # V_out_top[out]: (96, R) — top-R right singular vectors of W_out
    # Vh_out has shape (48, 96); columns of Vh_out.T span the hidden space read from.
    # ------------------------------------------------------------------
    V_out_top: dict[int, torch.Tensor] = {}
    with torch.no_grad():
        for out_idx in out_indices:
            W = load_weight(out_idx, "weight")  # (48, 96)
            _, _, Vh = torch.linalg.svd(W, full_matrices=False)  # Vh: (48, 96)
            V_out_top[out_idx] = Vh[:_R, :].T  # (96, R)

    # ------------------------------------------------------------------
    # Alignment score: ||U_inp_top.T @ V_out_top||_F / sqrt(R)
    # Maps to [0, 1] where 1 = identical subspace, 0 = orthogonal.
    # CoherenceScore = 1 - alignment so lower = better for Hungarian.
    # ------------------------------------------------------------------
    r_sqrt = float(_R) ** 0.5
    rows = []
    with torch.no_grad():
        for inp_idx in inp_indices:
            U = U_inp_top[inp_idx]  # (96, R)
            for out_idx in out_indices:
                if (inp_idx, out_idx) not in legal_set:
                    continue
                V = V_out_top[out_idx]  # (96, R)
                alignment = float(torch.linalg.norm(U.T @ V)) / r_sqrt
                rows.append({
                    "InpPieceIndex": inp_idx,
                    "OutPieceIndex": out_idx,
                    "CoherenceScore": 1.0 - alignment,
                })

    logger.info(f"[compute_svd_activation_scores] Computed {len(rows)} scores")
    return pd.DataFrame(rows, columns=["InpPieceIndex", "OutPieceIndex", "CoherenceScore"])
