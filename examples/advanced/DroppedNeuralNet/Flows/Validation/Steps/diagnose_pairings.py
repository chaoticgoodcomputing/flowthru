"""Diagnostic probes for Hungarian block assignment quality.

Three independent checks are run and their results emitted as (Category, Metric, Value, Notes)
rows so they can be inspected in _08_Reporting/Datasets/diagnostics.json.

Probe 1 — FixedOrdering
  Assembles the network using the Hungarian assignments in ascending BlockIndex order
  (an arbitrary but deterministic ordering).  If max_err is still large (>1.0) the
  inp/out pairings themselves are wrong — no ordering can fix this.  If max_err is
  small the pairings are correct and only the beam search range needs widening.

Probe 2 — PairingSignal
  Summarises the distribution of ProductNorm scores that fed into the Hungarian solver.
  A flat distribution (near-zero std) means the cost matrix has no discriminating power
  and the assignment is essentially random.

Probe 3 — Candidate_N
  Re-runs the forward pass for each ranked candidate permutation from rank_orderings and
  records its individual max_err.  This shows whether the beam search is getting closer
  or diverging, independent of whether the pairings are correct.
"""
import io
import json
import logging
import numpy as np
import pandas as pd
import torch
import torch.nn as nn
from flowthru import step

logger = logging.getLogger(__name__)


# ---------------------------------------------------------------------------
# Network building helpers (mirrors validate_permutations.py)
# ---------------------------------------------------------------------------

class Block(nn.Module):
    def __init__(self):
        super().__init__()
        self.inp = nn.Linear(48, 96, bias=True)
        self.out = nn.Linear(96, 48, bias=True)

    def forward(self, x):
        return x + self.out(torch.relu(self.inp(x)))


class LastLayer(nn.Module):
    def __init__(self):
        super().__init__()
        self.layer = nn.Linear(48, 1, bias=True)

    def forward(self, x):
        return self.layer(x)


def _load_linear(raw_bytes: bytes) -> nn.Linear:
    state_dict = torch.load(
        io.BytesIO(raw_bytes),
        weights_only=True,
        map_location=torch.device("cpu"),
    )
    out_f, in_f = state_dict["weight"].shape
    layer = nn.Linear(in_f, out_f, bias=True)
    layer.load_state_dict(state_dict)
    return layer


def _build_network_from_perm(perm: list[int], blob_by_index: dict[int, bytes]) -> nn.Sequential:
    """Reconstruct the full network from an explicit int[97] permutation."""
    modules = []
    for k in range(48):
        block = Block()
        block.inp = _load_linear(blob_by_index[perm[2 * k]])
        block.out = _load_linear(blob_by_index[perm[2 * k + 1]])
        modules.append(block)
    last = LastLayer()
    last.layer = _load_linear(blob_by_index[perm[96]])
    modules.append(last)
    return nn.Sequential(*modules)


def _eval_network(net: nn.Sequential, X: torch.Tensor, pred: torch.Tensor) -> tuple[float, float]:
    """Return (max_err, mean_err) for a network evaluated against pred."""
    net.eval()
    with torch.no_grad():
        result = net(X).squeeze(-1)
    abs_err = (result - pred).abs()
    return float(abs_err.max()), float(abs_err.mean())


# ---------------------------------------------------------------------------
# Step
# ---------------------------------------------------------------------------

@step(
    inputs=["BlockAssignment", "PieceBlob", "MeasurementSchema", "CandidatePermutation"],
    outputs=["DiagnosticEntry"],
)
def diagnose_pairings(
    block_assignments: pd.DataFrame,
    pieces: pd.DataFrame,
    historical_data: pd.DataFrame,
    candidate_permutations: pd.DataFrame,
) -> pd.DataFrame:
    """Run pairing-quality probes and emit diagnostic rows.

    Args:
        block_assignments: Hungarian-optimal (BlockIndex, InpPieceIndex, OutPieceIndex,
            AssignmentScore) — 48 rows.
        pieces: Raw byte blobs indexed by PieceIndex.
        historical_data: Sensor measurements; the ``pred`` column is the validation target.
        candidate_permutations: Ranked int[97] candidates from rank_orderings.

    Returns:
        DataFrame with columns [Category, Metric, Value, Notes].
    """
    rows: list[dict] = []

    blob_by_index: dict[int, bytes] = {
        int(r["PieceIndex"]): r["Data"] for _, r in pieces.iterrows()
    }
    feature_cols = [f"measurement_{i}" for i in range(48)]
    X = torch.tensor(historical_data[feature_cols].values, dtype=torch.float32)
    pred_t = torch.tensor(historical_data["pred"].values, dtype=torch.float32)

    # ------------------------------------------------------------------
    # Probe 1 — FixedOrdering baseline
    # ------------------------------------------------------------------
    logger.info("[diagnose_pairings] Probe 1: fixed-order network (BlockIndex ascending)")

    sorted_assignments = sorted(
        block_assignments.to_dict("records"),
        key=lambda r: int(r["BlockIndex"]),
    )

    # Identify the single LastLayer piece (not referenced by any block)
    block_pieces = {
        idx
        for a in sorted_assignments
        for idx in (int(a["InpPieceIndex"]), int(a["OutPieceIndex"]))
    }
    all_indices = set(blob_by_index.keys())
    last_candidates = all_indices - block_pieces
    assert len(last_candidates) == 1, (
        f"Expected exactly one LastLayer piece; found {last_candidates}"
    )
    last_idx = next(iter(last_candidates))

    # Build permutation: [inp_0, out_0, inp_1, out_1, ..., last]
    fixed_perm: list[int] = []
    for a in sorted_assignments:
        fixed_perm.append(int(a["InpPieceIndex"]))
        fixed_perm.append(int(a["OutPieceIndex"]))
    fixed_perm.append(last_idx)

    fixed_net = _build_network_from_perm(fixed_perm, blob_by_index)
    max_err, mean_err = _eval_network(fixed_net, X, pred_t)

    rows.append({"Category": "FixedOrdering", "Metric": "MaxErr",  "Value": max_err,  "Notes": "ascending BlockIndex; large => pairings wrong"})
    rows.append({"Category": "FixedOrdering", "Metric": "MeanErr", "Value": mean_err, "Notes": ""})

    pairing_verdict = "PAIRINGS LIKELY WRONG" if max_err > 1.0 else "pairings look correct"
    logger.info(
        f"[diagnose_pairings] FixedOrdering max_err={max_err:.6f} mean_err={mean_err:.6f} — {pairing_verdict}"
    )
    print(f"[diagnose_pairings] FixedOrdering max_err={max_err:.6f} ({pairing_verdict})", flush=True)

    # ------------------------------------------------------------------
    # Probe 2 — ProductNorm signal statistics
    # ------------------------------------------------------------------
    logger.info("[diagnose_pairings] Probe 2: ProductNorm score distribution")

    scores = block_assignments["AssignmentScore"].astype(float).values
    score_mean  = float(np.mean(scores))
    score_std   = float(np.std(scores))
    score_range = float(np.max(scores) - np.min(scores))

    rows.append({"Category": "PairingSignal", "Metric": "ScoreMean",  "Value": score_mean,  "Notes": "mean ProductNorm of Hungarian-assigned pairs"})
    rows.append({"Category": "PairingSignal", "Metric": "ScoreStd",   "Value": score_std,   "Notes": "near-zero => cost matrix is flat => Hungarian is guessing"})
    rows.append({"Category": "PairingSignal", "Metric": "ScoreRange", "Value": score_range, "Notes": "max - min ProductNorm across 48 assigned pairs"})

    logger.info(
        f"[diagnose_pairings] PairingSignal mean={score_mean:.6f} std={score_std:.6f} range={score_range:.6f}"
    )
    print(
        f"[diagnose_pairings] PairingSignal std={score_std:.6f} range={score_range:.6f}",
        flush=True,
    )

    # ------------------------------------------------------------------
    # Probe 3 — Per-candidate errors
    # ------------------------------------------------------------------
    logger.info(f"[diagnose_pairings] Probe 3: {len(candidate_permutations)} ranked candidates")

    for _, row in candidate_permutations.iterrows():
        candidate_idx = int(row["CandidateIndex"])
        perm: list[int] = json.loads(row["Permutation"])
        net = _build_network_from_perm(perm, blob_by_index)
        c_max_err, c_mean_err = _eval_network(net, X, pred_t)

        category = f"Candidate_{candidate_idx}"
        rows.append({"Category": category, "Metric": "MaxErr",  "Value": c_max_err,  "Notes": ""})
        rows.append({"Category": category, "Metric": "MeanErr", "Value": c_mean_err, "Notes": ""})
        print(
            f"[diagnose_pairings] {category}: max_err={c_max_err:.6f} mean_err={c_mean_err:.6f}",
            flush=True,
        )

    best_candidate_err = min(
        r["Value"] for r in rows if r["Metric"] == "MaxErr" and r["Category"].startswith("Candidate_")
    )
    rows.append({
        "Category": "Summary",
        "Metric": "BestCandidateMaxErr",
        "Value": best_candidate_err,
        "Notes": f"best across {len(candidate_permutations)} candidates",
    })

    logger.info(f"[diagnose_pairings] Done. Best candidate max_err={best_candidate_err:.6f}")
    print(f"[diagnose_pairings] Best candidate max_err={best_candidate_err:.6f}", flush=True)

    return pd.DataFrame(rows, columns=["Category", "Metric", "Value", "Notes"])
