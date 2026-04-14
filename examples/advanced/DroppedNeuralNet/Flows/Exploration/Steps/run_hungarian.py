"""Apply the Hungarian algorithm to the pairing score matrix to find optimal Block pairings.

The 48×48 cost matrix is built from ProductNorm scores (lower = better residual coupling).
scipy.optimize.linear_sum_assignment solves the minimum-cost perfect matching in O(n³).
"""
import logging
import numpy as np
import pandas as pd
from scipy.optimize import linear_sum_assignment
from flowthru import step

logger = logging.getLogger(__name__)


@step(
    inputs=["PairingScore"],
    outputs=["BlockAssignment"],
)
def run_hungarian(pairing_scores: pd.DataFrame) -> pd.DataFrame:
    """Solve the minimum-cost Block pairing via the Hungarian algorithm.

    Args:
        pairing_scores: DataFrame with [InpPieceIndex, OutPieceIndex, ProductNorm].

    Returns:
        DataFrame with [BlockIndex, InpPieceIndex, OutPieceIndex, AssignmentScore].
        BlockIndex is a sequential label (0–47), not the execution order.
    """
    inp_indices = sorted(pairing_scores["InpPieceIndex"].astype(int).unique().tolist())
    out_indices = sorted(pairing_scores["OutPieceIndex"].astype(int).unique().tolist())

    n_inp = len(inp_indices)
    n_out = len(out_indices)
    assert n_inp == n_out, f"Expected square pairing matrix, got {n_inp}×{n_out}"
    n = n_inp

    inp_pos = {idx: i for i, idx in enumerate(inp_indices)}
    out_pos = {idx: i for i, idx in enumerate(out_indices)}

    # Build cost matrix
    cost_matrix = np.full((n, n), np.inf)
    for _, row in pairing_scores.iterrows():
        i = inp_pos[int(row["InpPieceIndex"])]
        j = out_pos[int(row["OutPieceIndex"])]
        cost_matrix[i, j] = float(row["ProductNorm"])

    row_ind, col_ind = linear_sum_assignment(cost_matrix)

    rows = []
    for block_idx, (i, j) in enumerate(zip(row_ind, col_ind)):
        rows.append({
            "BlockIndex": block_idx,
            "InpPieceIndex": inp_indices[i],
            "OutPieceIndex": out_indices[j],
            "AssignmentScore": float(cost_matrix[i, j]),
        })

    total_cost = sum(r["AssignmentScore"] for r in rows)
    logger.info(
        f"[run_hungarian] Assigned {len(rows)} blocks, total cost={total_cost:.4f}, "
        f"mean={total_cost / len(rows):.4f}"
    )
    return pd.DataFrame(
        rows, columns=["BlockIndex", "InpPieceIndex", "OutPieceIndex", "AssignmentScore"]
    )
