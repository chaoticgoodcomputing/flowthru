# Flowthru.Tests.Kits Fixtures

Canonical JSON fixture data for conformance kits. Every format kit deserializes from these files via `FixtureLoader`, then exercises round-trip behavior against the format under test.

## Layout

```
Fixtures/
├─ Flat/        — schemas with scalar columns only; baseline shape every flat format round-trips
├─ Nested/      — schemas with sub-objects or lists (JSON, Parquet, XML, GQL)
└─ Mixed/       — schemas combining flat scalars with nested fields (JSON, Parquet, XML, GQL)
```

Flat fixtures include rows with null values. Every conforming flat format must preserve the null/non-null distinction; CSV does this via the configurable `nullValues` parameter on `CsvFormatSerializer<TRow>` (default `[""]` matches CSV convention).

Each shape directory contains scenarios under `<Scenario>/<variant>.json`, where `<Scenario>` corresponds to a schema in `Flowthru.Tests.Kits/Schemas/` and `<variant>.json` carries the fixture data.

## Field naming

JSON keys MUST match the `[SerializedLabel]` values on the schema, not the C# property names. This is what `JsonFormatSerializer<TRow>` produces and what every conforming format must round-trip.

## Adding fixtures

1. Add the schema to `Flowthru.Tests.Kits/Schemas/`.
2. Add a `<Scenario>/rows.json` (or per-case file) under the appropriate shape directory.
3. Update the conformance subclasses that should run the new scenario by adding the relative path to their `FlatFixtures` / `NestedFixtures` / `MixedFixtures` override.
