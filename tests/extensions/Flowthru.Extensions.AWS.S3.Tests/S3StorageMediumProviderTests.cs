using Flowthru.Data.Storage;
using Flowthru.Data.Storage.S3;
using Flowthru.Data.Storage.S3.Local;
using Flowthru.Prelude;

namespace Flowthru.Extensions.AWS.S3.Tests;

/// <summary>
/// <see cref="S3StorageMediumProvider"/> scheme-claiming and URI→(bucket, key)
/// parsing. Parsing is asserted behaviorally: a medium built for a URI is written
/// through the local stub, and the resulting file's location proves how the bucket
/// and key were parsed.
/// </summary>
[TestFixture]
[Category("AwsS3")]
public class S3StorageMediumProviderTests
{
  private string _root = null!;

  [SetUp]
  public void SetUp()
  {
    _root = Path.Combine(Path.GetTempPath(), $"flowthru-s3-provider-{Guid.NewGuid():N}");
    Directory.CreateDirectory(_root);
  }

  [TearDown]
  public void TearDown()
  {
    if (Directory.Exists(_root))
    {
      try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
    }
  }

  private S3StorageMediumProvider Provider() =>
    new(new LocalFileS3Gateway(_root));

  // ── CanHandle ───────────────────────────────────────────────────────────────

  [Test]
  public void CanHandle_S3Scheme_True()
  {
    Assert.That(Provider().CanHandle(new Uri("s3://bucket/key")), Is.True);
  }

  [TestCase("https://example.com/x")]
  [TestCase("http://example.com/x")]
  [TestCase("file:///tmp/x")]
  public void CanHandle_OtherSchemes_False(string uri)
  {
    Assert.That(Provider().CanHandle(new Uri(uri)), Is.False);
  }

  // ── Parsing (behavioral) ──────────────────────────────────────────────────────

  [Test]
  public async Task Create_NestedKey_MapsBucketAndKeyPath()
  {
    var medium = Provider().Create(new Uri("s3://my-bucket/nested/path/file.bin"));
    await WriteThrough(medium, [7, 7, 7]);

    Assert.That(File.Exists(Path.Combine(_root, "my-bucket", "nested", "path", "file.bin")), Is.True,
      "A nested key should map to a nested path under {root}/{bucket}/.");
  }

  [Test]
  public async Task Create_DottedBucketName_PreservedAsBucket()
  {
    var medium = Provider().Create(new Uri("s3://my.bucket.name/key.bin"));
    await WriteThrough(medium, [1]);

    Assert.That(File.Exists(Path.Combine(_root, "my.bucket.name", "key.bin")), Is.True,
      "A dotted bucket name should be used verbatim as the bucket.");
  }

  [Test]
  public async Task Create_PercentEncodedKey_Decoded()
  {
    var medium = Provider().Create(new Uri("s3://bucket/my%20key.bin"));
    await WriteThrough(medium, [2]);

    Assert.That(File.Exists(Path.Combine(_root, "bucket", "my key.bin")), Is.True,
      "A percent-encoded space in the key should be decoded to a literal space.");
  }

  // ── Invalid addresses ──────────────────────────────────────────────────────────

  [TestCase("s3://bucket")]
  [TestCase("s3://bucket/")]
  public void Create_NoKey_Throws(string uri)
  {
    Assert.That(() => Provider().Create(new Uri(uri)),
      Throws.InstanceOf<InvalidOperationException>(),
      "An s3:// URI with no object key is an invalid address and should fail early.");
  }

  private static async Task WriteThrough(IStorageMedium medium, byte[] bytes)
  {
    using var input = new MemoryStream(bytes);
    var result = await medium.WriteStream(input).Run();
    Assert.That(result, Is.InstanceOf<EffResult<FlowUnit>.Success>(),
      $"WriteStream should succeed. Got: {result}");
  }
}
