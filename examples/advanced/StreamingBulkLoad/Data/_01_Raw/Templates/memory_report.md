# Streaming vs. Eager Bulk Load — Memory Report

> {{verdict}}

Both variants read the **same** multi-row-group Parquet dataset, applied the
**same** normalise + filter transform, and bulk-loaded the result into the
**same** SQLite `Transactions` table — `{{row_count}}` rows each. The only
difference is the memory grain: the eager path buffers the whole file
(`O(file)`); the streaming path pulls one row group at a time and writes one
batch at a time (`O(batch)`).

## Peak memory

| Variant   | Peak managed heap | Peak working set | Duration |
|-----------|------------------:|-----------------:|---------:|
| Eager     | {{eager_peak_managed_mb}} MB | {{eager_peak_ws_mb}} MB | {{eager_ms}} ms |
| Streaming | {{streaming_peak_managed_mb}} MB | {{streaming_peak_ws_mb}} MB | {{streaming_ms}} ms |

- **Managed-heap ratio:** streaming held peak to **{{managed_ratio_pct}}%** of eager.
- **Working-set ratio:** streaming held peak to **{{ws_ratio_pct}}%** of eager.

The managed-heap figure is the cleanest signal: eager keeps the whole decoded
dataset live as a `List`, while streaming keeps only a row group plus one write
batch. Working set is noisier (the runtime does not return pages to the OS
promptly, and streaming is measured first so it also absorbs one-time
JIT/assembly-load cost) — it is reported as a conservative, real-world number.

## Why this matters

On a memory-constrained host (AWS Lambda, a small ECS/Fargate container) the eager
path's peak scales with the file and eventually OOMs — the #111 crash-loop. The
streaming path's peak stays flat regardless of dataset size, so the same host
loads an arbitrarily large dataset. See the README for how to watch the eager
path OOM and the streaming path survive under `podman run --memory=…`.

<sub>Generated {{generated_utc}} by the StreamingBulkLoad Reporting Flow.</sub>
