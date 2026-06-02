using Flowthru.Data.Catalog;
using Flowthru.Extensions.EFCore.Tests.Fixtures;
using Flowthru.Flow;
using Flowthru.Hosting;
using Flowthru.Prelude;
using Flowthru.Validation.PreFlight;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Extensions.EFCore.Tests.Hosting;

/// <summary>
/// Tests for VerifyEFCoreConnection / VerifyEFCoreConfiguration /
/// VerifyEFCoreSchema — the EFCore extension's contribution to the
/// Core registration-validation hook surface.
/// </summary>
[TestFixture]
[Category("EFCore")]
[Category("Validation")]
public class EFCoreFlowthruBuilderExtensionsTests
{
  public sealed class EmptyCatalog : CatalogAbstract { }

  private string _dbPath = null!;

  [SetUp]
  public void SetUp()
  {
    _dbPath = Path.Combine(Path.GetTempPath(), $"flowthru-efcore-verify-{Guid.NewGuid():N}.db");
  }

  [TearDown]
  public void TearDown()
  {
    if (File.Exists(_dbPath))
    {
      try { File.Delete(_dbPath); } catch { /* best effort */ }
    }
  }

  // ── VerifyEFCoreConnection ──────────────────────────────────────────

  [Test]
  public async Task VerifyEFCoreConnection_HappyPath_ReturnsValid()
  {
    using var ctx = new TestDbContext(BuildOptions(_dbPath));
    ctx.Database.EnsureCreated();

    var services = BuildHost(addEFCore: true, configure: b =>
      b.VerifyEFCoreConnection<TestDbContext>()
    );
    var service = services.GetRequiredService<IFlowthruService>();

    var result = await service.ValidateRegistrationAsync();
    Assert.That(result, Is.InstanceOf<Validated<PreFlightError, FlowUnit>.Valid>(),
      "A valid SQLite database should pass the connection probe.");
  }

  [Test]
  public async Task VerifyEFCoreConnection_NoFactoryRegistered_FailsWithRegistrationCheckFailed()
  {
    var services = BuildHost(addEFCore: false, configure: b =>
      b.VerifyEFCoreConnection<TestDbContext>()
    );
    var service = services.GetRequiredService<IFlowthruService>();

    var result = await service.ValidateRegistrationAsync();
    var invalid = (Validated<PreFlightError, FlowUnit>.Invalid)result;
    Assert.That(invalid.Errors, Has.Count.EqualTo(1));
    var failure = (PreFlightError.RegistrationCheckFailed)invalid.Errors[0];
    Assert.That(failure.HookId, Does.Contain("EFCore.Connection"));
    Assert.That(failure.CheckMessage, Does.Contain("not registered"),
      "Failure should explicitly mention DI registration so the operator can fix it.");
  }

  [Test]
  public async Task VerifyEFCoreConnection_HookIdAttribution_DefaultsToContextName()
  {
    var services = BuildHost(addEFCore: false, configure: b =>
      b.VerifyEFCoreConnection<TestDbContext>()
    );
    var service = services.GetRequiredService<IFlowthruService>();

    var result = await service.ValidateRegistrationAsync();
    var invalid = (Validated<PreFlightError, FlowUnit>.Invalid)result;
    var failure = (PreFlightError.RegistrationCheckFailed)invalid.Errors[0];
    Assert.That(failure.HookId, Is.EqualTo($"EFCore.Connection[{nameof(TestDbContext)}]"),
      "Default hook id should make the context type traceable in diagnostic surfaces.");
  }

  [Test]
  public async Task VerifyEFCoreConnection_CustomHookId_RoundTrips()
  {
    var services = BuildHost(addEFCore: false, configure: b =>
      b.VerifyEFCoreConnection<TestDbContext>(hookId: "my-custom-id")
    );
    var service = services.GetRequiredService<IFlowthruService>();

    var result = await service.ValidateRegistrationAsync();
    var invalid = (Validated<PreFlightError, FlowUnit>.Invalid)result;
    var failure = (PreFlightError.RegistrationCheckFailed)invalid.Errors[0];
    Assert.That(failure.HookId, Is.EqualTo("my-custom-id"));
  }

  // ── VerifyEFCoreConfiguration ───────────────────────────────────────

  [Test]
  public async Task VerifyEFCoreConfiguration_ValidModel_ReturnsValid()
  {
    using var ctx = new TestDbContext(BuildOptions(_dbPath));
    ctx.Database.EnsureCreated();

    var services = BuildHost(addEFCore: true, configure: b =>
      b.VerifyEFCoreConfiguration<TestDbContext>()
    );
    var service = services.GetRequiredService<IFlowthruService>();

    var result = await service.ValidateRegistrationAsync();
    Assert.That(result, Is.InstanceOf<Validated<PreFlightError, FlowUnit>.Valid>(),
      "A model where every entity has a primitive primary key should pass.");
  }

  [Test]
  public async Task VerifyEFCoreConfiguration_NoFactoryRegistered_FailsCleanly()
  {
    var services = BuildHost(addEFCore: false, configure: b =>
      b.VerifyEFCoreConfiguration<TestDbContext>()
    );
    var service = services.GetRequiredService<IFlowthruService>();

    var result = await service.ValidateRegistrationAsync();
    var invalid = (Validated<PreFlightError, FlowUnit>.Invalid)result;
    var failure = (PreFlightError.RegistrationCheckFailed)invalid.Errors[0];
    Assert.That(failure.HookId, Does.Contain("EFCore.Configuration"));
  }

  // ── VerifyEFCoreSchema ──────────────────────────────────────────────

  [Test]
  public async Task VerifyEFCoreSchema_HappyPath_ReturnsValid()
  {
    using var ctx = new TestDbContext(BuildOptions(_dbPath));
    ctx.Database.EnsureCreated();

    var services = BuildHost(addEFCore: true, configure: b =>
      b.VerifyEFCoreSchema<TestDbContext>()
    );
    var service = services.GetRequiredService<IFlowthruService>();

    var result = await service.ValidateRegistrationAsync();
    Assert.That(result, Is.InstanceOf<Validated<PreFlightError, FlowUnit>.Valid>(),
      "A freshly EnsureCreated database has columns matching the model.");
  }

  [Test]
  public async Task VerifyEFCoreSchema_NoFactoryRegistered_FailsCleanly()
  {
    var services = BuildHost(addEFCore: false, configure: b =>
      b.VerifyEFCoreSchema<TestDbContext>()
    );
    var service = services.GetRequiredService<IFlowthruService>();

    var result = await service.ValidateRegistrationAsync();
    var invalid = (Validated<PreFlightError, FlowUnit>.Invalid)result;
    var failure = (PreFlightError.RegistrationCheckFailed)invalid.Errors[0];
    Assert.That(failure.HookId, Does.Contain("EFCore.Schema"));
  }

  // ── Composition ─────────────────────────────────────────────────────

  [Test]
  public async Task AllThreeHooks_Together_RunIndependentlyAndAggregate()
  {
    // No DbContextFactory registered — all three hooks should fail and
    // their failures should aggregate into a single Invalid result.
    var services = BuildHost(addEFCore: false, configure: b =>
    {
      b.VerifyEFCoreConnection<TestDbContext>();
      b.VerifyEFCoreConfiguration<TestDbContext>();
      b.VerifyEFCoreSchema<TestDbContext>();
    });
    var service = services.GetRequiredService<IFlowthruService>();

    var result = await service.ValidateRegistrationAsync();
    var invalid = (Validated<PreFlightError, FlowUnit>.Invalid)result;
    Assert.That(invalid.Errors, Has.Count.EqualTo(3),
      "Each hook must independently report its failure — operator should see all three at once.");

    var hookIds = invalid.Errors
      .OfType<PreFlightError.RegistrationCheckFailed>()
      .Select(e => e.HookId)
      .ToArray();
    Assert.That(hookIds, Does.Contain($"EFCore.Connection[{nameof(TestDbContext)}]"));
    Assert.That(hookIds, Does.Contain($"EFCore.Configuration[{nameof(TestDbContext)}]"));
    Assert.That(hookIds, Does.Contain($"EFCore.Schema[{nameof(TestDbContext)}]"));
  }

  // ── Hermetic depth: live probe skipped, in-memory check kept ─────────

  [Test]
  public async Task Hermetic_SkipsLiveConnectionProbe_ButRunsInMemoryConfigurationCheck()
  {
    // The factory points at an unreachable database (its parent directory
    // does not exist). At Hermetic the live connection probe — classified
    // Shallow — is skipped, while the in-memory model/configuration check —
    // classified Hermetic — still runs. This is the offline smoke-test
    // contract: validate structure + wiring with no reachable database. At
    // Shallow the probe runs and fails, proving the skip is depth-specific.
    var unreachable = Path.Combine(
      Path.GetTempPath(), $"flowthru-missing-{Guid.NewGuid():N}", "x.db");
    var services = new ServiceCollection();
    services.AddDbContextFactory<TestDbContext>(opts =>
      opts.UseSqlite($"Data Source={unreachable}"));
    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => new EmptyCatalog());
      b.RegisterFlow("noop", () => FlowBuilder.CreateFlow("noop", _ => { }));
      b.VerifyEFCoreConnection<TestDbContext>();
      b.VerifyEFCoreConfiguration<TestDbContext>();
    });
    var service = services.BuildServiceProvider().GetRequiredService<IFlowthruService>();

    var hermetic = await service.RunAsync(
      flowLabel: null,
      new ExecutionOptions { ValidationDepth = ValidationDepth.Hermetic, DryRun = DryRunOption.On });
    Assert.That(hermetic.HasFailures, Is.False,
      "Hermetic skips the live EFCore connection probe; the in-memory configuration "
      + "check passes even with no reachable database.");

    var shallow = await service.RunAsync(
      flowLabel: null,
      new ExecutionOptions { ValidationDepth = ValidationDepth.Shallow, DryRun = DryRunOption.On });
    Assert.That(shallow.HasFailures, Is.True,
      "Shallow runs the connection probe, which fails against an unreachable database.");
  }

  // ── Helpers ─────────────────────────────────────────────────────────

  private static DbContextOptions<TestDbContext> BuildOptions(string dbPath) =>
    new DbContextOptionsBuilder<TestDbContext>()
      .UseSqlite($"Data Source={dbPath}")
      .Options;

  private IServiceProvider BuildHost(bool addEFCore, Action<IFlowthruBuilder> configure)
  {
    var services = new ServiceCollection();
    if (addEFCore)
    {
      services.AddDbContextFactory<TestDbContext>(opts => opts.UseSqlite($"Data Source={_dbPath}"));
    }
    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => new EmptyCatalog());
      b.RegisterFlow("noop", () => FlowBuilder.CreateFlow("noop", _ => { }));
      configure(b);
    });
    return services.BuildServiceProvider();
  }
}
