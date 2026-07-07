# Flowthru.Prelude

Flowthru's FP foundations. Globally imported by every consumer via the
`build/Flowthru.New.Core.props` file (mirrors Haskell's implicit `Prelude`).

## Provenance

The types in this directory are inspired by and partially derived from
[LanguageExt v5](https://github.com/louthy/language-ext) by Paul Louth.
The upstream MIT license is preserved verbatim in
[LICENSE-LanguageExt.md](LICENSE-LanguageExt.md). Source files derived from
LanguageExt include an attribution header pointing to that notice.

## Scope

This is **not** a port or fork of LanguageExt. It is a focused subset
containing only the FP primitives Flowthru actively uses.

**Included:**

- `Eff<TRuntime, T>` — capability-environment-typed effect, fails with `RuntimeError`
- `EffResult<T>` — closed sum returned by `Eff.Run`
- `Has<TRuntime, TCapability>` — capability constraint trait (Eff-specialised)
- `Validated<E, T>` — error-accumulating applicative for pre-flight checks
- `Unit` — bound type for effects with no meaningful result
- `FlowSource<T>` — the streaming sibling of the effect type: a lazy,
  resource-safe stream consumed by compiling back into `FlowIO`. Its minimum
  shape earns a Prelude seat because streaming is a first-class grain (bounded
  `O(batch)` reads on memory-constrained hosts); see
  [ADR-0023](/.claude/docs/adr/0023-streaming-reads-as-catalog-item-type.md)
  for why it is vendored, not taken as a LanguageExt dependency

**Excluded** (and not planned):

- The `K<F, A>` higher-kinded encoding
- Generic `Functor<F>` / `Applicative<F>` / `Monad<F>` typeclasses
- Monad transformer stack (`StateT`, `WriterT`, `ReaderT`, `RWST`, …)
- `Either`, `Option`, `Try`, `Fin` — Flowthru's `RuntimeError` ADT and
  `Validated<E, T>` cover these roles
- Free monads and Pipes (but **not** Streams — see `FlowSource<T>` above,
  added under ADR-0023)
- Immutable collections (`Seq`, `Lst`, `Iterable`, `HashMap`, …)

## Maintenance policy

These types are **owned by Flowthru** going forward. Do not chase upstream
LanguageExt changes. If a behaviour or naming choice diverges from upstream,
that is intentional and a Flowthru concern.

When touching files here: prefer Flowthru-internal idiom over LanguageExt
fidelity. The point is not to be a faithful subset; it is to give Flowthru
the FP primitives it needs without adopting a 50K-line dependency whose
maintenance trajectory is uncertain.

## Where the rest of the algebra lives

Some Flowthru-original types compose with the ones here but are not
LanguageExt-derived and live elsewhere:

- `Flowthru.Error.RuntimeError` — closed sum of runtime failure modes
  (the `Eff` failure type)
- `Flowthru.Validation.PreFlightError` — closed sum of pre-flight failure
  modes (the `Validated` error accumulator's `E` type)

The split is deliberate: this directory contains *language-level* primitives;
the Flowthru-specific ADTs that fill in the type parameters live with the
phase they describe.
