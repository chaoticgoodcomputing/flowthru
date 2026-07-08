using Flowthru.Hosting;
using Flowthru.Prelude;
using Flowthru.Step.DuckDb;
using Flowthru.Step.DuckDb.Internal;
using Flowthru.Validation.PreFlight;
using Flowthru.Validation.PreFlight.DuckDb;
using Flowthru.Validation.Runtime;
using Flowthru.Validation.Runtime.DuckDb;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Flowthru.Extensions.DuckDB.Tests;

/// <summary>
/// Pins the DI wiring contract of
/// <see cref="DuckDbFlowthruBuilderExtensions.UseDuckDb(IFlowthruBuilder)"/>
/// and its <c>Action&lt;DuckDbEngineOptions&gt;</c> overload: the exact
/// registrations contributed (engine singleton, profile contributor),
/// the <c>Flowthru:DuckDb</c> option binding, the post-configure
/// override semantics, and the try-add courtesy toward test doubles.
/// </summary>
[TestFixture]
[Category("DuckDB")]
public class DuckDbFlowthruBuilderExtensionsTests
{
  private static (IFlowthruBuilder Builder, IServiceCollection Services) MakeBuilder(
    IConfiguration? configuration = null
  )
  {
    var services = new ServiceCollection();
    services.AddSingleton<IConfiguration>(configuration ?? new ConfigurationBuilder().Build());
    var builder = new FlowthruServiceBuilder(services);
    return (builder, services);
  }

  private static IConfiguration ConfigFrom(IDictionary<string, string?> values) =>
    new ConfigurationBuilder().AddInMemoryCollection(values).Build();

  // ── Argument validation ─────────────────────────────────────────────────

  [Test]
  public void UseDuckDb_NullBuilder_Throws()
  {
    IFlowthruBuilder? builder = null;
    Assert.That(() => builder!.UseDuckDb(), Throws.TypeOf<ArgumentNullException>());
  }

  [Test]
  public void UseDuckDb_NullConfigureDelegate_Throws()
  {
    var (builder, _) = MakeBuilder();
    Assert.That(
      () => builder.UseDuckDb(configure: null!),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  // ── Registrations ───────────────────────────────────────────────────────

  [Test]
  public void UseDuckDb_RegistersInProcessEngine_AsSingleton()
  {
    var (builder, services) = MakeBuilder();
    builder.UseDuckDb();

    using var provider = services.BuildServiceProvider();
    var engine = provider.GetRequiredService<IDuckDbEngine>();

    Assert.That(engine, Is.InstanceOf<InProcessDuckDbEngine>());
    Assert.That(provider.GetRequiredService<IDuckDbEngine>(), Is.SameAs(engine),
      "The engine is the flow-wide conflict resource — it must be a singleton.");
  }

  [Test]
  public void UseDuckDb_RegistersTheEngineProfileContributor()
  {
    var (builder, services) = MakeBuilder();
    builder.UseDuckDb();

    using var provider = services.BuildServiceProvider();
    var contributors = provider.GetServices<IServiceProfileContributor>().ToList();

    Assert.That(contributors, Has.Some.InstanceOf<DuckDbEngineProfileContributor>(),
      "Without the contributor the scheduler would treat the engine as unbounded.");
  }

  [Test]
  public void UseDuckDb_RegistersTheHermeticSqlSchemaHook_Once()
  {
    var (builder, services) = MakeBuilder();
    builder.UseDuckDb();
    builder.UseDuckDb(); // idempotent — no duplicate findings

    using var provider = services.BuildServiceProvider();
    var hooks = provider.GetServices<IFlowValidationHook>()
      .OfType<DuckDbTransformValidationHook>()
      .ToList();

    Assert.Multiple(() =>
    {
      Assert.That(hooks, Has.Count.EqualTo(1),
        "TryAddEnumerable semantics: repeated UseDuckDb() calls must not stack "
        + "duplicate hooks, or every finding would report twice.");
      Assert.That(hooks.Single().MinimumDepth,
        Is.EqualTo(Flowthru.Flow.ValidationDepth.Hermetic),
        "The SQL schema check reaches nothing outside the process, so it "
        + "participates in offline smoke tests.");
    });
  }

  [Test]
  public void UseDuckDb_RespectsAnEarlierEngineRegistration()
  {
    var (builder, services) = MakeBuilder();
    var stub = new StubEngine();
    services.AddSingleton<IDuckDbEngine>(stub);

    builder.UseDuckDb();
    using var provider = services.BuildServiceProvider();

    Assert.That(provider.GetRequiredService<IDuckDbEngine>(), Is.SameAs(stub),
      "TryAddSingleton semantics: test doubles registered earlier take precedence.");
  }

  // ── Option binding ──────────────────────────────────────────────────────

  [Test]
  public void UseDuckDb_BindsOptions_FromTheFlowthruDuckDbSection()
  {
    var (builder, services) = MakeBuilder(ConfigFrom(new Dictionary<string, string?>
    {
      ["Flowthru:DuckDb:MaxConcurrentTransforms"] = "3",
      ["Flowthru:DuckDb:MemoryLimit"] = "2GB",
      ["Flowthru:DuckDb:Threads"] = "4",
    }));
    builder.UseDuckDb();

    using var provider = services.BuildServiceProvider();
    var options = provider.GetRequiredService<IOptions<DuckDbEngineOptions>>().Value;

    Assert.Multiple(() =>
    {
      Assert.That(options.MaxConcurrentTransforms, Is.EqualTo(3));
      Assert.That(options.MemoryLimit, Is.EqualTo("2GB"));
      Assert.That(options.Threads, Is.EqualTo(4));
    });
  }

  [Test]
  public void UseDuckDb_ConfigureOverload_RunsAfterSectionBinding()
  {
    var (builder, services) = MakeBuilder(ConfigFrom(new Dictionary<string, string?>
    {
      ["Flowthru:DuckDb:MaxConcurrentTransforms"] = "3",
      ["Flowthru:DuckDb:MemoryLimit"] = "2GB",
    }));
    builder.UseDuckDb(opts => opts.MaxConcurrentTransforms = 5);

    using var provider = services.BuildServiceProvider();
    var options = provider.GetRequiredService<IOptions<DuckDbEngineOptions>>().Value;

    Assert.Multiple(() =>
    {
      Assert.That(options.MaxConcurrentTransforms, Is.EqualTo(5),
        "Code-first overrides run after the configuration binding.");
      Assert.That(options.MemoryLimit, Is.EqualTo("2GB"),
        "Values the override doesn't touch keep their bound values.");
    });
  }

  [Test]
  public void UseDuckDb_EngineHonoursBoundConcurrency()
  {
    var (builder, services) = MakeBuilder(ConfigFrom(new Dictionary<string, string?>
    {
      ["Flowthru:DuckDb:MaxConcurrentTransforms"] = "2",
    }));
    builder.UseDuckDb();

    using var provider = services.BuildServiceProvider();
    Assert.That(provider.GetRequiredService<IDuckDbEngine>().MaxConcurrency, Is.EqualTo(2));
  }

  [Test]
  public void InProcessEngine_RejectsNonPositiveConcurrency()
  {
    Assert.That(
      () => new InProcessDuckDbEngine(new DuckDbEngineOptions { MaxConcurrentTransforms = 0 }),
      Throws.TypeOf<ArgumentOutOfRangeException>()
    );
  }

  private sealed class StubEngine : IDuckDbEngine
  {
    public int MaxConcurrency => 1;
    public FlowIO<DuckDbTransformResult> ExecuteTransform(DuckDbTransformRequest request) =>
      FlowIO.Pure(new DuckDbTransformResult(0, Array.Empty<(string, string)>()));
  }
}
