# File-based Python transit and IFlowResourceProvider

The Python step boundary serializes all inputs/outputs — including large tabular data — as base64 Arrow IPC embedded in a JSON line over stdin. This hits `System.Text.Json`'s value-length cap at ~160 MB and wastes memory on the 4/3× base64 inflation for every payload regardless of size. We're replacing the inline encoding with file-based transit: tabular data writes to Arrow IPC files on disk, bytes write to raw binary files, and the JSON envelope shrinks to metadata + file paths. To manage the transit scratch directory lifecycle with error-aware cleanup ("preserve on failure for debugging"), we're introducing `IFlowResourceProvider` — a new Core interface that generalizes `FlowResource` discovery beyond catalogs to any DI-registered service.

## Considered Options

**Transit format:**

- *Raise `System.Text.Json` `MaxValueLength`* — fixes the immediate cap but keeps hundreds of MB flowing through stdin as base64 JSON. Treats the symptom.
- *Adaptive threshold (inline under N MB, file above)* — two code paths per kind, threshold tuning, worker must handle both encodings. Complexity for marginal benefit on small payloads.
- *Always file for bulk kinds* ← chosen. One code path per kind. Tabular → Arrow IPC file, bytes → raw binary file, scalar stays inline. Disk I/O on small tables is negligible for ETL workloads.
- *Pass Parquet file paths directly for Parquet-backed items* — rejected because it couples the Parquet extension to the Python extension. Arrow IPC is the boundary contract regardless of backing storage.

**File lifecycle:**

- *Per-invocation temp dir, always delete* — no residue but hard to debug failures.
- *Persistent scratch dir with explicit cleanup* — files survive but need manual cleanup.
- *Per-invocation temp dir, keep-on-error* ← chosen. Modeled as a `FlowResource`: acquire creates the dir, release deletes on success (when `bodyError` is null) and preserves on failure.

**Resource provider scope:**

- *Catalog-only (status quo)* — `FlowResource` stays on `CatalogAbstract`. Python extension manages its own lifecycle outside the framework.
- *`IFlowResourceProvider` with per-flow + per-operation in Core* — framework defines both bracket granularities. Rejected: per-operation resources are consumed within the same extension that provides them; the framework can't do anything useful with the opaque `TScope`.
- *`IFlowResourceProvider` with per-flow only in Core* ← chosen. `FlowthruService` discovers providers via DI alongside catalog resources and runs the same acquire/LIFO-release bracket. Extensions handle per-operation brackets internally using `FlowResource<TScope>` directly. Promote to Core if a cross-extension pattern emerges.

## Consequences

- **Protocol change**: the Python worker protocol is co-versioned (worker .py ships in the NuGet package's `build/` directory), so no version negotiation is needed. The protocol comment block in `flowthru_worker.py` and the encoding section update in lockstep with the C# executor.
- **C# controls all file paths**: the invoke request includes output file paths for bulk kinds. The worker reads from and writes to paths it's given. Cleanup responsibility is entirely on the C# side (via the FlowResource bracket).
- **Both directions**: inputs and outputs use file-based transit for bulk kinds.
- **Core surface area**: `IFlowResourceProvider` is a small interface (`IFlowResource? FlowResource => null;`). `FlowthruService` adds a second discovery pass for `IFlowResourceProvider` registrations alongside the existing `CatalogAbstract.Resource` collection.
