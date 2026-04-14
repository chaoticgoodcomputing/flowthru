"""Stochastic consensus assignment via Gumbel-Sinkhorn perturbation.

Motivation
----------
Hungarian gives one MAP estimate from a weak cost matrix.  When CoherenceScore
std is ~0.07 (Sinkhorn-flattened), the correct pair and a plausible wrong pair
sit nearly at the same cost level — a single solve is fragile.

Gumbel-Sinkhorn replaces the single deterministic solve with K stochastic solves,
then picks the assignment with the highest *vote consensus* across samples.

Theory
------
Each sample k:

  1. Perturb the log-score matrix with Gumbel noise and anneal by temperature τ_k:

         log_S[i,j] = (−cost[i,j] + g[i,j]) / τ_k,   g[i,j] ~ Gumbel(0,1)

     Negating cost converts "lower CoherenceScore = better" into "higher log-score = better".
     At high τ (early samples) the Gumbel noise dominates — exploration.
     At low τ (late samples) the signal dominates — exploitation.

  2. Apply Sinkhorn normalization in log-space (20 iterations):

         log_S -= logsumexp(log_S, axis=1, keepdims=True)   # row marginals → 0
         log_S -= logsumexp(log_S, axis=0, keepdims=True)   # col marginals → 0

     This reinforces doubly-stochastic structure without collapsing signal into
     a uniform matrix (unlike the linear-space normalization in run_hungarian.py).

  3. Extract hard assignment: argmax = linear_sum_assignment(−log_S).

  4. Accumulate votes[i,j] += 1 for each assigned pair.

Final assignment: linear_sum_assignment(−votes) — the pair each inp piece most
consistently chose across temperatures, constrained to remain a perfect matching.

Supersedes run_hungarian.py (Sinkhorn + single deterministic solve), which is
retained but commented out in ExplorationFlow.cs.
"""
import logging
import numpy as np
import pandas as pd
from scipy.optimize import linear_sum_assignment
from scipy.special import logsumexp
from flowthru import step

logger = logging.getLogger(__name__)

_K = 500            # number of stochastic samples
_TAU_START = 2.0    # initial temperature (exploration)
_TAU_END = 0.05     # final temperature (exploitation)
_SINKHORN_ITERS = 20
_SEED = 42


@step(
    inputs=["PairingScore"],
    outputs=["BlockAssignment"],
)
def run_gumbel_sinkhorn(pairing_scores: pd.DataFrame) -> pd.DataFrame:
    """Consensus Block assignment via K Gumbel-Sinkhorn perturbation samples.

    Args:
        pairing_scores: DataFrame with [InpPieceIndex, OutPieceIndex, CoherenceScore].

    Returns:
        DataFrame with [BlockIndex, InpPieceIndex, OutPieceIndex, CoherenceScore].
        BlockIndex is a sequential label (0–47). CoherenceScore is the original
        pre-perturbation score for the winning pair.
    """
    inp_indices = sorted(pairing_scores["InpPieceIndex"].astype(int).unique().tolist())
    out_indices = sorted(pairing_scores["OutPieceIndex"].astype(int).unique().tolist())

    n_inp = len(inp_indices)
    n_out = len(out_indices)
    assert n_inp == n_out, f"Expected square pairing matrix, got {n_inp}×{n_out}"
    n = n_inp

    inp_pos = {idx: i for i, idx in enumerate(inp_indices)}
    out_pos = {idx: i for i, idx in enumerate(out_indices)}

    # Build cost matrix (lower CoherenceScore = better trained pair)
    cost_matrix = np.full((n, n), np.inf)
    for _, row in pairing_scores.iterrows():
        i = inp_pos[int(row["InpPieceIndex"])]
        j = out_pos[int(row["OutPieceIndex"])]
        cost_matrix[i, j] = float(row["CoherenceScore"])

    # Replace inf with a large finite sentinel so logsumexp stays finite
    finite_cost = cost_matrix.copy()
    max_finite = float(finite_cost[finite_cost < np.inf].max())
    finite_cost[finite_cost == np.inf] = max_finite * 10.0

    # Normalize by std so τ is in units of signal std, not scorer units.
    # τ=1.0 → noise ≈ 1 signal std (exploration); τ=0.05 → noise ≈ 5% std (exploitation).
    # This makes the temperature schedule portable across every scorer we use.
    cost_std = float(finite_cost[cost_matrix < np.inf].std())
    if cost_std > 1e-8:
        finite_cost /= cost_std

    votes = np.zeros((n, n), dtype=np.int32)
    taus = np.linspace(_TAU_START, _TAU_END, _K)
    rng = np.random.default_rng(seed=_SEED)

    for k in range(_K):
        tau = float(taus[k])
        gumbel = rng.gumbel(0.0, 1.0, size=(n, n))

        # log-score: negate cost so higher = more aligned
        log_s = (-finite_cost + gumbel) / tau

        # Sinkhorn in log-space
        for _ in range(_SINKHORN_ITERS):
            log_s -= logsumexp(log_s, axis=1, keepdims=True)
            log_s -= logsumexp(log_s, axis=0, keepdims=True)

        # Hard assignment: maximize log_s
        row_ind, col_ind = linear_sum_assignment(-log_s)
        for i, j in zip(row_ind, col_ind):
            votes[i, j] += 1

    # Consensus assignment from vote matrix
    row_ind, col_ind = linear_sum_assignment(-votes)

    rows = []
    for block_idx, (i, j) in enumerate(zip(row_ind, col_ind)):
        rows.append({
            "BlockIndex": block_idx,
            "InpPieceIndex": inp_indices[i],
            "OutPieceIndex": out_indices[j],
            "CoherenceScore": float(cost_matrix[i, j]),
        })

    vote_counts = [votes[i, j] for i, j in zip(row_ind, col_ind)]
    total_cost = sum(r["CoherenceScore"] for r in rows)
    logger.info(
        f"[run_gumbel_sinkhorn] K={_K} samples, τ {_TAU_START}→{_TAU_END}. "
        f"Consensus votes: min={min(vote_counts)} max={max(vote_counts)} "
        f"mean={np.mean(vote_counts):.1f}. "
        f"Total CoherenceScore={total_cost:.4f} mean={total_cost / len(rows):.4f}"
    )

    return pd.DataFrame(
        rows, columns=["BlockIndex", "InpPieceIndex", "OutPieceIndex", "CoherenceScore"]
    )
