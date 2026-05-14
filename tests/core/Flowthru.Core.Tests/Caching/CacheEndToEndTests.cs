using Flowthru.Caching;
using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Diagnostics;
using Flowthru.Flow;
using Flowthru.Hosting;
using Flowthru.Prelude;
using Flowthru.Step;
using Flowthru.Validation.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Core.Tests.Caching;

/// <summary>
/// Integration verification of Phase 6: wiring across pre-flight,
/// scheduler, and post-run upsert. Builds a real
/// <see cref="IFlowthruService"/> with a file-backed catalog and a
/// hand-rolled cacheable step, then asserts:
/// </summary>
/// <list type="bullet">
/// <item>First run is a cold miss — the step runs and the manifest
/// gets a fresh entry.</item>
/// <item>Second run is a cache hit — the step is short-circuited and
/// reports <see cref="StepResult.Succeeded.Reason"/> of
/// <c>"cached"</c> with <see cref="TimeSpan.Zero"/>.</item>
/// </list>
[TestFixture]
public class CacheEndToEndTests
{
  private string _tempDir = null!;
  private string _inputPath = null!;
  private string _outputPath = null!;
  private string _cachePath = null!;
  private bool _transformInvoked;

  [SetUp]
  public void SetUp()
  {
    _tempDir = Path.Combine(Path.GetTempPath(), $"flowthru-cache-e2e-{Guid.NewGuid():N}");
    Directory.CreateDirectory(_tempDir);
    _inputPath = Path.Combine(_tempDir, "input.bin");
    _outputPath = Path.Combine(_tempDir, "output.bin");
    _cachePath = Path.Combine(_tempDir, "cache.json");
    _transformInvoked = false;

    File.WriteAllBytes(_inputPath, new byte[] { 1, 2, 3, 4 });
  }

  [TearDown]
  public void TearDown()
  {
    if (Directory.Exists(_tempDir))
    {
      try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
    }
  }

  [Test]
  public async Task SecondRunWithUnchangedInputs_IsCacheHit()
  {
    var firstRun = await RunFlowAsync();
    Assert.That(firstRun.IsSuccess, Is.True, "First run should succeed.");
    Assert.That(_transformInvoked, Is.True, "First run is a cold cache miss; the transform should execute.");

    var firstStep = (StepResult.Succeeded)firstRun.StepResults.Single();
    Assert.That(firstStep.Reason, Is.Null,
      "A real execution should not carry the cached reason.");
    Assert.That(File.Exists(_cachePath), Is.True,
      "Successful run should write the cache manifest to disk.");

    // Run #2 — inputs unchanged, output still present, manifest hit expected.
    _transformInvoked = false;
    var secondRun = await RunFlowAsync();
    Assert.That(secondRun.IsSuccess, Is.True);
    Assert.That(_transformInvoked, Is.False,
      "Second run should short-circuit: the transform must NOT be invoked.");

    var secondStep = (StepResult.Succeeded)secondRun.StepResults.Single();
    Assert.That(secondStep.Reason, Is.EqualTo("cached"),
      "Cache hit must surface as Reason=\"cached\".");
    Assert.That(secondStep.Duration, Is.EqualTo(TimeSpan.Zero),
      "Cached steps emit zero duration — no wall-clock work happened.");
  }

  [Test]
  public async Task BypassCacheReads_ForcesReRunButStillUpdatesManifest()
  {
    // Run #1 — populate the manifest.
    await RunFlowAsync();
    Assert.That(_transformInvoked, Is.True, "First run is a cold miss.");
    Assert.That(File.Exists(_cachePath), Is.True);

    var firstManifestText = File.ReadAllText(_cachePath);

    // Run #2 with --no-cache equivalent — must re-execute even though
    // a cache hit would otherwise have been served.
    _transformInvoked = false;
    var rerun = await RunFlowAsync(new ExecutionOptions { BypassCacheReads = true });
    Assert.That(rerun.IsSuccess, Is.True);
    Assert.That(_transformInvoked, Is.True,
      "BypassCacheReads must force the transform to execute even when the manifest "
      + "would otherwise have served a hit.");

    var step = (StepResult.Succeeded)rerun.StepResults.Single();
    Assert.That(step.Reason, Is.Null,
      "Re-executed step should not carry the cached reason.");

    // The manifest must still be present (writes happen) and its
    // contents either unchanged or refreshed in place.
    Assert.That(File.Exists(_cachePath), Is.True,
      "BypassCacheReads suppresses reads but not writes — the manifest must persist.");
    var secondManifestText = File.ReadAllText(_cachePath);
    Assert.That(secondManifestText, Is.Not.Empty,
      "Manifest should still contain entries after a --no-cache run; only the timestamp "
      + "may have moved.");
  }

  [Test]
  public async Task ModifiedInput_BustsCache()
  {
    await RunFlowAsync();
    Assert.That(_transformInvoked, Is.True, "First run is a cold miss.");

    // Mutate the input file's mtime + size (the File fingerprint inputs).
    Thread.Sleep(50); // mtime resolution is platform-dependent; ensure a change
    File.WriteAllBytes(_inputPath, new byte[] { 9, 8, 7, 6, 5 });

    _transformInvoked = false;
    var rerun = await RunFlowAsync();
    Assert.That(rerun.IsSuccess, Is.True);
    Assert.That(_transformInvoked, Is.True,
      "Input change must propagate to a cache miss — the transform runs again.");

    var step = (StepResult.Succeeded)rerun.StepResults.Single();
    Assert.That(step.Reason, Is.Null,
      "Real execution after a miss should not carry the cached reason.");
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Helpers
  // ─────────────────────────────────────────────────────────────────────────

  private async Task<FlowResult> RunFlowAsync(ExecutionOptions? options = null)
  {
    var services = new ServiceCollection();
    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => new TestCatalog(_inputPath, _outputPath));
      b.RegisterFlow<TestCatalog>("cache-e2e", catalog =>
        FlowBuilder.CreateFlow("cache-e2e", fb =>
          fb.Add(BuildCacheableStep(catalog))));
      b.UseCacheStorage(_ =>
        Item.Of<CacheManifest>("flowthru.cache")
          .Json()
          .AtPath(_cachePath)
          .Build());
    });

    using var sp = services.BuildServiceProvider();
    var service = sp.GetRequiredService<IFlowthruService>();
    return options is null
      ? await service.RunAsync()
      : await service.RunAsync(flowLabel: null, options: options);
  }

  private Step<byte[], byte[]> BuildCacheableStep(TestCatalog catalog) =>
    new Step<byte[], byte[]>(
      label: "transform",
      transform: bytes =>
      {
        _transformInvoked = true;
        return FlowIO.Pure(bytes);
      },
      inputs: new IItem[] { catalog.Input },
      outputs: new IItem[] { catalog.Output },
      loadInputs: () => catalog.Input.Load(),
      saveOutputs: result => catalog.Output.Save(result),
      codeVersion: "transform-v1"
    );

  public sealed class TestCatalog : CatalogAbstract
  {
    private readonly string _inputPath;
    private readonly string _outputPath;
    public TestCatalog(string inputPath, string outputPath)
    {
      _inputPath = inputPath;
      _outputPath = outputPath;
    }
    public IItem<byte[]> Input => CreateItem(() =>
      new Item<byte[]>("e2e-input", new BinaryFileStorageAdapter(_inputPath)));
    public IItem<byte[]> Output => CreateItem(() =>
      new Item<byte[]>("e2e-output", new BinaryFileStorageAdapter(_outputPath)));
  }
}
