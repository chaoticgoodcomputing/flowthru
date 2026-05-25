"""Distributed training step exercising TorchrunLauncher.

Single-step example: train a small CNN on synthetic MNIST-shaped
(28×28 grayscale) data using PyTorch DDP. All ranks participate in the
forward/backward pass and gradient synchronization. Only rank 0 returns
the trained weights through the Flowthru protocol; ranks 1..N-1 join
the distributed training and then exit.

This example is purpose-built to surface the slice-5 blockers — see
the project README for the catalogue of failure modes it reproduces.
"""

import logging
import os
import pickle

import torch
import torch.distributed as dist
import torch.nn as nn
import torch.nn.functional as F
import torch.optim as optim
from torch.nn.parallel import DistributedDataParallel as DDP
from torch.utils.data import DataLoader, TensorDataset

from flowthru import step

logger = logging.getLogger(__name__)


class SmallCnn(nn.Module):
    """Tiny convolutional net — two conv layers + a fully-connected head."""

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


@step(inputs=["TrainingConfigSchema"], outputs=["bytes"])
def train_ddp(config: dict) -> bytes:
    """Train SmallCnn via DDP. Returns pickled state_dict on rank 0."""

    rank = int(os.environ.get("RANK", "0"))
    world_size = int(os.environ.get("WORLD_SIZE", "1"))

    # Per-rank stdout/stderr noise — deliberate, so the slice-4
    # interleaving problem on a shared parent stdout is observable.
    # Slice 5 will move the Flowthru JSON protocol off stdout onto a
    # dedicated fd; until then this print collides with the protocol
    # on every rank.
    print(f"[rank {rank}/{world_size}] entered train_ddp", flush=True)

    if world_size > 1:
        # Initialize the process group using torchrun-provided env
        # vars (MASTER_ADDR, MASTER_PORT, RANK, WORLD_SIZE). gloo so
        # the example works on CPU-only CI; nccl would be the GPU
        # backend.
        dist.init_process_group(backend="gloo")

    # Synthetic data — every rank generates the same dataset via a
    # shared seed. A real example would shard the data via
    # DistributedSampler; we keep it identical here to focus on the
    # launcher / coordination problem rather than data partitioning.
    torch.manual_seed(42)
    x = torch.randn(config["NumSamples"], 1, 28, 28)
    y = torch.randint(0, 10, (config["NumSamples"],))

    model: nn.Module = SmallCnn()
    if world_size > 1:
        model = DDP(model)

    optimizer = optim.SGD(model.parameters(), lr=config["LearningRate"])
    loader = DataLoader(
        TensorDataset(x, y),
        batch_size=config["BatchSize"],
        shuffle=False,
    )

    for epoch in range(config["Epochs"]):
        for batch_x, batch_y in loader:
            optimizer.zero_grad()
            output = model(batch_x)
            loss = F.cross_entropy(output, batch_y)
            loss.backward()
            optimizer.step()
        # Per-rank epoch log — interleaves on stdout across ranks pre-slice-5.
        print(
            f"[rank {rank}/{world_size}] epoch {epoch + 1}/{config['Epochs']} "
            f"loss={loss.item():.4f}",
            flush=True,
        )

    # Barrier so rank 0 doesn't pickle before others finish their last step.
    if world_size > 1:
        dist.barrier()

    # Only rank 0 returns the state_dict to Flowthru. Other ranks fall
    # through to destroy_process_group and exit.
    if rank == 0:
        underlying = model.module if isinstance(model, DDP) else model
        result = pickle.dumps(underlying.state_dict())
        if world_size > 1:
            dist.destroy_process_group()
        print(f"[rank 0] returning {len(result)} bytes of pickled state_dict", flush=True)
        return result

    if world_size > 1:
        dist.destroy_process_group()
    # Non-rank-0 returns are discarded by the protocol — but with slice
    # 4's worker, only rank 0 even owns the protocol stream. This `b""`
    # is observed only by the unit tests / type checker.
    return b""
