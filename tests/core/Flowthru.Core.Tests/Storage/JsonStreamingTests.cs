using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Prelude;

namespace Flowthru.Core.Tests.Storage;

/// <summary>
/// End-to-end streaming for the core JSON format (#120): a JSON item's
/// <c>.AsStream()</c> yields a working <c>FlowSource</c> that reads the array
/// incrementally. The trait/marker honesty (CanStream ⇔ IFormatStreamReader)
/// is enforced separately by <see cref="JsonFormatSerializerLaws_TestRow"/>.
/// </summary>
[TestFixture]
public class JsonStreamingTests
{
  private string _tempDir = null!;

  [SetUp]
  public void SetUp()
  {
    _tempDir = Path.Combine(Path.GetTempPath(), $"flowthru-json-stream-{Guid.NewGuid():N}");
    Directory.CreateDirectory(_tempDir);
  }

  [TearDown]
  public void TearDown()
  {
    if (Directory.Exists(_tempDir))
    {
      try { Directory.Delete(_tempDir, recursive: true); } catch { /* best-effort */ }
    }
  }

  [Test]
  public void Json_Serializer_HonestlyDeclaresStreaming()
  {
    var serializer = new JsonFormatSerializer<TestRow>();
    Assert.That(serializer, Is.InstanceOf<IFormatStreamReader<TestRow>>());
    Assert.That(serializer.Traits.CanStream, Is.True);
  }

  [Test]
  public async Task Json_AsStream_ReadsTheArrayIncrementally()
  {
    var path = Path.Combine(_tempDir, "rows.json");
    var item = ItemFactory.Enumerable.Json<TestRow>("rows", path);

    var input = new[]
    {
      new TestRow { Id = 1, Name = "alpha" },
      new TestRow { Id = 2, Name = "beta" },
      new TestRow { Id = 3, Name = "gamma" },
    };
    var saved = await item.Save(input).Run();
    Assert.That(saved, Is.InstanceOf<EffResult<FlowUnit>.Success>());

    // Stream it back through the read-only .AsStream() view.
    var loadResult = await item.AsStream().Load().Run();
    var source = ((EffResult<FlowSource<TestRow>>.Success)loadResult).Value;
    var listResult = await source.Compile().ToList().Run();
    var loaded = ((EffResult<IReadOnlyList<TestRow>>.Success)listResult).Value;

    Assert.That(loaded.Select(r => r.Id), Is.EqualTo(new[] { 1, 2, 3 }));
    Assert.That(loaded.Select(r => r.Name), Is.EqualTo(new[] { "alpha", "beta", "gamma" }));
  }
}
