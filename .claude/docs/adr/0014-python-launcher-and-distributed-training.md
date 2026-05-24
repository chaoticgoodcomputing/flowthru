# Python launcher abstraction and distributed training

The Python extension's worker subprocess is spawned via a swappable `IPythonLauncher` rather than the hardcoded `[pyExe, workerScript]` invocation in `SubprocessPythonExecutor.StartWorker`. The default preserves current behavior; alternative launchers unlock single-box multi-GPU training (PyTorch DDP, HuggingFace Accelerate, etc.) without forking the executor. The seam is designed alongside a rank-aware worker protocol so a step author writes ordinary single-process Python and the framework handles fanout. Requirements declared by each launcher fold into the [Python requirements algebra](0013-python-requirements-algebra.md).

## The launcher seam

`IPythonLauncher` is a `SubprocessPythonExecutor` dependency, not a parameter on `AddPythonStep`. Per-step launcher selection falls out naturally from per-step executor selection (already a parameter), avoiding a 64-overload source-generator change and avoiding leaking a `SubprocessPythonExecutor`-specific concept through executor implementations that have no launcher concept (in-process Python.NET, gRPC).

Four members:

- `ProcessStartInfo Build(string pyExe, string workerScript, IReadOnlyDictionary<string, string> envVars)` — constructs the worker PSI. Launcher controls the final env-var merge so launcher-set rank vars (`RANK`, `LOCAL_RANK`, `WORLD_SIZE`, `MASTER_ADDR`, `MASTER_PORT`) overlay cleanly on top of the existing IConfiguration→env-var bridge.
- `string Identity { get; }` — folds into `PythonCodeVersion.Derive` so a launcher change invalidates cached results. DDP outputs are not bitwise-reproducible across `nproc_per_node` changes; treating launcher choice as cache-equivalent would be wrong.
- `Validated<PythonPreFlightError, FlowUnit> Probe()` — pre-flight check the launcher can do that nothing else can: `TorchrunLauncher` reads `nvidia-smi -L` and refuses `nproc_per_node > available`; `AccelerateLauncher` validates the user's `accelerate config`. This is what makes per-launcher classes worth shipping versus a generic `ProcessLauncher`.
- `IReadOnlyList<PythonPackageRequirement> Requirements` — fed into the algebra in [ADR-0013](0013-python-requirements-algebra.md). `AccelerateLauncher → accelerate>=0.30`, `DirectPythonLauncher → []`.

Every per-launcher class exposes a `BinaryPath` override defaulting to `Path.Combine(venvBin, "<launcher-name>")` so site-specific renames (`lab-srun`, `mycorp-torchrun-wrapper`) don't require a new class.

## Per-launcher classes, not a generic launcher

Ship `DirectPythonLauncher` (default, today's behavior), `TorchrunLauncher`, `AccelerateLauncher` in core; `IPythonLauncher` as the seam for everything else (DeepSpeed direct, MosaicML composer, Lightning Fabric, SLURM-wrapped invocations). Generic `ProcessLauncher { Binary, PreArgs: string[] }` was rejected on two grounds:

- `Probe` is launcher-specific. A generic launcher reduces probing to "the binary exists" — losing the differentiating value prop. Domain-specific probes (GPU count, framework config validity, NCCL availability) are why the launcher concept earns its keep.
- Type-safe knobs are the framework's reason for existing. `NProcPerNode` as `int` property is compile-time safe; `PreArgs: ["--nproc-per-node", "2"]` is a string array where `--nproc-per-node` (kebab) vs `--nproc_per_node` (snake) is a runtime error in tools that disagree on convention. The framework's job is to make the typo unrepresentable.

Bespoke launchers implement `IPythonLauncher` directly — ~15 lines for a probe-less case. The interface *is* the generic pattern, same as `ICatalogFormat` and `IStepExtension`. Two-class minimum for distributed (Torchrun and Accelerate) because Accelerate carries a transitive `pip install accelerate` cost that torch-only shops shouldn't be forced into to get DDP.

## Rank-aware worker protocol

`torchrun` (and any DDP-style launcher) spawns N python processes sharing the parent's stdout by default. The current JSON-on-stdout protocol breaks immediately: rank 5's `print()` interleaves with rank 0's result frame and corrupts the line. Two changes to `flowthru_worker.py`:

- **Detect `RANK` at startup.** Non-rank-0 workers skip the stdin/stdout init handshake entirely (they have no parent pipe). They import the user's module and call into the user function — which delegates to whatever distributed-aware framework the user picked (HuggingFace `Trainer`, Accelerate, Lightning), all of which handle rank coordination internally.
- **Move the protocol off stdout onto a dedicated file descriptor.** Rank 0 reads init and writes results on a dedicated fd; stdout becomes free for user `print()` and library logging without protocol corruption. The launcher contract is aware of which fd carries the protocol so torchrun-class launchers can plumb the right `--redirects` flag while `DirectPythonLauncher` leaves it on stdout for backcompat. **Which fd is reserved for the protocol is deferred to implementation** — torchrun's `--redirects 3:0` convention is the leading candidate but warrants experimentation against the actual architecture before committing in the contract.

The step author writes ordinary single-process Python: import the trainer, call `trainer.train()`, return the result. Rank 0 is the only rank that returns to Flowthru — non-rank-0 ranks exit silently after their participation in the trainer's coordination code path.

## Rejected alternatives

- **Executor-wide command template (`PythonLauncherCommand = "torchrun --nproc_per_node=2 {workerScript}"`).** Zero new abstractions, but applies executor-wide; pays `torchrun` startup on every Python step in a 20+ step flow when only one step needs it. Per-step granularity via per-executor selection is the right axis.
- **`launcher:` parameter on `AddPythonStep`.** Would push a `SubprocessPythonExecutor`-specific concept through 64 source-generated overloads.
- **Tier 2 "rank 0 broadcasts init via `torch.distributed.broadcast_object_list`" (deferred).** The cleaner version where rank 0 reads init then broadcasts to ranks 1..N-1 after `init_process_group` is on the roadmap once the launcher seam stabilizes. The current "delegate to the trainer's internal coordination" path covers the common case; the broadcast version is a polish item, not a v1 requirement.
- **Flowthru-as-rank (the SLURM/k8s case).** Detecting that the .NET process itself is one of N ranks pre-launched by SLURM or PyTorchJob is a different problem and arguably crosses the "not an orchestrator" line in [/CONTRIBUTING.md](/CONTRIBUTING.md). Deferred until a concrete production-cluster use case surfaces; would warrant its own ADR.

## Why this matters for the ML pitch

Single-box multi-GPU is the floor for ML to be a credible use case for Flowthru. Without the launcher seam, every multi-GPU training step is a fork of the Python extension. With the seam plus the requirements algebra plus the rank-aware worker, distributed training is a one-line opt-in (`new AccelerateLauncher { NumProcesses = 2 }`) with the *exact* fail-fast surface the framework is built on: missing `accelerate`, wrong GPU count, conflicting deps — all caught before the first batch loads.
