"""Evaluate every candidate permutation and emit a full error report.

Every candidate is always evaluated and recorded — no candidate is silently dropped
by a tolerance gate.  The report (IEnumerable<CandidateEvaluation>) is persisted as a
catalog entry so the full error distribution is available for downstream analysis.  A
separate C# step (SelectSolution) picks the best entry and writes it to catalog.Solution.

The PassesTolerance column (1/0) records whether each candidate cleared the tolerance
threshold, but the step itself never raises on failure — that decision belongs to the
consumer of the report, not to the step.
"""
import io
import json
import logging
import pandas as pd
import torch
import torch.nn as nn
from flowthru import step

logger = logging.getLogger(__name__)

TOLERANCE = 1e-4   # threshold written into PassesTolerance; does not gate output


class Block(nn.Module):
    """Mirrors the original Block architecture: Linear(48→96) + ReLU + Linear(96→48) + residual."""
    def __init__(self, in_dim: int, hidden_dim: int):
        super().__init__()
        self.inp = nn.Linear(in_dim, hidden_dim)
        self.activation = nn.ReLU()
        self.out = nn.Linear(hidden_dim, in_dim)

    def forward(self, x):
        residual = x
        x = self.inp(x)
        x = self.activation(x)
        x = self.out(x)
        return residual + x


class LastLayer(nn.Module):
    """Mirrors the original LastLayer architecture: Linear(48→1) regression head."""
    def __init__(self, in_dim: int, out_dim: int):
        super().__init__()
        self.layer = nn.Linear(in_dim, out_dim)

    def forward(self, x):
        return self.layer(x)


def _load_linear(raw_bytes: bytes) -> nn.Linear:
    state_dict = torch.load(
        io.BytesIO(raw_bytes),
        weights_only=True,
        map_location=torch.device("cpu"),
    )
    out_features, in_features = state_dict["weight"].shape
    layer = nn.Linear(in_features, out_features, bias=True)
    layer.load_state_dict(state_dict)
    return layer


def _build_network(perm: list[int], blob_by_index: dict[int, bytes]) -> nn.Sequential:
    modules = []
    for k in range(48):
        block = Block(in_dim=48, hidden_dim=96)
        block.inp = _load_linear(blob_by_index[perm[2 * k]])
        block.out = _load_linear(blob_by_index[perm[2 * k + 1]])
        modules.append(block)
    last = LastLayer(in_dim=48, out_dim=1)
    last.layer = _load_linear(blob_by_index[perm[96]])
    modules.append(last)
    return nn.Sequential(*modules)


@step(
    inputs=["CandidatePermutation", "PieceBlob", "MeasurementSchema"],
    outputs=["CandidateEvaluation"],
)
def validate_permutations(
    candidate_permutations: pd.DataFrame,
    pieces: pd.DataFrame,
    historical_data: pd.DataFrame,
) -> pd.DataFrame:
    """Forward-pass every candidate permutation and emit a full evaluation report.

    All candidates are evaluated unconditionally.  PassesTolerance=1 marks rows that
    cleared TOLERANCE; the step does not raise if none do.  SelectSolutionStep picks
    the best row from the resulting catalog entry.

    Args:
        candidate_permutations: Ranked candidates from rank_orderings with JSON-encoded
            int[97] Permutation strings.
        pieces: Raw byte blobs indexed by PieceIndex.
        historical_data: Sensor measurements; `pred` is the validation target.

    Returns:
        DataFrame with [CandidateIndex, MaxErr, MeanErr, PassesTolerance, Permutation].
    """
    logger.info(
        f"[validate_permutations] Evaluating {len(candidate_permutations)} candidates "
        f"(TOLERANCE={TOLERANCE})"
    )

    blob_by_index: dict[int, bytes] = {
        int(r["PieceIndex"]): r["Data"] for _, r in pieces.iterrows()
    }

    feature_cols = [f"measurement_{i}" for i in range(48)]
    X = torch.tensor(historical_data[feature_cols].values, dtype=torch.float32)
    pred = torch.tensor(historical_data["pred"].values, dtype=torch.float32)

    rows = []
    for _, row in candidate_permutations.iterrows():
        candidate_idx = int(row["CandidateIndex"])
        perm: list[int] = json.loads(row["Permutation"])

        network = _build_network(perm, blob_by_index)
        network.eval()

        with torch.no_grad():
            result = network(X).squeeze(-1)

        abs_err = (result - pred).abs()
        max_err  = float(abs_err.max())
        mean_err = float(abs_err.mean())
        passes   = 1 if max_err < TOLERANCE else 0

        rows.append({
            "CandidateIndex":   candidate_idx,
            "MaxErr":           max_err,
            "MeanErr":          mean_err,
            "PassesTolerance":  passes,
            "Permutation":      row["Permutation"],
        })
        logger.info(
            f"[validate_permutations] Candidate {candidate_idx}: "
            f"max_err={max_err:.6f} passes={passes}"
        )

    n_passing = sum(r["PassesTolerance"] for r in rows)
    logger.info(
        f"[validate_permutations] Complete: {n_passing}/{len(rows)} candidates passed tolerance"
    )

    return pd.DataFrame(
        rows,
        columns=["CandidateIndex", "MaxErr", "MeanErr", "PassesTolerance", "Permutation"],
    )
