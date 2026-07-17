---
name: flowthru-s3
description: Deep skill for the Flowthru AWS S3 medium extension — reading and writing Catalog Items against `s3://bucket/key` URIs in a Flowthru (.NET) pipeline. Use when an Item's source or destination is an S3 (or S3-compatible — MinIO, R2, LocalStack) object. Pairs with the umbrella `flowthru` skill.
metadata:
  flowthru:
    extension: Flowthru.Extensions.AWS.S3
    surface: medium
    capability: Read/write any-format Item against s3://bucket/key — a remote object medium with atomic writes and a pre-flight write-probe; format is untouched.
    register: b.UseS3(…) / b.UseLocalS3(…)
---

# flowthru-s3

Adds the **S3 medium** to Flowthru's storage resolver. Medium is one axis of a Catalog Item (format × medium × container — see the `flowthru` umbrella skill's `catalog-developers.md`): it decides *where bytes live*, not how they serialize. This package teaches the resolver one new place — an S3 object store — so **any** format extension (Csv, Parquet, Json, Xml, …) reads and writes over S3 with no change to the Item's format declaration. Adding S3 is purely additive.

**Mental model:** an S3-backed Item is a remote object you read from and write to. The format code never learns it's talking to S3, and S3 never learns the bytes are Parquet; they meet through the medium resolver.

**Target it by writing an `s3://` URI where a path is accepted.** Nothing else changes: `.Parquet().AtPath("s3://my-bucket/raw/orders.parquet")` round-trips to your `[FlowthruSchema]` exactly as a local file would. Bare paths and `file://` still resolve to the local filesystem; only `s3://` routes here.

## Register

```bash
dotnet add package Flowthru.Extensions.AWS.S3
```

Enable the medium inside `AddFlowthru`. The Catalog must be built with the `IStorageMediumResolver` so an Item's `s3://` URI can be routed:

```csharp
services.AddFlowthru(b =>
{
    b.UseS3();                       // credentials via the standard AWS chain
    b.RegisterCatalog(sp => new Catalog(
        basePath, sp.GetRequiredService<IStorageMediumResolver>()));
});

// Anywhere a path is accepted:
ItemFactory.Enumerable.Csv<Order>("orders", "s3://my-bucket/raw/orders.csv", resolver);
```
_(source: [Flowthru.Extensions.AWS.S3 README](https://github.com/chaoticgoodcomputing/flowthru/blob/main/src/extensions/Flowthru.Extensions.AWS.S3/README.md))_

Configure a region or an S3-compatible endpoint code-first via `UseS3(s3 => …)`, or the `Flowthru:S3` config section (`Region`, `ServiceUrl`, `ForcePathStyle`, `Timeout`):

```csharp
b.UseS3(s3 =>
{
    s3.Region = "us-west-2";
    s3.ServiceUrl = "http://localhost:9000";  // MinIO / LocalStack
    s3.ForcePathStyle = true;
});
```
_(source: [Flowthru.Extensions.AWS.S3 README](https://github.com/chaoticgoodcomputing/flowthru/blob/main/src/extensions/Flowthru.Extensions.AWS.S3/README.md))_

## Local development — no AWS account

`UseLocalS3` swaps in a shipped file-backed stub: a fully offline stand-in for S3, no account, credentials, or network. Same provider, same medium, same Flow — only the gateway changes, so it's also the swap point that lets tests run offline. Each object lands at `{root}/{bucket}/{key}`, an inspectable directory tree.

```csharp
b.UseLocalS3("/tmp/s3-stub");   // s3://demo/out.json → /tmp/s3-stub/demo/out.json
```

## Notes

- **Credentials — the extension owns none.** `UseS3()` builds its client with no explicit credentials, so the AWS SDK resolves them through its standard chain — env vars (`AWS_ACCESS_KEY_ID`/`AWS_SECRET_ACCESS_KEY`/`AWS_SESSION_TOKEN`), the shared profile (`~/.aws/credentials`), or an ECS/EC2 instance role. Flowthru never loads, stores, or sees a secret.
- **Pre-flight write-probe.** `InspectTarget()` PUTs and deletes a zero-byte sentinel beside the target key, so a missing bucket or denied `s3:PutObject` fails at **pre-flight** with a `WriteAccessDenied` diagnostic, not mid-run. (It briefly creates an object, which can fire bucket notifications — same trade-off the filesystem sentinel probe makes.)
- **Atomic writes.** `WriteStream` is a single object PUT, all-or-nothing at the object level; a failed write never leaves a partial object.
- **Caching.** S3-backed Items are fingerprintable via the object ETag (one HEAD request, no body transfer), so they participate in Flowthru's cache plan.
- **S3-compatible stores.** `ServiceUrl` + `ForcePathStyle` targets MinIO, LocalStack, Cloudflare R2, or any S3-API endpoint. AWS SDK v4 sends request checksums by default; some older S3-compatible servers reject them — upgrade the server or pin a compatible SDK if you hit checksum errors (AWS itself is unaffected).
- **Format is orthogonal.** `.Csv()`, `.Parquet()`, `.Json()`, `.Xml()` all work over S3 with the identical declaration — swap the format axis freely without touching the medium.
