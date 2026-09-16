# Contributing to Flowthru.Extensions.Google.Sheets

This document is for developers working on **`Flowthru.Extensions.Google.Sheets`** — the extension that makes a Google Sheet a typed tabular Catalog store, modelled on the EFCore extension rather than on a file format.

**Audience scope:** assumes familiarity with [examples/CONTRIBUTING.md](/examples/CONTRIBUTING.md) (Flow / Catalog Developer vocabulary) and [src/extensions/CONTRIBUTING.md](/src/extensions/CONTRIBUTING.md) (the Extension surface and the Extension Developer role). Terms defined here are the *additional* vocabulary specific to this extension.

See [/CONTRIBUTING.md](/CONTRIBUTING.md) for the cross-cutting design rules every context shares.

## Why this package is its own context

A directory is a context iff it directly contains a `CONTRIBUTING.md`, minted lazily when a package has vocabulary or decisions that belong nowhere else. Sheets qualifies because it sits in a category of its own: it is neither a **format** (bytes serialized a particular way) nor a plain **medium** (bytes at a location), but a *store* with a schema, a type system, and an API that answers queries. Modelling it as a file format would have been the obvious move and the wrong one — that decision, and what follows from it, is recorded in this package's own [`docs/adr/`](/src/extensions/Flowthru.Extensions.Google.Sheets/docs/adr).

## Honoring Fail-Fast against a remote store

A Google Sheet is user-editable, remote, and weakly typed — three properties that push errors toward runtime. The obligation runs the other way:

- **Design-time** — the Schema declares the column set and their types, so a transform reading a column the Schema does not declare is a compile error.
- **Pre-flight** — the sheet's actual header row and inferred column types are inspected and reconciled against the declared Schema *before* any Step runs. A renamed column or a type drift is a pre-flight failure naming the column, not a cast exception mid-Flow.
- **Runtime** — API failures, quota limits, and permission changes, which genuinely cannot be known earlier.

The pre-flight rung is the whole point of modelling Sheets as a store: a format serializer has no way to inspect a remote schema, and a medium has no notion of columns.

## Glossary

**Sheets gateway**: The seam (`ISheetsGateway`) between the extension and the Google Sheets API — the single place credentials are used and API calls are made. Everything above it works against resolved tables and typed values, so the extension is testable against a local fake with no Google account. The same gateway-seam pattern the S3 medium uses, for the same reason: the credential never spreads past the seam.
_Avoid_: "client" (too generic — `SheetsService` is Google's client; the gateway is the seam over it), "adapter" (that is the storage-adapter layer above).

**Resolved table**: A sheet range interpreted as a table — a header row mapped to typed columns, plus the rows beneath it. Produced by the gateway and reconciled against the declared Schema at pre-flight. It is the unit that makes a sheet addressable as data rather than as a grid of cells.
_Avoid_: "range" (a range is the A1 address; the resolved table is what the range *means*), "worksheet" (a worksheet may hold several tables).

**Column type**: The extension's narrow type vocabulary for a sheet column, sitting between the Sheets API's untyped cell values and a Schema's CLR types. It exists because a sheet reports values, not a schema, so the column's type must be *inferred* from what is present and then checked against what was declared.
_Avoid_: "cell format" (that is presentation — a currency format is not a type), "data type" (ambiguous between the CLR type and the inferred one).

**Native Tables**: Google Sheets' own structured-table feature, distinct from a plain range with a header row. Worth naming separately because a sheet may use it or not, and the gateway must behave identically either way — a Flow Developer should never have to know which the sheet uses.
_Avoid_: "named range" (a different Sheets feature with different semantics).
