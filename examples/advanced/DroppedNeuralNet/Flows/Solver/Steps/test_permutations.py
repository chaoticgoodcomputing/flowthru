"""Search for the permutation of layer pieces that reproduces the recorded predictions.

Strategy:
  The original network is 48 residual Blocks followed by one LastLayer.
  Each Block consumes two pieces (inp + out). The candidate space is the set of
  legal (inp, out) pairings from LegalPairings, which each piece used exactly once.

  Phase 1 — constraint propagation:
    Build the bipartite assignment graph (inp pieces ↔ out pieces). Each Block
    position must assign a distinct inp and a distinct out piece.

  Phase 2 — forward-pass validation:
    For each candidate complete assignment, reassemble the network and run all
    10,000 historical rows through it. The permutation is correct when the
    network's output matches the recorded `pred` column within floating-point
    tolerance.
"""
import io
import logging
import pandas as pd
import torch
import torch.nn as nn
from flowthru import step

logger = logging.getLogger(__name__)

TOLERANCE = 1e-4   # max acceptable per-sample absolute error


class Block(nn.Module):
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
    def __init__(self, in_dim: int, out_dim: int):
        super().__init__()
        self.layer = nn.Linear(in_dim, out_dim)

    def forward(self, x):
        return self.layer(x)


def _load_linear(raw_bytes: bytes) -> nn.Linear:
    """Deserialize a single Linear layer from raw .pth bytes."""
    state_dict = torch.load(
        io.BytesIO(raw_bytes),
        weights_only=True,
        map_location=torch.device("cpu"),
    )
    out_features, in_features = state_dict["weight"].shape
    layer = nn.Linear(in_features, out_features, bias=True)
    layer.load_state_dict(state_dict)
    return layer


def _build_network(
    ordered_pieces: list[dict],
    last_piece: dict,
) -> nn.Sequential:
    """Assemble a Sequential network from an ordered list of Block piece-pairs.

    Args:
        ordered_pieces: List of (inp_bytes, out_bytes) dicts for each Block, in order.
        last_piece: Dict with 'Data' for the LastLayer piece.
    """
    modules = []
    for block_pieces in ordered_pieces:
        block = Block(in_dim=48, hidden_dim=96)
        block.inp = _load_linear(block_pieces["inp_data"])
        block.out = _load_linear(block_pieces["out_data"])
        modules.append(block)

    last = LastLayer(in_dim=48, out_dim=1)
    last.layer = _load_linear(last_piece["Data"])
    modules.append(last)

    return nn.Sequential(*modules)


def _forward_pass(network: nn.Sequential, X: torch.Tensor) -> torch.Tensor:
    with torch.no_grad():
        return network(X).squeeze(-1)


@step(
    inputs=["PieceMetadata", "PieceBlob", "BlockCandidate", "MeasurementSchema"],
    outputs="PermutationSolution",
)
def test_permutations(
    piece_metadata: pd.DataFrame,
    pieces: pd.DataFrame,
    legal_pairings: pd.DataFrame,
    historical_data: pd.DataFrame,
) -> dict:
    """Search over legal Block assignments to find the one that reproduces `pred`.

    Args:
        piece_metadata: Structural metadata (PieceIndex, InputDim, OutputDim, LayerType) — no blobs.
        pieces: Raw byte blobs (PieceIndex, Data) from the Pieces catalog item.
        legal_pairings: All legal (InpPieceIndex, OutPieceIndex) Block candidates.
        historical_data: Historical sensor data with `pred` column as the target.

    Returns:
        Dict matching PermutationSolution schema: {"Permutation": [int × 97]}.
    """
    logger.info("[test_permutations] Starting permutation search")

    # Join metadata to blobs by PieceIndex so we can classify and load in the same pass
    blob_by_index: dict[int, bytes] = {
        int(row["PieceIndex"]): row["Data"]
        for _, row in pieces.iterrows()
    }

    # Index metadata by PieceIndex
    meta_by_index = {
        int(row["PieceIndex"]): row
        for _, row in piece_metadata.iterrows()
    }

    # Separate LastLayer piece (metadata + blob)
    last_meta = next(
        (m for m in meta_by_index.values() if m["LayerType"] == "Last"), None
    )
    assert last_meta is not None, "Expected exactly one LastLayer piece"
    last_piece = {
        "PieceIndex": int(last_meta["PieceIndex"]),
        "Data": blob_by_index[int(last_meta["PieceIndex"])],
    }

    # Build inp/out lookup dicts: PieceIndex → blob bytes (only need the bytes for assembly)
    inp_blobs: dict[int, bytes] = {
        int(m["PieceIndex"]): blob_by_index[int(m["PieceIndex"])]
        for m in meta_by_index.values()
        if m["LayerType"] == "BlockInp"
    }
    out_blobs: dict[int, bytes] = {
        int(m["PieceIndex"]): blob_by_index[int(m["PieceIndex"])]
        for m in meta_by_index.values()
        if m["LayerType"] == "BlockOut"
    }

    n_blocks = 48
    feature_cols = [f"measurement_{i}" for i in range(48)]
    X = torch.tensor(historical_data[feature_cols].values, dtype=torch.float32)
    pred = torch.tensor(historical_data["pred"].values, dtype=torch.float32)

    # Build candidate pairing list
    pairings = [
        (int(row["InpPieceIndex"]), int(row["OutPieceIndex"]))
        for _, row in legal_pairings.iterrows()
    ]

    # Use a recursive search with arc-consistency pruning.
    # Each Block position must pick a unique inp piece and a unique out piece.
    solution_permutation: list[int] | None = None

    def search(
        block_idx: int,
        assignment: list[tuple[int, int]],  # (inp_piece_idx, out_piece_idx) per block
        used_inp: set[int],
        used_out: set[int],
    ) -> bool:
        nonlocal solution_permutation

        if block_idx == n_blocks:
            # Validate: assemble network and forward-pass
            ordered = [
                {
                    "inp_data": inp_blobs[inp_idx],
                    "out_data": out_blobs[out_idx],
                }
                for inp_idx, out_idx in assignment
            ]
            network = _build_network(ordered, dict(last_piece))
            network.eval()
            result = _forward_pass(network, X)
            max_err = float((result - pred).abs().max())
            logger.debug(f"[test_permutations] Candidate max_err={max_err:.6f}")
            if max_err < TOLERANCE:
                # flatten: inp0, out0, inp1, out1, ..., last
                perm = []
                for inp_idx, out_idx in assignment:
                    perm.append(inp_idx)
                    perm.append(out_idx)
                perm.append(int(last_piece["PieceIndex"]))
                solution_permutation = perm
                return True
            return False

        for inp_idx, out_idx in pairings:
            if inp_idx in used_inp or out_idx in used_out:
                continue
            assignment.append((inp_idx, out_idx))
            used_inp.add(inp_idx)
            used_out.add(out_idx)
            if search(block_idx + 1, assignment, used_inp, used_out):
                return True
            assignment.pop()
            used_inp.discard(inp_idx)
            used_out.discard(out_idx)

        return False

    logger.info("[test_permutations] Beginning recursive search over block assignments")
    found = search(0, [], set(), set())

    if not found or solution_permutation is None:
        raise RuntimeError(
            "No valid permutation found — check piece classification and historical data"
        )

    logger.info(f"[test_permutations] Solution found: {solution_permutation}")
    return {"Permutation": solution_permutation}
