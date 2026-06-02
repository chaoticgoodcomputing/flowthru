using Flowthru.Hosting;
using Flowthru.Prelude;
using Flowthru.Step.Python;
using Flowthru.Step.Python.Internal;
using Flowthru.Validation.PreFlight;
using Flowthru.Validation.PreFlight.Python;
using Flowthru.Validation.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Flowthru.Extensions.Python.Tests;

/// <summary>
/// Pins the DI wiring contract of <see cref="PythonFlowthruBuilderExtensions.UsePython(IFlowthruBuilder)"/>
/// and its <c>Action&lt;PythonRuntimeOptions&gt;</c> overload. The extension is a thin façade — its only
/// responsibility is the exact set and lifetime of DI registrations it contributes, plus the option-binding
/// + post-configure semantics — so the tests check each registration outcome rather than any runtime behaviour.
/// </summary>
[TestFixture]
[Category("Python")]
public class PythonFlowthruBuilderExtensionsTests
{
  // ── Helpers ────────────────────────────────────────────────────────────

  /// <summary>
  /// Construct a real <see cref="IFlowthruBuilder"/> via
  /// <see cref="FlowthruServiceBuilder"/> directly — the SUT only consumes
  /// <c>builder.Services</c>, so going through <c>AddFlowthru</c> would
  /// drag in unrelated catalog/flow constraints. The provided
  /// <paramref name="configuration"/> is registered as a singleton so the
  /// options pipeline's <c>Configure&lt;IConfiguration&gt;</c> step can
  /// resolve it.
  /// </summary>
  private static (IFlowthruBuilder Builder, IServiceCollection Services) MakeBuilder(
    IConfiguration? configuration = null
  )
  {
    var services = new ServiceCollection();
    services.AddSingleton<IConfiguration>(configuration ?? new ConfigurationBuilder().Build());
    // Open-generic NullLogger<> from Logging.Abstractions satisfies
    // ILogger<T> resolution (e.g. ILogger<SubprocessPythonExecutor>)
    // without dragging in the full Microsoft.Extensions.Logging package.
    services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
    var builder = new FlowthruServiceBuilder(services);
    return (builder, services);
  }

  private static IConfiguration ConfigFrom(IDictionary<string, string?> values) =>
    new ConfigurationBuilder().AddInMemoryCollection(values).Build();

  // ── Argument validation ────────────────────────────────────────────────

  [Test]
  public void UsePython_NullBuilder_Throws()
  {
    IFlowthruBuilder? builder = null;
    Assert.That(
      () => builder!.UsePython(),
      Throws.TypeOf<ArgumentNullException>(),
      "The extension must guard against null builders so misuse fails at the call site."
    );
  }

  [Test]
  public void UsePython_ConfigureOverload_NullBuilder_Throws()
  {
    IFlowthruBuilder? builder = null;
    Assert.That(
      () => builder!.UsePython(_ => { }),
      Throws.TypeOf<ArgumentNullException>(),
      "The configure overload must also guard against null builders."
    );
  }

  [Test]
  public void UsePython_NullConfigureDelegate_Throws()
  {
    var (builder, _) = MakeBuilder();
    Assert.That(
      () => builder.UsePython((Action<PythonRuntimeOptions>)null!),
      Throws.TypeOf<ArgumentNullException>(),
      "A null configure delegate is a programmer error — fail fast at registration."
    );
  }

  // ── DI registrations after UsePython() ─────────────────────────────────

  [Test]
  public void UsePython_RegistersPythonConfigurationFlattener_AsConcreteSingleton()
  {
    var (builder, _) = MakeBuilder();
    builder.UsePython();
    using var sp = builder.Services.BuildServiceProvider();

    var first = sp.GetRequiredService<IPythonConfigurationFlattener>();
    var second = sp.GetRequiredService<IPythonConfigurationFlattener>();

    Assert.That(first, Is.InstanceOf<PythonConfigurationFlattener>(),
      "Default implementation must be the concrete PythonConfigurationFlattener.");
    Assert.That(second, Is.SameAs(first),
      "Registration must be a DI singleton — the same instance every resolution.");
  }

  [Test]
  public void UsePython_RegistersPythonServiceInspectorRegistry_AsConcreteSingleton()
  {
    var (builder, _) = MakeBuilder();
    builder.UsePython();
    using var sp = builder.Services.BuildServiceProvider();

    var first = sp.GetRequiredService<IPythonServiceInspectorRegistry>();
    var second = sp.GetRequiredService<IPythonServiceInspectorRegistry>();

    Assert.That(first, Is.InstanceOf<PythonServiceInspectorRegistry>());
    Assert.That(second, Is.SameAs(first));
  }

  [Test]
  public void UsePython_RegistersPythonExecutor_AsSubprocessSingleton()
  {
    var (builder, _) = MakeBuilder();
    builder.UsePython();
    using var sp = builder.Services.BuildServiceProvider();

    var executor = sp.GetRequiredService<IPythonExecutor>();
    Assert.That(executor, Is.InstanceOf<SubprocessPythonExecutor>(),
      "The default IPythonExecutor must be the subprocess implementation.");
    Assert.That(sp.GetRequiredService<IPythonExecutor>(), Is.SameAs(executor),
      "IPythonExecutor must be a singleton.");
  }

  [Test]
  public void UsePython_RegistersPythonServiceDependencyDispatcher()
  {
    var (builder, _) = MakeBuilder();
    builder.UsePython();
    using var sp = builder.Services.BuildServiceProvider();

    var dispatchers = sp.GetServices<IServiceDependencyDispatcher>().ToArray();
    Assert.That(dispatchers, Is.Not.Empty,
      "UsePython() must contribute an IServiceDependencyDispatcher so PythonServiceDependency can be routed.");
    Assert.That(
      dispatchers.Any(d => d is PythonServiceDependencyDispatcher),
      Is.True,
      "At least one dispatcher must be the PythonServiceDependencyDispatcher matching Category=\"python\"."
    );
  }

  [Test]
  public void UsePython_RegistersPythonStepValidationHook()
  {
    var (builder, _) = MakeBuilder();
    builder.UsePython();
    using var sp = builder.Services.BuildServiceProvider();

    var hooks = sp.GetServices<IFlowValidationHook>().ToArray();
    Assert.That(hooks, Is.Not.Empty,
      "UsePython() must contribute its pre-flight validation hook.");
    Assert.That(
      hooks.Any(h => h is PythonStepValidationHook),
      Is.True,
      "At least one hook must be the PythonStepValidationHook — the single authoritative site for Python step-shape checks."
    );
  }

  [Test]
  public void UsePython_RegistersOptions_ResolvableWithDefaults()
  {
    var (builder, _) = MakeBuilder();
    builder.UsePython();
    using var sp = builder.Services.BuildServiceProvider();

    var options = sp.GetRequiredService<IOptions<PythonRuntimeOptions>>();
    Assert.That(options.Value, Is.Not.Null,
      "IOptions<PythonRuntimeOptions> must resolve even when no Flowthru:Python section is present.");
    Assert.That(options.Value.UvPath, Is.EqualTo("uv"),
      "Unbound options should keep the type-defined defaults.");
  }

  // ── TryAddSingleton semantics ──────────────────────────────────────────

  [Test]
  public void UsePython_PreRegisteredExecutor_TakesPrecedence_OverSubprocessDefault()
  {
    // Pre-register a fake IPythonExecutor BEFORE calling UsePython. The
    // SUT uses TryAddSingleton for IPythonExecutor, so the earlier
    // registration must survive — this pins the "test doubles or user
    // overrides take precedence" comment in PythonFlowthruBuilderExtensions.
    var (builder, services) = MakeBuilder();
    var fake = new FakeExecutor();
    services.AddSingleton<IPythonExecutor>(fake);

    builder.UsePython();
    using var sp = services.BuildServiceProvider();

    var resolved = sp.GetRequiredService<IPythonExecutor>();
    Assert.That(resolved, Is.SameAs(fake),
      "TryAddSingleton must not overwrite a user-supplied IPythonExecutor.");
    Assert.That(resolved, Is.Not.InstanceOf<SubprocessPythonExecutor>(),
      "The subprocess default must be skipped when an executor is already registered.");
  }

  // ── Configuration binding ──────────────────────────────────────────────

  [Test]
  public void UsePython_BindsFlowthruPythonSection_ToOptions()
  {
    var configuration = ConfigFrom(new Dictionary<string, string?>
    {
      ["Flowthru:Python:VenvPath"] = "/tmp/test-venv",
      ["Flowthru:Python:UvPath"] = "uv2",
      ["Flowthru:Python:ConfigurationSection"] = "MySection",
    });
    var (builder, _) = MakeBuilder(configuration);
    builder.UsePython();
    using var sp = builder.Services.BuildServiceProvider();

    var options = sp.GetRequiredService<IOptions<PythonRuntimeOptions>>().Value;

    Assert.That(options.VenvPath, Is.EqualTo("/tmp/test-venv"),
      "VenvPath must round-trip from the Flowthru:Python section.");
    Assert.That(options.UvPath, Is.EqualTo("uv2"),
      "UvPath in the config section must override the type-defined default.");
    Assert.That(options.ConfigurationSection, Is.EqualTo("MySection"),
      "ConfigurationSection must round-trip from config — operators rely on this name to scope the Python env-var bridge.");
  }

  // ── PostConfigure overload ─────────────────────────────────────────────

  [Test]
  public void UsePython_ConfigureOverload_OverridesConfigurationBoundValues()
  {
    // IConfiguration sets VenvPath one way; the PostConfigure callback
    // runs afterward and must win. ModuleSearchPaths mutation through
    // the callback must also be observable.
    var configuration = ConfigFrom(new Dictionary<string, string?>
    {
      ["Flowthru:Python:VenvPath"] = "/from-config",
    });
    var (builder, _) = MakeBuilder(configuration);

    builder.UsePython(opts =>
    {
      opts.VenvPath = "/override";
      opts.ModuleSearchPaths.Add("/extra");
    });

    using var sp = builder.Services.BuildServiceProvider();
    var options = sp.GetRequiredService<IOptions<PythonRuntimeOptions>>().Value;

    Assert.That(options.VenvPath, Is.EqualTo("/override"),
      "PostConfigure must run after IConfiguration binding so the code-first override wins.");
    Assert.That(options.ModuleSearchPaths, Does.Contain("/extra"),
      "PostConfigure mutations to ModuleSearchPaths must be observed on the resolved options.");
  }

  [Test]
  public void UsePython_ConfigureOverload_StillRegistersDefaultServices()
  {
    // The configure overload delegates to the base UsePython(); the full
    // set of DI registrations must still appear.
    var (builder, _) = MakeBuilder();
    builder.UsePython(_ => { });
    using var sp = builder.Services.BuildServiceProvider();

    Assert.Multiple(() =>
    {
      Assert.That(sp.GetService<IPythonConfigurationFlattener>(), Is.Not.Null);
      Assert.That(sp.GetService<IPythonServiceInspectorRegistry>(), Is.Not.Null);
      Assert.That(sp.GetService<IPythonExecutor>(), Is.Not.Null);
      Assert.That(
        sp.GetServices<IServiceDependencyDispatcher>().Any(d => d is PythonServiceDependencyDispatcher),
        Is.True
      );
      Assert.That(
        sp.GetServices<IFlowValidationHook>().Any(h => h is PythonStepValidationHook),
        Is.True
      );
    });
  }

  // ── Fakes ──────────────────────────────────────────────────────────────

  /// <summary>
  /// Minimal IPythonExecutor used to pin TryAddSingleton override
  /// semantics — the implementation does not need to do anything; the
  /// test only checks identity.
  /// </summary>
  private sealed class FakeExecutor : IPythonExecutor
  {
    public FlowIO<Validated<PreFlightError, FlowUnit>> InvokeInspector(
      PythonServiceRegistration registration
    ) => FlowIO.Pure(Validated<PreFlightError, FlowUnit>.Pure(FlowUnit.Default));

    public FlowIO<TOutput> Invoke<TInput, TOutput>(
      string moduleName,
      string functionName,
      TInput input
    ) => FlowIO.Fail<TOutput>(
      new RuntimeError.InvariantViolated("FakeExecutor", "Invoke not used")
    );

    public FlowIO<PythonStepMetadata> ValidateStep(string moduleName, string functionName) =>
      FlowIO.Fail<PythonStepMetadata>(
        new RuntimeError.InvariantViolated("FakeExecutor", "ValidateStep not used")
      );
  }
}
