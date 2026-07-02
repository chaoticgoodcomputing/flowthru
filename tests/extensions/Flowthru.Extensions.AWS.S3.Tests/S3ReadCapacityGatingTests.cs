using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.S3;
using Flowthru.Data.Storage.S3.Local;
using Flowthru.Flow;
using Flowthru.Hosting;
using Flowthru.Prelude;
using Flowthru.Step;
using Flowthru.Validation.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Extensions.AWS.S3.Tests;

/// <summary>
/// Prototype validation for the issue #111 fix (ADR-0019): declaring
/// <c>S3Options.MaxConcurrentReads</c> bounds how many <c>s3://</c> Parquet reads
/// the scheduler dispatches at once. All S3 reads share one memory-domain
/// conflict key (<c>Read:s3:read</c>), so distinct objects gate together — the
/// property a per-object key would miss. Runs offline over
/// <see cref="LocalFileS3Gateway"/>; no container needed (this tests the
/// scheduler gating, not the memory ceiling itself).
/// </summary>
[TestFixture]
[Category("AwsS3")]
public class S3ReadCapacityGatingTests
{
  // Four independent read steps, each loading a distinct s3:// Parquet object,
  // run at Parallelism=4. Returns the observed peak concurrency.
  private static async Task<int> ObservePeakConcurrency(int? maxConcurrentReads)
  {
    var runId = Guid.NewGuid().ToString("N");
    var root = Path.Combine(Path.GetTempPath(), $"s3cap-{runId}");

    var services = new ServiceCollection();
    services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
    if (maxConcurrentReads is { } cap)
    {
      services.Configure<S3Options>(o => o.MaxConcurrentReads = cap);
    }
    services.AddFlowthru(b => b.UseS3(new LocalFileS3Gateway(root)));
    using var sp = services.BuildServiceProvider();
    var resolver = sp.GetRequiredService<IStorageMediumResolver>();
    var profiles = sp.GetRequiredService<IServiceProfileProvider>();

    // Seed four distinct s3:// Parquet objects.
    var items = new List<IItem<IEnumerable<PqRow>>>();
    for (var i = 0; i < 4; i++)
    {
      var item = ItemFactory.Enumerable.Parquet<PqRow>(
        label: $"s3cap-{runId}-obj{i}",
        filePath: $"s3://cap-bucket/{runId}/obj{i}.parquet",
        resolver: resolver);
      await item.Save(new[]
      {
        new PqRow { Id = i, Name = $"n{i}", Category = "c", V1 = i, V2 = i, V3 = i, Flags = 0, Payload = "p" },
      }).Run();
      items.Add(item);
    }

    var running = 0;
    var peak = 0;
    var gate = new object();
    Func<IEnumerable<PqRow>, FlowIO<int>> track = rows => FlowIO.LiftAsync(
      async ct =>
      {
        var now = Interlocked.Increment(ref running);
        lock (gate) peak = Math.Max(peak, now);
        await Task.Delay(80, ct).ConfigureAwait(false); // window so overlap is observable
        Interlocked.Decrement(ref running);
        return rows.Count();
      },
      source: "s3cap:track");

    IStepNode ReadStep(int i)
    {
      var input = items[i];
      var output = ItemFactory.Singleton.Memory<int>($"s3cap-{runId}-out{i}");
      return new Step<IEnumerable<PqRow>, int>(
        label: $"read-{i}",
        transform: track,
        inputs: new IItem[] { input },
        outputs: new IItem[] { output },
        loadInputs: () => input.Load(),
        saveOutputs: v => output.Save(v));
    }

    var flow = FlowBuilder.CreateFlow($"s3cap-{runId}", b =>
    {
      for (var i = 0; i < 4; i++) b.Add(ReadStep(i));
    });

    var result = await new ParallelFlowScheduler(profiles: profiles)
      .ExecuteAsync(flow, new ExecutionOptions { Parallelism = 4 });

    Assert.That(result.IsSuccess, Is.True, "all read steps should succeed");
    try { Directory.Delete(root, recursive: true); } catch { /* best effort */ }
    return peak;
  }

  [Test]
  public async Task MaxConcurrentReads_BoundsConcurrentS3ReadSteps()
  {
    var peak = await ObservePeakConcurrency(maxConcurrentReads: 2);
    Assert.That(peak, Is.EqualTo(2),
      "With MaxConcurrentReads=2, four steps reading distinct s3:// objects must never "
      + "exceed 2 in flight — they share the memory-domain read key (issue #111 fix).");
  }

  [Test]
  public async Task Unbounded_ReadsRunFullyConcurrently()
  {
    var peak = await ObservePeakConcurrency(maxConcurrentReads: null);
    Assert.That(peak, Is.EqualTo(4),
      "Unbounded (the default) leaves reads ungated — four run at once at Parallelism=4. "
      + "Proves the harness observes overlap, so the bounded case above is meaningful, and "
      + "that default behaviour is unchanged.");
  }

  [Test]
  public async Task DistinctS3Items_ShareOneReadConflictKey()
  {
    var root = Path.Combine(Path.GetTempPath(), $"s3cap-key-{Guid.NewGuid():N}");
    var services = new ServiceCollection();
    services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
    services.Configure<S3Options>(o => o.MaxConcurrentReads = 3);
    services.AddFlowthru(b => b.UseS3(new LocalFileS3Gateway(root)));
    using var sp = services.BuildServiceProvider();
    var resolver = sp.GetRequiredService<IStorageMediumResolver>();
    var profiles = sp.GetRequiredService<IServiceProfileProvider>();

    var itemA = ItemFactory.Enumerable.Parquet<PqRow>("A", "s3://b/a.parquet", resolver);
    var itemB = ItemFactory.Enumerable.Parquet<PqRow>("B", "s3://b/other/b.parquet", resolver);

    var depA = itemA.ServiceDependencies.Single();
    var depB = itemB.ServiceDependencies.Single();

    Assert.That(depA.DagId, Is.EqualTo("s3:read"), "the read dependency uses the shared memory-domain identity");
    Assert.That(ConflictKeys.KeyFor(depA, ConflictOp.Read),
      Is.EqualTo(ConflictKeys.KeyFor(depB, ConflictOp.Read)),
      "distinct s3:// objects must resolve to the SAME read conflict key so they gate together");
    Assert.That(profiles.Resolve(depA).CapacityFor(ConflictOp.Read), Is.EqualTo(3),
      "the DI-wired profile provider resolves the declared read capacity");
  }
}
