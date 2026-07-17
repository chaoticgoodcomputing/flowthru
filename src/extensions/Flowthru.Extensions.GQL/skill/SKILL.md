---
name: flowthru-gql
description: Deep skill for the Flowthru GraphQL medium — declaring Catalog Items backed by a StrawberryShake client so a Flow loads typed rows from a GraphQL API the same way it loads a file. Use when a project reads (or writes) catalog data over a GraphQL endpoint. Pairs with the umbrella `flowthru` skill.
metadata:
  flowthru:
    extension: Flowthru.Extensions.GQL
    surface: medium
    capability: Read (and optionally write) Catalog Items against a GraphQL API via a StrawberryShake client — an Item is a named handle on a query, load runs the operation and a projection pulls rows out.
    register: "— (declare a .Gql() item; UseGql() only caps concurrency)"
---

# flowthru-gql

Adds a **GraphQL medium** to the Catalog: a Catalog Item whose bytes come from a GraphQL operation instead of a file. This is the *medium* axis of a catalog item (format × medium × container — see the `flowthru` umbrella's `catalog-developers.md`); it decides *where* rows come from, not their in-memory shape. Steps that consume a GQL-backed item are ordinary Flowthru steps — GQL is not a new step type.

**Reach for GQL** when an external GraphQL API is your source (or sink) of record. An Item becomes a named handle on one query operation: the load fires the operation, and a `selectData` projection pulls your rows out of the result envelope.

## Mental model

Bring **everything StrawberryShake gives you** — generated operation types, one `IOperationResult<TResult>` per query, your `.graphql` documents. The extension brings **no client of its own**: wire your StrawberryShake client in DI as usual, then pass its operation delegates to the Item. You never write GraphQL-over-HTTP by hand; you hand the Item a `queryFunc` and a `selectData`.

## Use it

Reference the package — declaring an item needs **no `UseXxx()` call**. Once referenced, `.Gql(...)` and `.GqlDeferred(...)` are available on the item builder:

```bash
dotnet add package Flowthru.Extensions.GQL
```

Wire the client in host DI (`services.AddYourClient().ConfigureHttpClient(...)`), then declare the item. `.GqlDeferred(...)` yields a `GqlQuery<TResult,T>` handle — no network I/O at catalog construction or pre-flight; the consuming step decides *when* to fire by calling `.ToList()`:

<!-- flowthru:snippet:docs:gql-usage:start -->
```csharp
public IItem<GqlQuery<IGetCompaniesResult, IGetCompanies_Companies>> Companies =>
  CreateItem(() => Item.Of<GqlQuery<IGetCompaniesResult, IGetCompanies_Companies>>("GQLCompanies")
    .GqlDeferred(
      queryFunc: ct => _client.GetCompanies.ExecuteAsync(ct),
      selectData: r => r.Companies)
    .AllowEmpty()
    .Build());
```
_(source: [`SpaceflightsGQL/Catalog.Raw.cs`](https://github.com/chaoticgoodcomputing/flowthru/blob/main/examples/advanced/SpaceflightsGQL/Data/_01_Raw/Catalog.Raw.cs))_
<!-- flowthru:snippet:docs:gql-usage:end -->

## Choosing an item shape

- **Eager collection** — `.Gql(...)`: the load materializes the whole result set. Add pagination (Relay cursor or offset) at declaration and the adapter walks every page into one flat `IEnumerable<T>`.
- **Deferred handle** — `.GqlDeferred(...)`: the item carries a `GqlQuery<TResult,T>`; the *step* fires the call with `.ToList()` / `.ToListAsync()`. Use this when a step should decide when the network hit happens, or to join several queries at one materialization point (the step-level analog of a Spark `TypedFrame<T>.ToList()`).
- **Read/write single item** — supply a `mutationFunc` to a single-item `Gql` query and the item becomes writable; the Flow persists back through the mutation.
- **Typed filter** — the `GqlDeferredQuery<TFilter, TResult, T>` overloads take a filter input the step applies via `WithFilter(...)` before materializing.

To *write* many rows (seeding), you don't need a special item — a plain step can call the client's add-mutations directly (see `SeedGqlDatabaseStep`).

## Gotchas

- **You own the client.** Nothing works until your StrawberryShake client is registered in DI and its `BaseAddress` is configured. The extension only consumes the operation delegates you pass.
- **Deferred means deferred.** A `.GqlDeferred` item does zero network I/O until a step calls `.ToList()`. Don't expect rows in the catalog; expect a handle.
- **Empty results fail pre-flight by default.** If the query can legitimately return no rows (e.g. an unseeded server), add `.AllowEmpty()` — otherwise a null/empty envelope trips inspection.
- **`UseGql()` is only for throttling.** Item declaration never needs it. Call `b.UseGql()` once *and* declare `.WithGqlConcurrency(...)` on the item to cap concurrent calls to a rate-limited endpoint under `Parallelism > 1`; without `UseGql()`, `WithGqlConcurrency` resolves to unbounded (a no-op).
- **Pre-flight probe.** Add a `FlowServiceInspector<TClient>` to health-check the endpoint before a Flow runs; the medium keeps catalog construction I/O-free so this is where connectivity is verified.
