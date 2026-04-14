"""Rank Block execution orderings using activation chaining on historical data.

Strategy:
  The Hungarian algorithm gives us 48 confirmed (inp, out) Block pairs but no ordering.
  To order them we exploit the fact that each Block was trained to operate on a specific
  activation distribution. We score candidate orderings by measuring how well each
  Block's output distribution matches what the *next* Block's inp weights expect.

  1. For each Block, run a forward pass on the raw measurement features.
  2. Score each Block as the possible *first* block: whose inp weights are most
     correlated with the raw data distribution?
  3. Score transitions: whose output distribution best matches each successor's inp weights?
  4. Beam-search the ordering space, keeping N_CANDIDATES beams.
  5. Encode each surviving ordering as a JSON-serialised int[97] permutation.
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

N_CANDIDATES = 16   # Number of candidate permutations to pass to the Solver


class Block(nn.Module):
    def __init__(self):
        super().__init__()
        self.inp = nn.Linear(48, 96)
        self.activation = nn.ReLU()
        self.out = nn.Linear(96, 48)

    def forward(self, x):
        return x + self.out(self.activation(self.inp(x)))


def _load_block(inp_bytes: bytes, out_bytes: bytes) -> Block:
    block = Block()
    block.inp.load_state_dict(
        torch.load(io.BytesIO(inp_bytes), weights_only=True, map_location="cpu")
    )
    block.out.load_state_dict(
        torch.load(io.BytesIO(out_bytes), weights_only=True, map_location="cpu")
    )
    return block


@step(
    inputs=["BlockAssignment", "PieceBlob", "MeasurementSchema"],
    outputs=["CandidatePermutation"],
)
def rank_orderings(
    block_assignments: pd.DataFrame,
    pieces: pd.DataFrame,
    historical_data: pd.DataFrame,
) -> pd.DataFrame:
    """Score Block orderings via activation chaining; emit top-N candidate permutations.

    Args:
        block_assignments: Hungarian-optimal (BlockIndex, InpPieceIndex, OutPieceIndex) pairs.
        pieces: Raw byte blobs indexed by PieceIndex.
        historical_data: Sensor measurements used to probe each Block's activation behaviour.

    Returns:
        DataFrame with [CandidateIndex, Permutation] where Permutation is a JSON int[97].
    """
    blob_by_index: dict[int, bytes] = {
        int(r["PieceIndex"]): r["Data"] for _, r in pieces.iterrows()
    }

    # Index assignments: block_idx → {inp_piece, out_piece, block_module}
    assignments = {}
    for _, row in block_assignments.iterrows():
        block_idx = int(row["BlockIndex"])
        inp_idx = int(row["InpPieceIndex"])
        out_idx = int(row["OutPieceIndex"])
        assignments[block_idx] = {
            "inp_piece": inp_idx,
            "out_piece": out_idx,
            "block": _load_block(blob_by_index[inp_idx], blob_by_index[out_idx]),
        }

    n_blocks = len(assignments)
    feature_cols = [f"measurement_{i}" for i in range(48)]
    X = torch.tensor(historical_data[feature_cols].values, dtype=torch.float32)

    # --- Signal 1: output activations of each Block applied to raw features ---
    block_outputs: dict[int, np.ndarray] = {}
    with torch.no_grad():
        for block_idx, info in assignments.items():
            info["block"].eval()
            block_outputs[block_idx] = info["block"](X).numpy()  # (N, 48)

    # --- Signal 2: mean inp weight row = "receptive field" of each Block ---
    inp_weight_mean: dict[int, np.ndarray] = {}
    for block_idx, info in assignments.items():
        inp_weight_mean[block_idx] = (
            info["block"].inp.weight.detach().numpy().mean(axis=0)  # (48,)
        )

    # --- Score: how well does block j's output look like block i's expected input? ---
    # transition_score[i][j] = cosine similarity between j's output mean and i's inp weight mean
    raw_mean = X.numpy().mean(axis=0)  # (48,)

    def cosine(a: np.ndarray, b: np.ndarray) -> float:
        return float(np.dot(a, b) / (np.linalg.norm(a) * np.linalg.norm(b) + 1e-8))

    # Score for being first block: cosine between raw features and inp weight mean
    first_score = {
        block_idx: cosine(raw_mean, inp_weight_mean[block_idx])
        for block_idx in range(n_blocks)
    }

    # Transition score matrix
    transition_score = np.zeros((n_blocks, n_blocks))
    for i in range(n_blocks):
        for j in range(n_blocks):
            if i == j:
                continue
            out_mean = block_outputs[j].mean(axis=0)     # j's mean output
            transition_score[i, j] = cosine(out_mean, inp_weight_mean[i])

    # --- Beam search over orderings ---
    # Each beam state: (ordering: list[int], remaining: frozenset[int], score: float)
    initial_beam: list[tuple[list[int], frozenset[int], float]] = [
        ([], frozenset(range(n_blocks)), 0.0)
    ]
    beam = initial_beam

    for step_num in range(n_blocks):
        candidates = []
        for ordering, remaining, score in beam:
            for candidate in remaining:
                if step_num == 0:
                    new_score = score + first_score[candidate]
                else:
                    prev = ordering[-1]
                    new_score = score + transition_score[candidate][prev]
                candidates.append((
                    ordering + [candidate],
                    remaining - {candidate},
                    new_score,
                ))
        candidates.sort(key=lambda x: x[2], reverse=True)
        beam = candidates[:N_CANDIDATES]

    # --- Identify the single LastLayer piece ---
    all_block_pieces = {
        idx
        for info in assignments.values()
        for idx in (info["inp_piece"], info["out_piece"])
    }
    all_piece_indices = {int(r["PieceIndex"]) for _, r in pieces.iterrows()}
    last_piece_candidates = all_piece_indices - all_block_pieces
    assert len(last_piece_candidates) == 1, (
        f"Expected exactly one LastLayer piece, found {last_piece_candidates}"
    )
    last_piece_idx = next(iter(last_piece_candidates))

    # --- Encode each beam state as an int[97] permutation ---
    rows = []
    for candidate_idx, (ordering, _, score) in enumerate(beam):
        perm: list[int] = []
        for block_position in ordering:
            perm.append(assignments[block_position]["inp_piece"])
            perm.append(assignments[block_position]["out_piece"])
        perm.append(last_piece_idx)

        rows.append({
            "CandidateIndex": candidate_idx,
            "Permutation": json.dumps(perm),
        })
        logger.debug(f"[rank_orderings] Candidate {candidate_idx}: score={score:.4f}")

    logger.info(f"[rank_orderings] Emitting {len(rows)} candidate permutations")
    return pd.DataFrame(rows, columns=["CandidateIndex", "Permutation"])
