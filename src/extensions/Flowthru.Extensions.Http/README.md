# Flowthru.Extensions.Http

Read Flowthru Catalog Items from `http://` and `https://` URLs. This package registers an
`IStorageMediumProvider` for the HTTP(S) schemes, so **any** format extension (Csv, Parquet,
Json, Xml, …) can read from a remote endpoint with no change — a Flow targets HTTP by writing a
`https://…` URL where a path is accepted, and the typed mapping to your schema happens exactly as
it would for a local file.

[![coverage](https://codecov.io/gh/chaoticgoodcomputing/flowthru/branch/main/graph/badge.svg?component=flowthru_extensions_http)](https://codecov.io/gh/chaoticgoodcomputing/flowthru)

## Mental model

Storage in Flowthru is three independent axes: **format** (how bytes serialize) × **medium**
(where bytes live) × **container** (the in-memory shape). This package adds one medium — an HTTP
endpoint. It does not know about CSV or Parquet, and they do not know about HTTP; they meet
through the medium resolver. Bring your mental model of fetching a file over the web: a GET
returns bytes, an optional `User-Agent`, a timeout, and conditional-GET caching so a large file
isn't re-downloaded on every run. An HTTP-backed Item is a remote read source.

## Install

```bash
dotnet add package Flowthru.Extensions.Http
```

Register the medium, then target HTTP by writing an `https://` URL anywhere a path is accepted:

```csharp
services.AddFlowthru(b =>
{
    b.UseHttp();                     // bound from the Flowthru:Http config section
    b.RegisterCatalog(sp => new Catalog(
        basePath, sp.GetRequiredService<IStorageMediumResolver>()));
});

// In the Catalog — the resolver routes the https:// URI through the HTTP medium:
public IItem<IEnumerable<RetailTransactionSchema>> RetailTransactionsRaw =>
    CreateItem(() => Item.Of<IEnumerable<RetailTransactionSchema>>("RetailTransactionsRaw")
        .Csv()
        .AtPath("https://example.com/data/online-retail-dataset.csv")
        .Build());
```

Bare paths and `file://` still resolve to the local filesystem; only `http://`/`https://` route
here.

## Configuration

Bound from the `Flowthru:Http` section, or overridden code-first via `UseHttp(http => …)`.
On-disk conditional-GET caching avoids re-downloading an unchanged file on every run:

```csharp
b.UseHttp(http =>
{
    http.Timeout = TimeSpan.FromMinutes(15);
    http.UserAgent = "MyOrg-Pipeline/2.0";
    http.Cache = new HttpCacheOptions
    {
        Directory = "/var/cache/flowthru",
        MaxAge = TimeSpan.FromHours(24),
    };
});
```

`MaxConcurrentRequestsPerHost` is the opt-in cap for throttling a rate-limited endpoint; HTTP
reads are parallel-safe and unbounded by default.
