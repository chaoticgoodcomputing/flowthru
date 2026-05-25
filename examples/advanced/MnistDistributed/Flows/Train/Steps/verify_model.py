"""Second distributed step — verifies the trained model via a forward
pass on all ranks. The point is *not* to do real validation
(synthetic data, no labels, no metric); the point is to exercise a
second invoke through the same TorchrunLauncher-backed executor,
proving that workers stay alive across multiple distributed calls.
"""

import logging
import os
import pickle

import torch
import torch.distributed as dist
import torch.nn as nn
import torch.nn.functional as F

from flowthru import step

logger = logging.getLogger(__name__)


class SmallCnn(nn.Module):
    """Mirror of train_ddp.SmallCnn — kept duplicated so each step can
    be read in isolation. Production code would share the module
    definition; example code prefers self-contained step files."""

    def __init__(self) -> None:
        super().__init__()
        self.conv1 = nn.Conv2d(1, 8, 3)
        self.conv2 = nn.Conv2d(8, 16, 3)
        self.fc = nn.Linear(16 * 5 * 5, 10)

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        x = F.relu(self.conv1(x))
        x = F.max_pool2d(x, 2)
        x = F.relu(self.conv2(x))
        x = F.max_pool2d(x, 2)
        x = x.flatten(1)
        return self.fc(x)


@step(inputs=["bytes"], outputs=["bytes"])
def verify_model(model_bytes: bytes) -> bytes:
    """Run one forward pass per rank on a deterministic synthetic
    input. Returns a pickled dict of {rank: forward_pass_norm} —
    rank 0 returns the dict, non-rank-0 returns nothing."""

    rank = int(os.environ.get("RANK", "0"))
    world_size = int(os.environ.get("WORLD_SIZE", "1"))

    print(f"[rank {rank}/{world_size}] entered verify_model", flush=True)

    # torch.distributed.is_initialized() is True from the first step's
    # init_process_group call, so we don't re-init here. This is what
    # makes multi-invoke through one worker pool tractable —
    # init_process_group is per-process, not per-invoke.
    if world_size > 1 and not dist.is_initialized():
        dist.init_process_group(backend="gloo")

    model = SmallCnn()
    model.load_state_dict(pickle.loads(model_bytes))
    model.eval()

    # Deterministic synthetic input, identical on every rank.
    torch.manual_seed(7)
    x = torch.randn(1, 1, 28, 28)
    with torch.no_grad():
        logits = model(x)

    rank_norm = float(logits.norm().item())
    print(
        f"[rank {rank}/{world_size}] forward-pass norm = {rank_norm:.4f}",
        flush=True,
    )

    if rank == 0:
        result = pickle.dumps({"rank_0_norm": rank_norm, "world_size": world_size})
        print(f"[rank 0] returning {len(result)} bytes of verification dict", flush=True)
        return result

    return b""
