# MnistDistributed — tracer-bullet for distributed Python training

A one-step Flowthru pipeline that asks PyTorch to train a small CNN on
synthetic 28×28 grayscale data using `TorchrunLauncher` (ADR-0014).
The model and dataset are deliberately tiny — a few KB of model
weights, a few hundred synthetic samples — because **this example is
not for training quality**, it is for exercising the launcher seam and
**reproducing the slice-5 protocol-coordination blockers** in a
controlled setting.

## What works today

With `NProcPerNode = 1` in `Program.cs`:

```
[rank 0/1] entered train_ddp
[rank 0/1] epoch 1/2 loss=2.2959
[rank 0/1] epoch 2/2 loss=2.2875
[rank 0] returning 23063 bytes of pickled state_dict
✓ TrainCnnDistributed (2362.95 ms)
```

End-to-end success. `TorchrunLauncher` resolves the venv's `torchrun`
binary, spawns one Python worker via `torchrun --nproc_per_node=1
flowthru_worker.py`, the worker speaks the JSON-over-stdio protocol,
and the trained `state_dict` flows back to the C# catalog as bytes.
That single-rank path is the proof that the launcher seam — the
`IPythonLauncher` abstraction from slice 1, wired through `UsePython()`
to `SubprocessPythonExecutor` — is structurally sound.

## What's broken — reproduced failure modes

With `NProcPerNode = 2`, the run **hangs** at `init_process_group`.
The root cause is a **stdin race**, not stdout interleaving as
ADR-0014 originally framed it:

1. `torchrun --nproc_per_node=2` spawns two `flowthru_worker.py`
   children.
2. Both children inherit the parent's stdin file descriptor — the
   single pipe that carries the Flowthru init message.
3. They both attempt to read from that pipe at startup. **One wins**;
   it parses the init JSON, calls the user step. **The other starves**
   on an empty pipe.
4. The winner reaches `dist.init_process_group(backend="gloo")` and
   blocks waiting for the other rank to join.
5. The loser is stuck in `sys.stdin.readline()` and never reaches
   `init_process_group`.
6. Deadlock. No timeout, no error — just hang.

Worse, **which rank wins is non-deterministic**. Sometimes rank 0
wins and rank 1 starves; sometimes rank 1 wins and rank 0 starves.

### Things that don't fix it

- **`TorchrunLauncher.RedirectsFlag = "1:3"`.** Redirects ranks
  1..N-1's *stdout/stderr* to per-rank log files. Doesn't touch
  *stdin* — the race is unaffected. Confirmed: still hangs.
- **`--master_addr` / `--master_port` overrides.** Process-group
  config can't help because the loser never reaches the call.
- **Larger model / more epochs.** Orthogonal — the hang is at
  pre-training process-group formation.

### Three slice-5 blockers this example surfaces

The slice-5 fix needs to handle all three, in order of cause-and-effect:

| # | Failure                  | Mechanism                                                                                              | Slice-5 fix candidate                                                                                                                                                                      |
| - | ------------------------ | ------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| 1 | **Stdin race**           | Multiple workers contend for the same parent-stdin pipe.                                               | Worker reads `RANK` env at startup; if `RANK != 0`, skip the stdin read entirely.                                                                                                          |
| 2 | **Non-rank-0 dispatch**  | If non-rank-0 doesn't read stdin, it has no init message — doesn't know which user function to call.   | Rank 0 broadcasts the init payload (module/function/args) to all ranks via `torch.distributed.broadcast_object_list` after `init_process_group`. Non-rank-0 receives + dispatches locally. |
| 3 | **Stdout interleaving**  | Once all ranks reach the user function, their `print()` output multiplexes onto shared parent stdout. | Move the JSON protocol off stdout onto a dedicated fd. `TorchrunLauncher` adds `--redirects` to fan ranks 1..N-1's stdout to per-rank files; rank 0's stdout becomes free for the protocol. |

Slice 4's `TorchrunLauncher.RedirectsFlag` is enough to mitigate (3)
*if* (1) and (2) are fixed first — but on its own it's useless
because the run dies at (1) before reaching (3).

## Running

```
cd examples/advanced/MnistDistributed
uv sync                       # materializes .venv with torch + pyarrow
dotnet run                    # NProcPerNode=2 → hangs (see above)
```

For the working single-rank path: edit `Program.cs`, set
`NProcPerNode = 1`, rebuild, rerun.

## Why this example exists

Slice 4 (ADR-0014) shipped `TorchrunLauncher` + `AccelerateLauncher`
as a structural foundation: launchers that *can* express
distributed-training entry points without forking
`SubprocessPythonExecutor`. But the protocol the worker speaks was
designed for a single Python process; it doesn't survive being
fan-out by torchrun. The slice-4 work didn't address that gap because
the right fix needs empirical decisions (which fd carries the
protocol, which broadcast mechanism rank 0 uses, etc.) that are
easier to make against a concrete reproducer than from theory.

That's this example. It is **not** a working distributed-training
demo; it is the **specification** for what slice 5 needs to deliver.
When slice 5 lands, the same example with `NProcPerNode = 2` should
run end-to-end without modification — and the README's "Reproduced
failure modes" section should describe history rather than the
present.
