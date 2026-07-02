using System.Text;
using Flowthru.Extensions.AWS.S3.Tests.Backends;
using Flowthru.Extensions.AWS.S3.Tests.Support;

namespace Flowthru.Extensions.AWS.S3.Tests.Contract;

/// <summary>
/// The <see cref="Flowthru.Data.Storage.S3.IS3Gateway"/> contract as
/// backend-agnostic laws, run identically over every <see cref="IS3GatewayBackend"/>
/// via <c>[TestFixture(typeof(...))]</c> — the S3 analogue of the Sheets gateway
/// laws. The offline tier (<see cref="LocalFileS3Backend"/>) always runs; the live
/// tier (<see cref="LiveS3Backend"/>) gates on <see cref="Flowthru.Tests.Kits.Prelude.TestCapabilities.AwsS3"/>
/// and reports Inconclusive when no test bucket is configured, so the default flow
/// stays green on CI.
/// </summary>
/// <remarks>
/// These laws are what make the shipped <see cref="Flowthru.Data.Storage.S3.Local.LocalFileS3Gateway"/>
/// a <em>verified</em> S3 stand-in: the same behaviour suite passes against the
/// stub and (when configured) against real S3.
/// </remarks>
[TestFixture(typeof(LocalFileS3Backend))]
[TestFixture(typeof(LiveS3Backend))]
[TestFixture(typeof(MinioContainerBackend))]
[Category("AwsS3")]
[Category("Laws")]
public sealed class S3GatewayLaws<TBackend>
  where TBackend : IS3GatewayBackend, new()
{
  private TBackend _backend = default!;

  [OneTimeSetUp]
  public async Task GateAndInitialiseBackend()
  {
    _backend = new TBackend();
    foreach (var capability in _backend.RequiredCapabilities)
    {
      Assume.That(capability.IsAvailable(), $"[{capability.Name}] {capability.MissingMessage}");
    }
    await _backend.InitializeAsync();
  }

  [OneTimeTearDown]
  public async Task ReleaseBackendResources()
  {
    if (_backend is not null)
    {
      await _backend.Cleanup();
    }
  }

  private S3GatewayContext Fresh() => _backend.CreateResource();

  // ── Round-trip ──────────────────────────────────────────────────────────────

  [Test]
  public async Task PutThenGet_RoundTripsBytes()
  {
    var ctx = Fresh();
    var key = ctx.Key("round-trip.bin");
    var payload = Bytes("Flowthru over S3");

    await ctx.Gateway.PutObject(ctx.Bucket, key, new MemoryStream(payload), default);
    var read = await ReadAll(ctx, key);

    Assert.That(read, Is.EqualTo(payload), "Bytes read should equal bytes written.");
  }

  // ── Existence ───────────────────────────────────────────────────────────────

  [Test]
  public async Task ObjectExists_FalseBeforePut_TrueAfter()
  {
    var ctx = Fresh();
    var key = ctx.Key("exists.bin");

    Assert.That(await ctx.Gateway.ObjectExists(ctx.Bucket, key, default), Is.False,
      "A key with no object should not exist.");

    await ctx.Gateway.PutObject(ctx.Bucket, key, new MemoryStream(Bytes("x")), default);

    Assert.That(await ctx.Gateway.ObjectExists(ctx.Bucket, key, default), Is.True,
      "After a PUT the object should exist.");
  }

  // ── Overwrite ───────────────────────────────────────────────────────────────

  [Test]
  public async Task Put_OverwritesExistingObject()
  {
    var ctx = Fresh();
    var key = ctx.Key("overwrite.bin");

    await ctx.Gateway.PutObject(ctx.Bucket, key, new MemoryStream(Bytes("first")), default);
    await ctx.Gateway.PutObject(ctx.Bucket, key, new MemoryStream(Bytes("second")), default);

    Assert.That(await ReadAll(ctx, key), Is.EqualTo(Bytes("second")),
      "A second PUT to the same key should replace the object.");
  }

  // ── Delete ──────────────────────────────────────────────────────────────────

  [Test]
  public async Task DeleteObject_RemovesObject_AndIsIdempotent()
  {
    var ctx = Fresh();
    var key = ctx.Key("delete.bin");
    await ctx.Gateway.PutObject(ctx.Bucket, key, new MemoryStream(Bytes("bye")), default);

    await ctx.Gateway.DeleteObject(ctx.Bucket, key, default);
    Assert.That(await ctx.Gateway.ObjectExists(ctx.Bucket, key, default), Is.False,
      "After delete the object should be gone.");

    Assert.That(async () => await ctx.Gateway.DeleteObject(ctx.Bucket, key, default),
      Throws.Nothing, "Deleting an absent object should be a no-op, not a throw.");
  }

  // ── Not-found read ──────────────────────────────────────────────────────────

  [Test]
  public void GetObject_AbsentKey_Throws()
  {
    var ctx = Fresh();
    Assert.That(
      async () => await ctx.Gateway.GetObject(ctx.Bucket, ctx.Key("nope.bin"), default),
      Throws.Exception,
      "Reading an absent object should throw (the medium lifts this into a FlowIO failure).");
  }

  // ── ETag fingerprint source ───────────────────────────────────────────────────

  [Test]
  public async Task GetETag_NullForAbsent_StableForPresent_SensitiveToChange()
  {
    var ctx = Fresh();
    var key = ctx.Key("etag.bin");

    Assert.That(await ctx.Gateway.GetETag(ctx.Bucket, key, default), Is.Null,
      "An absent object has no ETag.");

    await ctx.Gateway.PutObject(ctx.Bucket, key, new MemoryStream(Bytes("alpha")), default);
    var first = await ctx.Gateway.GetETag(ctx.Bucket, key, default);
    var second = await ctx.Gateway.GetETag(ctx.Bucket, key, default);

    Assert.That(first, Is.Not.Null.And.Not.Empty, "A present object should have an ETag.");
    Assert.That(second, Is.EqualTo(first), "Repeat ETag reads without a change should be stable.");

    await ctx.Gateway.PutObject(ctx.Bucket, key, new MemoryStream(Bytes("beta-different")), default);
    var afterChange = await ctx.Gateway.GetETag(ctx.Bucket, key, default);

    Assert.That(afterChange, Is.Not.EqualTo(first),
      "Changing the object content should change the ETag.");
  }

  // ── Helpers ─────────────────────────────────────────────────────────────────

  private static byte[] Bytes(string s) => Encoding.UTF8.GetBytes(s);

  private static async Task<byte[]> ReadAll(S3GatewayContext ctx, string key)
  {
    await using var stream = await ctx.Gateway.GetObject(ctx.Bucket, key, default);
    using var ms = new MemoryStream();
    await stream.CopyToAsync(ms);
    return ms.ToArray();
  }
}
