"""Score every legal (inp, out) Block pairing using data-driven activation response.

Attempts
--------
v1 — Mean residual magnitude  (SUPERSEDED, see commented block below)
    score = mean_i ||W_out · relu(W_inp · x_i + b_inp) + b_out||_2
    Result: std=0.33, gap-to-2nd=0.02 — the bias term dominates ||R||_2 regardless
    of H, giving poor within-row discrimination (only 6/48 assignments at row min).

v2 — Pearson correlation between mean activation pattern and column attention  (CURRENT)
    For a trained (inp, out) pair:
      - inp uses a specific subset of the 96 hidden neurons on real data
        → measured by mean_act[inp][k] = E[relu(W_inp[k,:] · x + b_inp[k])]
      - out has been trained to attend to exactly those neurons
        → measured by col_norm[out][k] = ||W_out[:, k]||_2

    Pearson correlation(mean_act[inp], col_norm[out]) should be strongly positive
    for trained pairs and near-zero for random ones.

    score = 1 - pearson_r   (lower = better, consistent with Hungarian minimization)

    Bias terms are irrelevant: mean_act uses only the inp bias inside ReLU (shifts the
    active set, which is part of the signal), and col_norm ignores b_out entirely.
"""
import io
import logging
import numpy as np
import pandas as pd
import torch
from flowthru import step

logger = logging.getLogger(__name__)


@step(
    inputs=["PieceMetadata", "PieceBlob", "BlockCandidate", "MeasurementSchema"],
    outputs=["PairingScore"],
)
def compute_activation_scores(
    piece_metadata: pd.DataFrame,
    pieces: pd.DataFrame,
    legal_pairings: pd.DataFrame,
    historical_data: pd.DataFrame,
) -> pd.DataFrame:
    """Score each legal (inp, out) pairing by activation-pattern / attention alignment.

    Uses Pearson correlation between:
      - mean_act[inp]: which of the 96 hidden neurons inp activates most strongly on data
      - col_norm[out]: which of the 96 hidden neurons out pays most attention to

    A trained pair has high positive correlation; mismatched pairs are near zero.
    score = 1 - pearson_r  (lower = better for Hungarian minimization).

    Args:
        piece_metadata: Structural metadata for all pieces (LayerType, dims).
        pieces: Raw byte blobs indexed by PieceIndex.
        legal_pairings: Dimension-valid (InpPieceIndex, OutPieceIndex) candidates.
        historical_data: Sensor measurements used to probe inp activation patterns.

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
        f"[compute_activation_scores] {len(inp_indices)} inp × {len(out_indices)} out = "
        f"{len(legal_set)} legal pairs"
    )

    feature_cols = [f"measurement_{i}" for i in range(48)]
    X_t = torch.tensor(
        historical_data[feature_cols].values, dtype=torch.float32
    ).T  # (48, N)

    def load_state(piece_idx: int) -> dict[str, torch.Tensor]:
        return torch.load(
            io.BytesIO(blob_by_index[piece_idx]),
            weights_only=True,
            map_location=torch.device("cpu"),
        )

    # ------------------------------------------------------------------
    # mean_act[inp]: (96,) — average activation of each hidden neuron over the dataset
    # ------------------------------------------------------------------
    mean_act: dict[int, np.ndarray] = {}
    with torch.no_grad():
        for inp_idx in inp_indices:
            sd = load_state(inp_idx)
            H = torch.relu(sd["weight"] @ X_t + sd["bias"].unsqueeze(1))  # (96, N)
            mean_act[inp_idx] = H.mean(dim=1).numpy()  # (96,)

    # ------------------------------------------------------------------
    # col_norm[out]: (96,) — L2 norm of each column of W_out
    # column k corresponds to how much out "attends to" hidden neuron k
    # ------------------------------------------------------------------
    col_norm: dict[int, np.ndarray] = {}
    for out_idx in out_indices:
        sd = load_state(out_idx)
        col_norm[out_idx] = np.linalg.norm(sd["weight"].numpy(), axis=0)  # (96,)

    # ------------------------------------------------------------------
    # Pearson correlation → score = 1 - r  (lower = better)
    # ------------------------------------------------------------------
    def pearson_r(a: np.ndarray, b: np.ndarray) -> float:
        a_c = a - a.mean()
        b_c = b - b.mean()
        denom = np.linalg.norm(a_c) * np.linalg.norm(b_c)
        return float(np.dot(a_c, b_c) / denom) if denom > 1e-8 else 0.0

    rows = []
    for inp_idx in inp_indices:
        for out_idx in out_indices:
            if (inp_idx, out_idx) not in legal_set:
                continue
            r = pearson_r(mean_act[inp_idx], col_norm[out_idx])
            rows.append({
                "InpPieceIndex": inp_idx,
                "OutPieceIndex": out_idx,
                "CoherenceScore": 1.0 - r,   # lower = better alignment
            })

    logger.info(f"[compute_activation_scores] Computed {len(rows)} scores")
    return pd.DataFrame(rows, columns=["InpPieceIndex", "OutPieceIndex", "CoherenceScore"])


# =============================================================================
# v1 — SUPERSEDED: mean residual magnitude
# Bias dominates ||W_out @ H + b_out||_2 regardless of H; poor within-row gaps.
# =============================================================================
# def compute_activation_scores_v1(...):
#     ...
#     for inp_idx in inp_indices:
#         H = inp_activations[inp_idx]
#         for out_idx in out_indices:
#             W_out, b_out = out_params[out_idx]
#             R = W_out @ H + b_out.unsqueeze(1)
#             score = float(torch.norm(R, dim=0).mean())
#             rows.append({"InpPieceIndex": inp_idx, "OutPieceIndex": out_idx,
#                          "CoherenceScore": score})

