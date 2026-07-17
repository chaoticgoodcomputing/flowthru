# Security

## Reporting a vulnerability

Please report suspected vulnerabilities privately via the repository's security
advisories (**Security → Report a vulnerability**) on
[chaoticgoodcomputing/flowthru](https://github.com/chaoticgoodcomputing/flowthru),
rather than a public issue. We aim to acknowledge within a few business days.

## Attestations

This section documents security decisions an automated scanner may re-raise, so
a human reviewer has the rationale on hand. Each is a deliberate, reviewed
design recorded in an ADR.

### AWS S3 credential handoff to a native engine (`DATA_EXFILTRATION` — accepted false positive)

**What a scanner sees.** `AmazonS3Gateway.LocateObject`
(`src/extensions/Flowthru.Extensions.AWS.S3/...`) resolves AWS credentials
through the standard chain and returns them on a `ByteLocation.RemoteUri`. A
taint/heuristic scanner keys on *resolver → secret read → escape via return
value* and flags it as data exfiltration.

**Why it is not exfiltration.** The handoff is entirely **in-process**. When two Flowthru extensions
wrap stacks that can already talk to each other natively, the aim is to let them do so if possible.
However, this conflcits with Flowthru's policy of not tightly coupling any two extension stacks. The
policy, when that happens, is to secure any secrets necessary for native interactions via `.Core` securely
and in-process.
  - A precise example is S3 and DuckDB. The embedded DuckDB engine running a wide transform over
`s3://` Parquet reaches the object with its *own* S3 client, and must be handed
credentials to authenticate to **the same bucket the pipeline already targets**.
Nothing is transmitted to any external or unauthorized destination. Consumer-side
credential resolution (DuckDB's `credential_chain`) was considered and rejected
because it authenticates the engine as a *different principal* than the gateway
read, silently breaking explicitly-wired clients (LocalStack/MinIO). See
**[ADR-0026](.claude/docs/adr/0026-typed-access-handoff-and-secret-containment.md)**
(and ADR-0020, ADR-0024).

**Controls that make the residual risk defensible.** Credentials are:

- **Typed and contained.** They ride inside `SecretText`, whose `ToString` is
  redacted and whose serialization throws — so a handoff cannot leak them into a
  log, an error, or a persisted run record by accident (enforced by tests:
  `RemoteAccessContainmentTests`, `SecretTextTests`).
- **Contained at every reveal site.** A secret becomes plaintext only where a
  consumer needs it (the gateway's resolution boundary; DuckDB's `CREATE SECRET`
  SQL). Each reveal site scrubs its own failures and retains no raw exception, so
  no `RuntimeError.Message` — and therefore no metadata/JSON run record — can
  carry credential material.
- **Never persisted.** They are minted per call, never written to the catalog,
  the DAG, or disk; the DuckDB secret is temporary and dies with the transform's
  in-memory connection.
- **Guarded at design time (in progress).** A syntactic `Reveal()`-position
  analyzer flags a revealed credential used in a logging / interpolation /
  destructuring argument position. It is a position check, not full taint
  tracking; see ADR-0026.
