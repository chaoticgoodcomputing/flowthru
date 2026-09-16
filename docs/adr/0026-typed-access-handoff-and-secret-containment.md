---
status: accepted
---

# The byte-location access handoff is a Core-owned closed sum of contained secrets — mediums and consumers meet through the type, secrets are contained at every reveal site, and a re-scan is answered by attestation, not source-silencing

[ADR-0024](./0024-engine-delegated-wide-transforms-and-native-bulk-transfer.md) gave Core a **byte-location capability** so an engine extension can address an item's bytes without loading rows — "file path, S3 URI + credential handoff via the existing gateway seams." [ADR-0020](./0020-s3-storage-medium-via-gateway-seam.md) built that handoff on the `IS3Gateway` seam and fixed the invariant worth keeping: **a secret never enters the catalog or the DAG.** The realisation, `AmazonS3Gateway.LocateObject`, mints the handoff as `ByteLocation.RemoteUri.Access` — an `IReadOnlyDictionary<string,string>` of `access_key_id` / `secret_access_key` / `session_token` / `region` / `endpoint` / `url_style` — and `DuckDbS3SecretSql` reads those six keys back to build a DuckDB `CREATE SECRET`. It works, and the secret's lifetime is honest (minted at locate time, dead with the transform's in-memory connection). But the *shape* carries failures that a security-scan flag surfaced and two adversarial review panels sharpened:

**A stringly-typed protocol smuggled through a Core type.** `DuckDbS3SecretSql` re-declares the six keys as private constants and its own doc comment concedes "the access vocabulary is the S3 gateway's." The two extensions reference only Core — but they are coupled through an undocumented string contract Core disclaims. A key typo is a **runtime** error, and an unknown minted key is **silently ignored** — the silent-downgrade / MagicAtlas failure shape [ADR-0024](./0024-engine-delegated-wide-transforms-and-native-bulk-transfer.md) already rejected.

**Raw secrets on a general-purpose record.** The material is plain `string`s on a `record`, whose synthesised `ToString`/`PrintMembers` means `$"{remoteUri}"` in any log line or exception prints live AWS credentials. The panel's red-team pass proved this is not the only place a revealed secret meets a persisted string: when credential resolution *throws*, the AWS SDK exception is lifted generically into `RuntimeError.External(Cause)`, whose `Message` recomputes `$"…: {Cause.Message}"` on demand from the **retained raw `Exception`**, flows into `FlowMetadataProjection.FailureMessage`, and is `JsonSerializer.Serialize`d to a run file — a live secret-to-disk path. Critically, that sink lives in an **extension** (`Flowthru.Extensions.Metadata.Json`), and its siblings (Mermaid, Diagnostics, any third-party `IMetadataProvider`) read the same `Error.Message`, so no *downstream* scrub can be "inherited for free."

**The decision** has two halves answering two different questions the panels insisted on separating:

1. **Fix the real risk.** The handoff becomes a **Core-owned closed sum** (`RemoteAccess`) whose secret fields are a **Core containment type** (`SecretText`); the credential vocabulary moves from a doc comment into Core's type system, where a wrong field is a *design-time* error; the accidental-`ToString`/serialise surface is closed at the leaf; and the error-path leak is closed by **containing every secret at its reveal site** — the one choke point where a secret becomes a plaintext string — so no `RuntimeError.Message`, and therefore no metadata sink, can ever carry one. Backed by per-medium **laws** and a **design-time analyzer**, not case-author discipline.
2. **Answer the re-scan honestly.** Because credentials still transit (consumer-side `credential_chain` resolution is rejected on identity fidelity — below), a pattern-matching scanner *will* re-raise `DATA_EXFILTRATION` against `LocateObject`. That is not silenced at the source; it is **dispositioned** by documented risk-acceptance, machine-readable suppression, and a `SECURITY.md` attestation. Conflating "the risk is fixed" with "the scanner stops flagging" is the trap the first panel caught; this ADR keeps them distinct.

## Decided

### The handoff is a closed sum, not a dictionary

`RemoteUri.Access : IReadOnlyDictionary<string,string>` is replaced by a Core-owned closed sum built in `ByteLocation`'s own idiom (private constructor, nested sealed records, exhaustive `Match`):

- `RemoteAccess.Anonymous` — no handoff; consumer defaults apply (today's empty dictionary → `DuckDbS3SecretSql.Plan`'s `Access.Count == 0` skip). **Reserved meaning:** `Anonymous` is *only* "the medium hands off nothing." When consumer-side resolution ships it arrives as a *distinct additive case* (`DeferToConsumer`) — a non-breaking addition to the sum, never a second meaning grafted onto `Anonymous`.
- `RemoteAccess.S3Compatible(string? Region, Uri? Endpoint, bool ForcePathStyle, S3Credentials? Credentials)`, with `S3Credentials(SecretText KeyId, SecretText SecretKey, SecretText? SessionToken)`. Field names mirror `S3Options`; `Endpoint` stays a `Uri` — DuckDB's host/`USE_SSL` split is consumer-specific rendering.
- Future cases are *honestly different*: `AzureBlobSas(...)`, `BearerToken(...)`. A flat universal schema would be a lie.

**Core naming an `S3Compatible` case does not break medium-neutrality — it restores it.** The S3-compatible shape is a de-facto protocol (AWS, MinIO, R2, LocalStack); the DuckDB consumer already branches on `uri.Scheme == "s3"`. Typing the case in Core adds zero protocol knowledge to any consumer; it moves the vocabulary from a doc comment into the type system.

Two consumer rules replace "unknown keys are ignored":

- Consumers `Match` exhaustively; an unservable case is a **typed error** (`AccessKindUnsupported`), never a silent skip. Exhaustiveness over the delegate-based `Match` is enforced by the existing `ClosedSumExhaustivenessAnalyzer` (`FT0001`), configured **error-severity** for this contract (below) so it is a true design-time gate.
- Core exposes each case's `Secrets` as an `IReadOnlyList<SecretText>` — the single scrub-list vocabulary, though (see reveal-site containment) the scrub at a *throw* site uses the credentials in local scope, not a post-hoc `Secrets` list.

### Secrets are a Core `SecretText` — containment, not cryptography, with honestly-scoped limits

`SecretText` is a small, hand-rolled Core type that makes *accidental* disclosure through **`ToString`-based rendering** structurally impossible:

- `ToString()` → `"[redacted]"`; a `[DebuggerDisplay]` matches. A containing `record`'s synthesised `ToString` recurses into a redacted leaf, so `RemoteUri`, `S3Compatible`, and a `DuckDbTransformRequest` in a debugger are safe by composition.
- **No public value member.** The only way out is a single, greppable `Reveal()`. Sealed class, not `record`.
- A `[System.Text.Json.Serialization.JsonConverter]` that **throws on write** — serialising a handoff violates [ADR-0020](./0020-s3-storage-medium-via-gateway-seam.md)'s catalog/DAG invariant; failing fast beats emitting `{}`.
- Equality via `CryptographicOperations.FixedTimeEquals`.

**Honestly-scoped limits, documented on the type** (per [ADR-0008](./0008-documentation-honesty-three-error-phases.md)):

- The `ToString` guarantee holds only against **`ToString`-based renderers** (interpolation, `string.Format`, the default `Microsoft.Extensions.Logging` formatter, the debugger). It does **not** stop a **field-walking destructurer** — Serilog `{@handoff}` and reflection destructurers read fields, not properties, and never call `ToString`. Mitigation: don't destructure handoff types; the analyzer flags `{@…}` positions.
- The serialisation throw covers **System.Text.Json's reflection serializer**. **Newtonsoft is out of scope** (it ignores an STJ converter and reflects to `{}`), and STJ's **source-gen** context may not honour a runtime converter — enforced by test for the reflection path, documented as a boundary for the others, never claimed universal.
- **No memory-zeroing** — .NET strings are immutable and GC-copied. It cannot protect the *final* materialisation: the credential must become a plaintext `string` inside `CREATE SECRET` SQL. That surface is handled by reveal-site containment, next.

`SecureString` (DE0001) and `Microsoft.Extensions.Compliance.Redaction`-as-container are covered in Considered options.

### Secrets are contained at their reveal sites, so no error can surface them

A secret becomes a plaintext `string` at exactly two **reveal sites**: credential resolution inside `S3StorageMedium.LocateBytes`, and the `CREATE SECRET` SQL in the DuckDB consumer. The panel proved the earlier draft's "the `External` envelope applies the `Secrets` scrub" is unbuildable — `RuntimeError.External` is minted generically in `FlowIO.LiftAsync` (one of ~15 call sites) with **no `RemoteAccess` in scope**, and on the throw path the `Secrets` list is the un-returned output of the call that threw. So the scrub is placed where the material is actually in hand — the reveal site — and the generic `External`/`FlowIO` machinery is **left unmodified**:

- **`LocateBytes` catches resolution failures inside its own effect**, where the credentials it is resolving are local, scrubs the message against *those* values, and returns a `RuntimeError` that **retains no raw `Exception`** carrying the secret (the `Cause` is dropped or replaced with a redacted-message exception). Scrubbing a *copy* of `.Message` is insufficient — `External.Message` recomputes from `Cause` on demand — so containment happens at *mint*, not at read.
- **The DuckDB consumer keeps `RedactForRequest`** for the `CREATE SECRET` path: it has the `DuckDbTransformRequest`'s endpoints in scope, so it is already a correct reveal-site scrub.
- **The scrub itself is hardened**: values under a minimum length are skipped, replacement is longest-first, and an empty value never matches — avoiding the over-/under-redaction a naive `string.Replace` invites.

Because the error reaching `StepResult.Failed.Error` was **contained at birth**, every metadata sink — `Flowthru.Extensions.Metadata.Json`, Mermaid, Diagnostics, and any third-party `IMetadataProvider` — inherits a clean `Error.Message` with **no per-sink work**. This is the honest "inherited for free": inherited because the error was never poisoned, not because each sink (in an extension Core cannot reach) re-implements a scrub. The invariant — *no `RuntimeError.Message` can contain a revealed secret* — is a property of the reveal sites plus a verifying law, **not** a structural property of the `External` type.

### Structural enforcement: laws + a design-time analyzer

The closed sum makes *field-level* correctness a compile error, but a future `AzureBlobSas` case could still type its secret as a bare `string` or forget to list it in `Secrets`. Backstops make containment a property of *every* medium, not of the author's memory:

- **Per-medium laws** in `ISupportsByteLocationLaws`: a located `RemoteAccess` (i) throws on serialisation; (ii) renders a `ToString()` containing none of its `Secrets`' values; (iii) a failed run whose error text contains a known credential serialises `[redacted]` to the metadata JSON (exercising the reveal-site containment end-to-end).
- **A reflection law closing the enumeration hole the panel found:** every `SecretText`-typed field on a case must be reachable from that case's `Secrets`. Without it, a case that types a token as `SecretText` but forgets to enumerate it passes laws (i)–(iii) *vacuously* (nothing in `Secrets` to find) and still leaks. (Residual, documented limit: a case author who types a genuine secret as a bare `string` defeats containment entirely — undetectable by reflection; the analyzer and review are the only controls, and this is stated as a known limit.)
- **A Roslyn analyzer** flagging `Reveal()` in string-interpolation / logging / `{@…}` argument positions. It is **syntactic — argument-position only**, in the house style of the shipped `FailAsValueThrowAnalyzer` (single-node, no data-flow, skips lambda bodies). It does **not** track `var s = x.Reveal(); log(s)` or `Helper(x.Reveal())` across statements or methods; the ADR and the `SECURITY.md` attestation must describe it as a position check, not a taint guarantee. It is **sequenced as a separate work item** that must merge before `SECURITY.md` cites it; the type change + laws land first and hold the invariant by test.
- **Severity is made honest:** the repo has no global `TreatWarningsAsErrors`, and `FT` analyzers ship at `Warning`. This ADR configures the containment/exhaustiveness diagnostics (`FT0001` and the `Reveal()` analyzer's code) to **error severity via `.editorconfig`** (`dotnet_diagnostic.FTxxxx.severity = error`) — a surgical per-code bump, not a repo-wide flip — so the "design-time" claims are true build-breaking gates rather than suppressible warnings.

### The scanner will re-flag `DATA_EXFILTRATION`; the disposition is attestation, not source-silencing

`LocateObject` retains the pattern a scanner keys on — resolver → `.AccessKey`/`.SecretKey` reads → escape via the returned `ByteLocation`. Wrapping the value in `SecretText` changes the *sink type*, not the taint flow (and a symbol named `SecretText`/`Reveal()` may *raise* sensitivity). Removing the trigger requires consumer-side `credential_chain`, rejected below on identity fidelity. So a re-scan is expected to re-raise `DATA_EXFILTRATION`, **accepted as a false positive** (in-process handoff to the same bucket). The disposition:

- **Machine-readable suppression, per scanner class**, each with a justification and (where supported) an expiry: a Socket ignore (`socket.yml`), a Snyk `.snyk` ignore, an inline annotation at `LocateObject`.
- **A `SECURITY.md` attestation** describing the in-process handoff, pointing at this ADR. Its cited structural controls are what *exist when it publishes*: the closed sum, `SecretText.ToString`, reveal-site containment, and the serialisation-refusal + enumeration laws. The `Reveal()`-position analyzer is cited as a *syntactic* control, added once merged — `SECURITY.md` must not claim a control that isn't built.
- **The doc-accuracy fix is complete across *both* skill copies** (`src/extensions/…/skill/SKILL.md`, the source of truth published to skills.sh, and the generated `.claude/skills/flowthru-s3/SKILL.md`) plus the README, closing the `CREDENTIALS_UNSAFE` re-trigger.

### Credentials keep moving; consumer-side resolution is an opt-in, not the contract

DuckDB can resolve its own credentials (`credential_chain`, via its `aws` extension), removing the scanner trigger. **Rejected as the contract** — all three round-1 reviewers upheld this — for three code-grounded reasons:

1. **Identity fidelity.** `UseS3(IFlowthruBuilder, IS3Gateway)` and the `AmazonS3Gateway(IAmazonS3)` constructor let a user wire a client with explicit credentials the ambient chain cannot reproduce (LocalStack/MinIO). Consumer-side resolution creates a split-brain where the medium reads and the engine authenticate as different principals — invisible until a permission boundary bites.
2. **Operational surface.** `credential_chain` needs a second native extension (`aws`) beyond `httpfs`.
3. **Generality.** Consumer-side resolution is engine-specific; the Core contract must serve future consumers and mediums whose credentials aren't ambient (a medium-minted SAS token).

The reserved `DeferToConsumer` case keeps `credential_chain` reachable as an opt-in — nothing is foreclosed.

### Placement and error phases

| Piece | Lives in | Failure it addresses | Phase |
|---|---|---|---|
| `SecretText` | Core (beside `ByteLocation`) | accidental leak via `ToString` / default-logger | design-time (structural) |
| `RemoteAccess` sum | Core (beside `ByteLocation`) | vocabulary typos (field) | design-time (compile error) |
| `AccessKindUnsupported` exhaustiveness | `ClosedSumExhaustivenessAnalyzer` `FT0001`, `.editorconfig` error-severity | half-understood handoffs | design-time (analyzer, error) |
| Containment + enumeration + redaction **laws** | `ISupportsByteLocationLaws` / `S3ByteLocationLaws` | a medium regressing containment or leaking via the JSON path | design-time (failing test) |
| `Reveal()`-position analyzer (syntactic) | Core `SourceGenerators`, `.editorconfig` error-severity | a `Reveal()` in a log/interp/`{@…}` argument position | design-time (diagnostic) |
| **Reveal-site containment** (scrub at mint, drop raw `Cause`) | S3 gateway (`LocateBytes`) + DuckDB consumer (`RedactForRequest`) | revealed secret reaching an error → any metadata sink → disk | runtime, contained at source |
| Minting `S3Compatible` | S3 extension (`AmazonS3Gateway.LocateObject`) | unresolvable credential chain | runtime, contained & typed via `FlowIO` |
| Scanner re-flag | `socket.yml` / `.snyk` / `SECURITY.md` | recurring false-positive `DATA_EXFILTRATION` | out-of-band (attestation) |
| Write-reachability | S3 sentinel (`InspectTarget`, ADR-0020) | denied / missing credentials | pre-flight (unchanged) |

## Considered options

- **Scrub at the generic `External` envelope / the metadata projection.** Rejected — the round-2 finding. `RuntimeError.External` is minted in `FlowIO.LiftAsync` with no `Secrets` in scope, and the JSON sink is an optional extension with siblings (Mermaid, Diagnostics, third-party providers) reading the same `Error.Message`; a boundary scrub would either thread secret-adjacent state through Core's generic error channel (an ADR-0020 tension) or force every provider to re-implement redaction — the per-consumer discipline this ADR exists to kill. Containment at the reveal site is the only formulation where the scrub-list is in scope, and it makes every sink safe with no per-sink work.
- **An open `IRemoteAccess` contract instead of a closed sum.** Rejected. It removes Core as the per-medium bottleneck but forfeits **exhaustive `Match`** — "unsupported case" degrades to a runtime rejection, and the containment law and analyzer both lean on the closed set. The closed sum's cost (a small Core case record + a coordinated consumer recompile, a handful of times ever) buys the design-time safety the rest of this ADR depends on. This is the deliberate trade.
- **`credential_chain` as the default contract.** Rejected — identity fidelity, a second native extension, engine-specificity. Retained as the `DeferToConsumer` opt-in.
- **A user-brokered engine credential factory** (e.g. `UseDuckDb(o => o.AddS3Secret(scope, keyId, secret))`: the gateway returns `Anonymous`, the user configures the engine's secret directly, so Flowthru never *resolves* one). Evaluated by a three-reviewer panel (ergonomics / security / architecture); **rejected as an eager feature, unanimously.** As sketched — raw keys on `DuckDbEngineOptions` — it is a *regression*, not an improvement: the common user configures credentials **zero** times today (the ambient AWS chain resolves them, and the handoff is minted from that same resolution), so the factory drags them to two hand-typed keys; those keys bind from the `Flowthru:DuckDb` config section, inviting a plaintext secret into `appsettings.json` — an on-disk, version-controlled document, which defeats [ADR-0020](./0020-s3-storage-medium-via-gateway-seam.md)'s "a secret never enters a persisted document" invariant and routes *around* the `SecretText` converter (Flowthru stops *resolving* the secret but starts *holding* it, less safely). It revives the two-principal split-brain the `credential_chain` rejection closed — now with a human filling the second brain, a mismatch silent until a runtime permission failure. It scales O(engines × media) — a bespoke credential surface per engine — against the handoff's O(1). And its "looser coupling" is a mirage: DuckDB already speaks S3 natively, so the factory only relocates where credentials *originate*, removing no S3 knowledge from the consumer. The one genuinely-stronger case — a least-privilege credential scoped to the engine alone, or an endpoint/role the gateway's client cannot reproduce — is real but narrow, and its disciplined home is the reserved **`DeferToConsumer`** case, *not* a raw secret on an options bag: a distinct sum member (never an `Anonymous` overload), mutually exclusive with the gateway handoff per `s3://bucket/key` scope (never a merge), `SecretText`-typed, and promoted from reservation via an ADR amendment when a *concrete, recurring* need is demonstrated. Nothing is foreclosed by waiting.
- **`System.Security.SecureString`.** Rejected (DE0001): no encryption off Windows, and DuckDB needs the plaintext regardless.
- **`Microsoft.Extensions.Compliance.Redaction` as the container.** Rejected: it redacts in the *logging pipeline*, not a value carried between extensions, and adds a Core dependency.
- **A trusted third-party secret-string package.** Rejected: no audited canonical one exists; a small-audience dependency in Core's security path is worse than ~60 auditable lines.
- **Keep the dictionary, wrap only the values / a flat universal schema.** Rejected: still stringly-typed and coupled; a universal schema is a lie across S3 / Azure SAS / GCS.
- **Treating the fix as "the scanner will stop flagging."** Rejected as the framing — the first panel's central catch. The honest durable answer is risk-acceptance + suppression + attestation, recorded above.

## Consequences

- **Flow developer** — nothing changes; the `DeferToConsumer`/`credential_chain` opt-in is a future additive knob.
- **Catalog developer** — nothing new.
- **Extension developer** — a medium mints a typed `RemoteAccess` case; a consumer `Match`es exhaustively (typed error for unsupported); a medium that *reveals* a secret into an effect must scrub inside that effect and drop the raw exception — the reveal-site rule, verified by the shared laws, not per-consumer memory. A new medium's containment is enforced by law + the enumeration reflection check.
- **Core developer** — new types beside `ByteLocation` (`SecretText`; the `RemoteAccess` sum with `Secrets`); `RemoteUri.Access` changes shape (breaking, both consumers in-repo); **`RuntimeError.External` and `FlowIO.LiftAsync` are unmodified** — containment is upstream of them; an `.editorconfig` severity bump for the relevant `FT` codes; a new syntactic `Reveal()`-position analyzer (sequenced separately).
- **Security / release** — a `SECURITY.md` attestation (citing only built controls), `socket.yml` and `.snyk` suppressions with justification, an inline annotation at `LocateObject`.
- **Issues** — filed from the S3 security-scan report and two adversarial panels. The `DATA_EXFILTRATION`/high verdict is a false positive; the real observations — raw secret on a general-purpose type, the credential-resolution *throw* path leaking to the persisted JSON run file, and an unfixed source-of-truth `SKILL.md` — are what this ADR closes.

## Anchor code

- `src/core/Flowthru.Core/Data/Storage/ByteLocation.cs` — `RemoteUri.Access` becomes the `RemoteAccess` sum; new `RemoteAccess` + `SecretText` beside it.
- `src/extensions/Flowthru.Extensions.AWS.S3/Data/Storage/S3/S3StorageMedium.cs` + `AmazonS3Gateway.cs` — `LocateObject` mints `RemoteAccess.S3Compatible` with `SecretText`; `LocateBytes` catches resolution failures inside its effect, scrubs against the credentials in scope, and returns a `RuntimeError` retaining no raw `Exception`.
- `src/core/Flowthru.Core/Validation/Runtime/RuntimeError.cs` + `Prelude/FlowIO.cs` — **unchanged**; recorded here to state explicitly that the generic `External` envelope holds no scrub-list and is not the redaction site.
- `src/extensions/Flowthru.Extensions.Metadata.Json/Diagnostics/Json/Internal/FlowMetadataProjection.cs` + `../JsonMetadataProvider.cs` — the extension-owned persisted-JSON sink (`FailureMessage = f.Error.Message`) that inherits a clean error because containment happened upstream; covered by law (iii). Mermaid / Diagnostics providers share the shape and are safe by the same upstream containment.
- `src/extensions/Flowthru.Extensions.DuckDB/Step/DuckDb/Internal/DuckDbS3SecretSql.cs` + `InProcessDuckDbEngine.cs` — consume the typed case, drop the copied key constants; `RedactForRequest` remains the reveal-site scrub for the `CREATE SECRET` path, hardened (min-length, longest-first).
- `src/core/Flowthru.Core.SourceGenerators/` + `.editorconfig` — the syntactic `Reveal()`-position analyzer (separate work item) and the `FT` error-severity bumps.
- `tests/helpers/Flowthru.Tests.Kits/Storage/ISupportsByteLocationLaws.cs` + `tests/extensions/Flowthru.Extensions.AWS.S3.Tests/S3ByteLocationLaws.cs` — serialisation-refusal, ToString-excludes-`Secrets`, JSON-path-redaction, and the `SecretText`-field-∈-`Secrets` reflection law.
- `SECURITY.md` **(new)**, `socket.yml` / `.snyk` **(new)** — attestation (citing only built controls) and suppressions.
- Provenance: an independent design review (Fable) supplied `SecretText`, the typed sum, and the identity-fidelity argument; a round-1 Opus panel (unanimous ADJUST) produced the Core-boundary-redaction requirement, the containment law + analyzer, the honestly-scoped limits, the `Anonymous`/`DeferToConsumer` split, and the attestation posture; a round-2 Opus panel (unanimous ADJUST) relocated redaction from the ungraspable generic `External` boundary to the reveal sites, downgraded the "structurally incapable"/"compile" over-claims, scoped the analyzer to syntactic positions, hardened the scrub, added the enumeration reflection law, and corrected the metadata-sink anchor. A post-implementation three-reviewer Opus panel (ergonomics / security / architecture, unanimous DON'T-ADD) evaluated a user-brokered engine credential factory and declined it in favour of the reserved `DeferToConsumer` case (see Considered options).
