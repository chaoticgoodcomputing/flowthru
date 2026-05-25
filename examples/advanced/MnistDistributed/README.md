# MnistDistributed — distributed PyTorch training via TorchrunLauncher

A one-step Flowthru pipeline that trains a small CNN on synthetic
28×28 grayscale data using `TorchrunLauncher` (ADR-0014). The model
and dataset are deliberately tiny — a few KB of model weights, a few
hundred synthetic samples — because the example is for **exercising
the distributed launcher seam**, not for training quality.

## What it does

```
[rank 0/2] entered train_ddp
[rank 0/2] epoch 1/2 loss=2.2959
[rank 0/2] epoch 2/2 loss=2.2875
[rank 0] returning 23063 bytes of pickled state_dict
✓ TrainCnnDistributed (2740.25 ms)
```

`TorchrunLauncher { NProcPerNode = 2 }` resolves the venv's
`torchrun` binary, spawns two Python workers via
`torchrun --nproc_per_node=2 flowthru_worker.py`, and lets PyTorch's
gloo backend coordinate gradient synchronization across both ranks.
Only rank 0 returns the trained `state_dict` to the C# catalog;
rank 1's stdout is redirected to a per-rank log file (`/tmp/torchelastic_*`)
to keep the Flowthru protocol stream clean on the parent pipe.

Running with `NProcPerNode = 1` works identically — the rank-0 path
gracefully degrades to single-process training.

## How the rank coordination works (slice 5)

The slice-5 worker (`flowthru_worker.py`) branches on `RANK` /
`WORLD_SIZE` env vars (set by torchrun) at startup:

- **Single-rank** (`WORLD_SIZE == 1`): existing protocol — read init
  from stdin, respond, loop on stdin for invokes, exit on shutdown.
- **Rank 0 in distributed**: read init + invoke from stdin as usual,
  but **also publish each message** to a sequenced broadcast file in
  `$TMPDIR/flowthru-bcast-<master-addr>-<master-port>-<run-id>/`.
  Return the result via stdout. Exit after one invoke (single-shot;
  torchrun expects to launch fresh per DDP cycle).
- **Non-rank-0**: redirect own stdout to `/dev/null` (no protocol
  corruption), poll the broadcast directory for the next sequenced
  file, dispatch the message locally, discard the response. Exit
  after one invoke.

All ranks call the user step function identically — DDP / NCCL / gloo
coordination is the user code's responsibility (via
`torch.distributed.init_process_group`, `DDP` wrapping, etc.). Rank
1 publishes nothing back to the host.

## Slice-5 limitations / single-shot constraint

Distributed launches are **single-shot** in the current
`SubprocessPythonExecutor`: after one invoke completes, all workers
have exited and the executor can't serve another invoke against the
same launch. Practical implication: a flow with multiple distributed
steps should use **one `SubprocessPythonExecutor` per step** (each
constructed with its own `TorchrunLauncher`). Sharing a single
distributed executor across steps will fail with a broken-pipe error
on the second invoke.

Multi-step distributed flows that share rank coordination are
deferred until a concrete use case shows up.

## Running

```bash
cd examples/advanced/MnistDistributed
uv sync                       # materializes .venv with torch + pyarrow
dotnet run                    # runs with NProcPerNode = 2
```

Edit `Program.cs` to change `NProcPerNode` — the rank-0 single-rank
path and the rank-0/rank-N>0 distributed path both work via the same
worker entry point.
