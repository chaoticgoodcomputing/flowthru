# Flowthru.Extensions.GQL

Read and write Flowthru Catalog Items against a GraphQL API through a StrawberryShake client.
Declare an Item from a query operation and a projection (`result -> rows`), and a Flow loads
typed rows from the API the same way it loads them from a file. Single-item, collection,
paginated (Relay cursor or offset), and deferred query handles are all supported, and a single
item can be made read/write by supplying a mutation delegate.

[![coverage](https://codecov.io/gh/chaoticgoodcomputing/flowthru/branch/main/graph/badge.svg?component=flowthru_extensions_gql)](https://codecov.io/gh/chaoticgoodcomputing/flowthru)

## Mental model

This bridges a [StrawberryShake](https://chillicream.com/docs/strawberryshake) client into the
Catalog. Bring everything StrawberryShake gives you — generated operation types, an
`IOperationResult<TResult>` per query, your `.graphql` documents. The extension brings no client
of its own: you wire your StrawberryShake client in DI as usual and pass its operation delegates
to the Item. An Item is a named handle on a query — the load executes the operation and the
`selectData` projection pulls your rows out of the result envelope. Use `GqlDeferred` when a step
should decide when to fire the network call (it materialises via `.ToList()`).

## Install

```bash
dotnet add package Flowthru.Extensions.GQL
```

Wire your StrawberryShake client in DI, then declare a GQL-backed Item from its operations:

```csharp
// Host wiring — your generated StrawberryShake client, configured as usual.
services
    .AddSpaceflightsClient()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.example.com/graphql"));

// In the Catalog — a deferred query handle; the consuming step calls .ToList()
// to fire the GetCompanies operation against the server.
public IItem<GqlQuery<IGetCompaniesResult, IGetCompanies_Companies>> Companies =>
    CreateItem(() => Item.Of<GqlQuery<IGetCompaniesResult, IGetCompanies_Companies>>("GQLCompanies")
        .GqlDeferred(
            queryFunc: ct => _client.GetCompanies.ExecuteAsync(ct),
            selectData: r => r.Companies)
        .AllowEmpty()
        .Build());
```

`UseGql()` is the opt-in scheduler gate for rate-limited endpoints — pair it with
`WithGqlConcurrency(...)` to cap concurrent calls to an endpoint when running at
`Parallelism > 1`.
