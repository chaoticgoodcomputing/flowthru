---
name: flowthru-http
description: Deep skill for the Flowthru HTTP medium extension — reading Catalog Items from `http://`/`https://` URLs in a Flowthru (.NET) pipeline. Use when an Item's source is a remote web URL, or when a raw dataset lives at a URL instead of on disk. Pairs with the umbrella `flowthru` skill.
metadata:
  flowthru:
    extension: Flowthru.Extensions.Http
    surface: medium
    capability: Read any-format Item from an http(s):// URL — a remote read medium with conditional-GET caching; format is untouched.
    register: b.UseHttp(…)
---

# flowthru-http

Adds the **HTTP(S) medium** to Flowthru's storage resolver. Medium is one axis of a Catalog Item (format × medium × container — see the `flowthru` umbrella skill's `catalog-developers.md`): it decides *where bytes live*, not how they serialize. This package teaches the resolver one new place — a web endpoint — so **any** format extension (Csv, Parquet, Json, Xml, …) can read from a remote URL with no change to the Item's format declaration.

**Mental model:** an HTTP-backed Item is a remote *read* source. Bring the mental model of fetching a file over the web — a GET returns bytes, with an optional `User-Agent`, a timeout, and conditional-GET caching so a large file isn't re-downloaded every run. The format code never learns it's talking to HTTP, and this medium never learns the bytes are CSV; they meet through the medium resolver.

**Target it by writing a URL where a path is accepted.** Nothing else changes: `.Csv().AtPath("https://…/data.csv")` reads and maps to your `[FlowthruSchema]` exactly as a local file would. Bare paths and `file://` still resolve to the local filesystem; only `http://`/`https://` route here.

## Register

```bash
dotnet add package Flowthru.Extensions.Http
```

Enable the medium inside `AddFlowthru`. It binds from the `Flowthru:Http` config section; override code-first via the `UseHttp(http => …)` overload (below, tuning the on-disk conditional-GET cache):

<!-- flowthru:snippet:docs:register-http:start -->
```csharp
flowthru.UseHttp(http =>
{
  http.Cache = new Flowthru.Data.Storage.Http.HttpCacheOptions
  {
    Directory = Path.Combine(basePath, ".http-cache"),
    MaxAge = TimeSpan.FromHours(24),
  };
});
```
<!-- flowthru:snippet:docs:register-http:end -->
_(real source: [RetailDataSplitFlow `Program.cs`](https://github.com/chaoticgoodcomputing/flowthru/blob/main/examples/advanced/RetailDataSplitFlow/Program.cs))_

For the resolver to route an Item's `https://` URI, the Catalog must be constructed with the `IStorageMediumResolver` (`b.RegisterCatalog(sp => new Catalog(basePath, sp.GetRequiredService<IStorageMediumResolver>()))`), and the Item must carry that resolver.

## Use it

Declare the Item's format as usual and point `AtPath` at the URL — the resolver routes it through the HTTP medium at runtime:

```csharp
public IItem<IEnumerable<RetailTransactionSchema>> RetailTransactionsRaw =>
  CreateItem(() => Item.Of<IEnumerable<RetailTransactionSchema>>("RetailTransactionsRaw")
    .Csv()
    .AtPath("https://example.com/data/online-retail-dataset.csv")
    .Build());
```
_(source: [Flowthru.Extensions.Http README](https://github.com/chaoticgoodcomputing/flowthru/blob/main/src/extensions/Flowthru.Extensions.Http/README.md))_

## Notes

- **Read-only medium.** HTTP is a fetch source; it has no write path. Use it for the raw/ingest edge — a URL an external producer publishes — and write outputs to a local or S3 medium (see `flowthru-s3`).
- **Conditional-GET caching.** Set `http.Cache` (`Directory` + `MaxAge`) so an unchanged file isn't re-downloaded every run — essential for a large dataset. Without a cache, every run re-fetches.
- **Config knobs:** `Timeout` and `UserAgent` on the `UseHttp(http => …)` overload, or the `Flowthru:Http` config section. `MaxConcurrentRequestsPerHost` is the opt-in throttle for a rate-limited endpoint; HTTP reads are parallel-safe and unbounded by default.
- **Format is orthogonal.** `.Csv()`, `.Parquet()`, `.Json()`, `.Xml()` all work over HTTP with the identical declaration — swap the format axis freely without touching the medium.
