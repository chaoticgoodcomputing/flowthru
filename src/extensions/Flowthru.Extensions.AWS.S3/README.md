# Flowthru.Extensions.AWS.S3

Read and write Flowthru Catalog Items against `s3://` URIs. This package registers
an `IStorageMediumProvider` for the `s3://` scheme, so **any** format extension
(Csv, Parquet, Json, Xml, …) works over S3 with no change — a Flow targets S3 by
writing an `s3://bucket/key` path on an Item, nothing more.

[![coverage](https://codecov.io/gh/chaoticgoodcomputing/flowthru/branch/main/graph/badge.svg?component=flowthru_extensions_aws_s3)](https://codecov.io/gh/chaoticgoodcomputing/flowthru)

## Mental model

Storage in Flowthru is three independent axes: **format** (how bytes serialize) ×
**medium** (where bytes live) × **container** (the in-memory shape). This package
adds one medium. It does not know about CSV or Parquet, and CSV/Parquet do not know
about S3 — they meet through the medium resolver. Adding S3 is purely additive.

## Install

```bash
dotnet add package Flowthru.Extensions.AWS.S3
```

Register the medium, then target S3 by writing an `s3://` path anywhere a path is accepted:

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

Bare paths and `file://` still resolve to the local filesystem; only `s3://` routes
here.

## Credentials

The extension **holds no credentials of its own**. The AWS-backed gateway builds its
client with no explicit credentials, so the AWS SDK resolves them through its standard
chain — environment variables (`AWS_ACCESS_KEY_ID` / `AWS_SECRET_ACCESS_KEY` /
`AWS_SESSION_TOKEN`), the shared profile (`~/.aws/credentials`), or an ECS/EC2 instance
role.

On the ordinary read/write path the SDK client uses those credentials internally and
Flowthru never touches them. The one exception is the limited case where a consumer
reaches the object **natively** — with its own S3 client, to read the bytes without
round-tripping them through Flowthru — and so must be handed credentials to authenticate
itself. There the gateway resolves them from that same chain, per call, into Core's
access handoff. On that path Flowthru *does* resolve and pass the secret, but the handoff
is minted on demand, never persisted to the catalog or DAG, never written to disk, and
scrubbed from any error text. Flowthru never stores a secret.

## Configuration (`Flowthru:S3`)

| Key | Meaning | Default |
|-----|---------|---------|
| `Region` | AWS region system name (e.g. `us-east-1`) | SDK default chain |
| `ServiceUrl` | Override endpoint for an S3-compatible store | AWS S3 |
| `ForcePathStyle` | Path-style addressing (`endpoint/bucket/key`) | `false` |
| `Timeout` | Per-request timeout | 5 minutes |

```csharp
b.UseS3(s3 =>
{
    s3.Region = "us-west-2";
    s3.ServiceUrl = "http://localhost:9000";  // MinIO / LocalStack
    s3.ForcePathStyle = true;
});
```

### S3-compatible stores

Setting `ServiceUrl` + `ForcePathStyle` targets MinIO, LocalStack, Cloudflare R2, or
any S3-API endpoint. Note: AWS SDK v4 sends request checksums by default; some older
S3-compatible servers reject them — upgrade the server or pin a compatible SDK if you
hit checksum errors. (AWS itself is unaffected.)

## Local development without an AWS account

`UseLocalS3` swaps in a shipped file-backed stub — a fully offline stand-in for S3,
no account, credentials, or network. Each object lands at `{root}/{bucket}/{key}`, so
the directory tree is an inspectable record of the "bucket".

```csharp
b.UseLocalS3("/tmp/s3-stub");   // s3://demo/out.json → /tmp/s3-stub/demo/out.json
```

Same provider, same medium, same Flow — only the gateway changes. This is the swap
point that also lets tests run offline. For shared or production storage, use `UseS3()`.

## Behavior notes

- **Atomic writes** — `WriteStream` is a single object PUT, all-or-nothing at the
  object level; a failed write never leaves a partial object.
- **Pre-flight write-probe** — `InspectTarget()` PUTs and deletes a zero-byte sentinel
  beside the target key, so a missing bucket or denied `s3:PutObject` fails at
  pre-flight with a `WriteAccessDenied` diagnostic, not at runtime. (It briefly creates
  an object, which can fire bucket notifications — the same trade-off the filesystem
  medium's sentinel probe makes.)
- **Caching** — S3-backed Items are fingerprintable via the object ETag (one HEAD
  request, no body transfer), so they participate in Flowthru's cache plan.
