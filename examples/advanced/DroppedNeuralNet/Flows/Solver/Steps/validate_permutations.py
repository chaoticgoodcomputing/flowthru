"""Validate candidate permutations against the historical `pred` column.

Receives a small ranked set of candidate int[97] permutations from Exploration,
assembles each network from raw piece blobs, forward-passes the historical data,
and returns the first permutation whose output matches `pred` within tolerance.
"""
import io
import json
import logging
import pandas as pd
import torch
import torch.nn as nn
from flowthru import step

logger = logging.getLogger(__name__)

TOLERANCE = 1e-4   # max acceptable per-sample absolute error


class Block(nn.Module):
    """Mirrors the original Block architecture: Linear(48 → 96) + ReLU + Linear(96 → 48) with a residual connection."""
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
    """Mirrors the original LastLayer architecture: Linear(48 → 1) regression head."""
    def __init__(self, in_dim: int, out_dim: int):
        super().__init__()
        self.layer = nn.Linear(in_dim, out_dim)

    def forward(self, x):
        return self.layer(x)


def _load_linear(raw_bytes: bytes) -> nn.Linear:
    """Deserialize raw .pth bytes into a nn.Linear layer with weights and bias."""
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
    """Reconstruct the full network from a permutation and piece blobs.

    Args:
        perm: int[97] where perm[2k]/perm[2k+1] are inp/out for Block k, perm[96] is LastLayer.
        blob_by_index: PieceIndex -> raw .pth bytes.
    """
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
    outputs="PermutationSolution",
)
def validate_permutations(
    candidate_permutations: pd.DataFrame,
    pieces: pd.DataFrame,
    historical_data: pd.DataFrame,
) -> dict:
    """Forward-pass each candidate permutation and return the one that matches `pred`.

    Args:
        candidate_permutations: Ranked candidates from rank_orderings, each with a
            JSON-encoded int[97] Permutation string.
        pieces: Raw byte blobs indexed by PieceIndex.
        historical_data: Sensor measurements; `pred` column is the validation target.

    Returns:
        Dict matching PermutationSolution schema: {"Permutation": [int x 97]}.

    Raises:
        RuntimeError: If no candidate passes the tolerance check.
    """
    logger.info(
        f"[validate_permutations] Validating {len(candidate_permutations)} candidate permutations"
    )

    blob_by_index: dict[int, bytes] = {
        int(r["PieceIndex"]): r["Data"] for _, r in pieces.iterrows()
    }

    feature_cols = [f"measurement_{i}" for i in range(48)]
    X = torch.tensor(historical_data[feature_cols].values, dtype=torch.float32)
    pred = torch.tensor(historical_data["pred"].values, dtype=torch.float32)

    errors: list[float] = []
    for _, row in candidate_permutations.iterrows():
        candidate_idx = int(row["CandidateIndex"])
        perm: list[int] = json.loads(row["Permutation"])

        network = _build_network(perm, blob_by_index)
        network.eval()

        with torch.no_grad():
            result = network(X).squeeze(-1)

        max_err = float((result - pred).abs().max())
        errors.append(max_err)
        print(f"[validate_permutations] Candidate {candidate_idx}: max_err={max_err:.6f}", flush=True)

        if max_err < TOLERANCE:
            logger.info(
                f"[validate_permutations] Solution found at candidate {candidate_idx}"
            )
            return {"Permutation": perm}

    best_err = min(errors) if errors else float("inf")
    raise RuntimeError(
        f"No candidate permutation passed the tolerance check (TOLERANCE={TOLERANCE}). "
        f"Best max_err across {len(errors)} candidates: {best_err:.6f}. "
        "Try increasing N_CANDIDATES in rank_orderings.py or re-running Exploration."
    )
