# Flowthru.Misc.DataFrames

A framework-agnostic, strongly-typed DataFrame abstraction. `TypedFrame<T>` is a phantom-typed
`IQueryable<T>`: `Where`, `Select`, `Join`, and grouping build an expression tree against your
schema type instead of running anything, and a pluggable `IFrameQueryProvider` translates that
tree into native operations on whatever backend you wire up (Spark columns, ML.NET transforms,
and so on) without materializing rows into .NET objects. The type parameter carries schema
information through every operation, so a misspelled column or a type-mismatched join is a
compile error. It carries no Flowthru.Core dependency and is usable standalone.

[![coverage](https://codecov.io/gh/chaoticgoodcomputing/flowthru/branch/main/graph/badge.svg?component=flowthru_misc_dataframes)](https://codecov.io/gh/chaoticgoodcomputing/flowthru)

## Install

```bash
dotnet add package Flowthru.Misc.DataFrames
```

Root a `TypedFrame<T>` on a provider, then compose typed operators — the chain is captured as an
expression tree and only translated when the provider materializes it:

```csharp
// `provider` is your IFrameQueryProvider implementation over the native backend.
var people = new TypedFrame<Person>(provider);

var adults = people
    .Where(p => p.Age >= 18)
    .Select(p => new PersonSummary { Name = p.Name, Age = p.Age });

// Materialization is the provider's job — enumerating triggers translation + execution.
foreach (var summary in adults) { /* … */ }
```

The schema type (`Person` here) is annotated with `[FlowthruSchema]` to participate in the
compile-time and pre-flight validation a translator plugin can layer on top.
