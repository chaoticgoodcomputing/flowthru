using Flowthru.Caching;
using Flowthru.Data.Catalog;
using Flowthru.Flow;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Flowthru.Hosting;

/// <summary>
/// Concrete <see cref="IFlowthruBuilder"/>. Holds the registrations
/// supplied during <c>services.AddFlowthru(builder => …)</c>; the
/// resulting state is consumed by <see cref="FlowthruService"/> when
/// the host materialises and runs flows.
/// </summary>
/// <remarks>
/// <para>
/// Per §2.6, catalogs are DI-resolvable values. Each
/// <see cref="RegisterCatalog{TCatalog}"/> call delegates straight to
/// the host's <see cref="IServiceCollection"/> as
/// <c>AddSingleton&lt;TCatalog&gt;</c>; flow registrations capture
/// the user's factory delegate plus the typed list of catalog types
/// the framework should resolve from DI before invoking it.
/// </para>
/// </remarks>
public sealed class FlowthruServiceBuilder : IFlowthruBuilder
{
  private readonly List<FlowRegistration> _flows = new();
  private readonly List<IFlowValidationHook> _validationHooks = new();
  private readonly List<IRegistrationValidationHook> _registrationHooks = new();
  private readonly List<InspectorRegistration> _inspectors = new();
  private readonly List<Type> _catalogTypes = new();
  private readonly FlowthruMetadataBuilder _metadataBuilder = new();

  public FlowthruServiceBuilder(IServiceCollection services)
  {
    Services = services ?? throw new ArgumentNullException(nameof(services));
  }

  /// <inheritdoc/>
  public IServiceCollection Services { get; }

  /// <summary>
  /// Register an <see cref="IConfiguration"/> as a DI singleton, making
  /// it resolvable by catalog factories that consume it through
  /// <see cref="Flowthru.Data.Catalog.Configuration.ConfigurationItem{T}"/>.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Phase 5 of the smart-caching-and-slicing RFC re-introduces the
  /// pre-0.17 config-as-catalog pattern. Inside a host setup:
  /// </para>
  /// <code>
  /// services.AddFlowthru(b =&gt;
  /// {
  ///   b.UseConfiguration(hostContext.Configuration);
  ///   b.RegisterCatalog(sp =&gt;
  ///     new MyCatalog(sp.GetRequiredService&lt;IConfiguration&gt;()));
  ///   b.RegisterFlow&lt;MyCatalog&gt;("main", c =&gt;
  ///     FlowBuilder.CreateFlow("main", p =&gt; /* steps reading c.FlowConfig */));
  /// });
  /// </code>
  /// <para>
  /// <b>Reload semantics:</b> The captured <see cref="IConfiguration"/>
  /// is held for the lifetime of the FlowthruService. v1 does not
  /// participate in host-level reload events — if your config changes
  /// between flow runs, the next pre-flight pass observes the new
  /// values (and produces a distinct fingerprint, invalidating cache).
  /// Within a single run, config is stable.
  /// </para>
  /// <para>
  /// <b>Last-call-wins:</b> Multiple <see cref="UseConfiguration"/>
  /// calls replace prior registrations (standard
  /// <see cref="ServiceCollectionDescriptorExtensions.Replace"/>
  /// semantics). Hosts that need to layer config sources should
  /// compose them via <see cref="IConfigurationBuilder"/> before
  /// calling.
  /// </para>
  /// </remarks>
  /// <param name="configuration">The host-built configuration root.</param>
  /// <exception cref="ArgumentNullException">
  /// Thrown when <paramref name="configuration"/> is null.
  /// </exception>
  public IFlowthruBuilder UseConfiguration(IConfiguration configuration)
  {
    if (configuration is null) throw new ArgumentNullException(nameof(configuration));
    Services.Replace(ServiceDescriptor.Singleton(configuration));
    return this;
  }

  /// <inheritdoc/>
  public IFlowthruBuilder UseCacheStorage(Func<IServiceProvider, IItem<CacheManifest>> factory)
  {
    if (factory is null) throw new ArgumentNullException(nameof(factory));
    Services.Replace(ServiceDescriptor.Singleton<IItem<CacheManifest>>(factory));
    return this;
  }

  /// <inheritdoc/>
  public IFlowthruBuilder RegisterCatalog<TCatalog>(Func<IServiceProvider, TCatalog> factory)
    where TCatalog : class
  {
    if (factory is null) throw new ArgumentNullException(nameof(factory));
    // Wrap the user-supplied factory so any catalog that derives from
    // CatalogAbstract picks up the DI-resolved IStorageMediumResolver
    // automatically — even if the user's constructor didn't thread one
    // through. CreateItem<T> consumes this resolver to push the ambient
    // slot during materialization (Phase 1 of the smart-caching RFC).
    Services.AddSingleton(sp =>
    {
      var catalog = factory(sp);
      if (catalog is Flowthru.Data.Catalog.CatalogAbstract abstractCatalog
          && abstractCatalog.Resolver is null)
      {
        var resolver = sp.GetService<Flowthru.Data.Storage.IStorageMediumResolver>();
        if (resolver is not null)
        {
          abstractCatalog.AttachResolver(resolver);
        }
      }
      return catalog;
    });
    _catalogTypes.Add(typeof(TCatalog));
    return this;
  }

  // ── RegisterFlow arity overloads ────────────────────────────────────────

  /// <inheritdoc/>
  public IFlowRegistration RegisterFlow(string label, Func<BuiltFlow> factory)
  {
    Validate(label, factory);
    return Add(label, _ => factory());
  }

  /// <inheritdoc/>
  public IFlowRegistration RegisterFlow<T1>(string label, Func<T1, BuiltFlow> factory)
    where T1 : class
  {
    Validate(label, factory);
    return Add(label, sp => factory(sp.GetRequiredService<T1>()));
  }

  /// <inheritdoc/>
  public IFlowRegistration RegisterFlow<T1, T2>(string label, Func<T1, T2, BuiltFlow> factory)
    where T1 : class
    where T2 : class
  {
    Validate(label, factory);
    return Add(label, sp => factory(
      sp.GetRequiredService<T1>(),
      sp.GetRequiredService<T2>()
    ));
  }

  /// <inheritdoc/>
  public IFlowRegistration RegisterFlow<T1, T2, T3>(
    string label,
    Func<T1, T2, T3, BuiltFlow> factory
  )
    where T1 : class
    where T2 : class
    where T3 : class
  {
    Validate(label, factory);
    return Add(label, sp => factory(
      sp.GetRequiredService<T1>(),
      sp.GetRequiredService<T2>(),
      sp.GetRequiredService<T3>()
    ));
  }

  /// <inheritdoc/>
  public IFlowRegistration RegisterFlow<T1, T2, T3, T4>(
    string label,
    Func<T1, T2, T3, T4, BuiltFlow> factory
  )
    where T1 : class
    where T2 : class
    where T3 : class
    where T4 : class
  {
    Validate(label, factory);
    return Add(label, sp => factory(
      sp.GetRequiredService<T1>(),
      sp.GetRequiredService<T2>(),
      sp.GetRequiredService<T3>(),
      sp.GetRequiredService<T4>()
    ));
  }

  /// <inheritdoc/>
  public IFlowRegistration RegisterFlow<T1, T2, T3, T4, T5>(
    string label,
    Func<T1, T2, T3, T4, T5, BuiltFlow> factory
  )
    where T1 : class
    where T2 : class
    where T3 : class
    where T4 : class
    where T5 : class
  {
    Validate(label, factory);
    return Add(label, sp => factory(
      sp.GetRequiredService<T1>(),
      sp.GetRequiredService<T2>(),
      sp.GetRequiredService<T3>(),
      sp.GetRequiredService<T4>(),
      sp.GetRequiredService<T5>()
    ));
  }

  // ── Hooks / inspectors / metadata ──────────────────────────────────────

  /// <inheritdoc/>
  public IFlowthruBuilder RegisterValidationHook(IFlowValidationHook hook)
  {
    if (hook is null) throw new ArgumentNullException(nameof(hook));
    _validationHooks.Add(hook);
    return this;
  }

  /// <inheritdoc/>
  public IFlowthruBuilder RegisterValidationHook(IRegistrationValidationHook hook)
  {
    if (hook is null) throw new ArgumentNullException(nameof(hook));
    _registrationHooks.Add(hook);
    return this;
  }

  /// <inheritdoc/>
  public IFlowthruBuilder RegisterValidationHook(
    string hookId,
    Func<IServiceProvider, FlowIO<Validated<PreFlightError, FlowUnit>>> validate,
    ValidationDepth minimumDepth = ValidationDepth.Shallow
  )
  {
    if (string.IsNullOrWhiteSpace(hookId))
      throw new ArgumentException("Hook id required.", nameof(hookId));
    if (validate is null) throw new ArgumentNullException(nameof(validate));
    _registrationHooks.Add(new FunctionRegistrationValidationHook(hookId, validate, minimumDepth));
    return this;
  }

  private sealed class FunctionRegistrationValidationHook : IRegistrationValidationHook
  {
    private readonly Func<IServiceProvider, FlowIO<Validated<PreFlightError, FlowUnit>>> _validate;
    public FunctionRegistrationValidationHook(
      string hookId,
      Func<IServiceProvider, FlowIO<Validated<PreFlightError, FlowUnit>>> validate,
      ValidationDepth minimumDepth = ValidationDepth.Shallow
    )
    {
      HookId = hookId;
      _validate = validate;
      MinimumDepth = minimumDepth;
    }
    public string HookId { get; }
    public ValidationDepth MinimumDepth { get; }
    public FlowIO<Validated<PreFlightError, FlowUnit>> Validate(IServiceProvider services) =>
      _validate(services);
  }

  /// <inheritdoc/>
  public IFlowthruBuilder AddFlowServiceInspector<TService>(IFlowServiceInspector<TService> inspector)
    where TService : class
  {
    if (inspector is null) throw new ArgumentNullException(nameof(inspector));
    _inspectors.Add(BuildInspectorRegistration<TService>(
      (svc, ct) => inspector.InspectAsync(svc, ct)
    ));
    return this;
  }

  /// <inheritdoc/>
  public IFlowthruBuilder AddFlowServiceInspector<TService>(
    Func<TService, CancellationToken, Task<InspectionResult>> probe
  )
    where TService : class
  {
    if (probe is null) throw new ArgumentNullException(nameof(probe));
    _inspectors.Add(BuildInspectorRegistration(probe));
    return this;
  }

  /// <summary>
  /// Bridge a user-facing <see cref="InspectionResult"/>-returning
  /// probe into the dispatcher pipeline's
  /// <see cref="FlowIO{A}"/>+<see cref="Validated{TError, TValue}"/>
  /// shape. Hides the FP-algebra unwrap from the call site.
  /// </summary>
  private static InspectorRegistration BuildInspectorRegistration<TService>(
    Func<TService, CancellationToken, Task<InspectionResult>> probe
  )
    where TService : class
  {
    return new InspectorRegistration(typeof(TService), sp =>
      FlowIO.LiftAsync<Validated<PreFlightError, FlowUnit>>(
        async ct =>
        {
          var service = sp.GetService<TService>()
            ?? throw new InvalidOperationException(
              $"Inspector for service type '{typeof(TService).FullName}' was registered, "
              + "but the service itself is not registered in DI. "
              + "Call services.AddSingleton<" + typeof(TService).Name + ">(...) before AddFlowthru."
            );
          var result = await probe(service, ct).ConfigureAwait(false);
          return result.Internal;
        },
        source: $"FlowServiceInspector[{typeof(TService).Name}]"
      )
    );
  }

  /// <inheritdoc/>
  public IFlowthruBuilder ConfigureMetadata(Action<FlowthruMetadataBuilder> configure)
  {
    if (configure is null) throw new ArgumentNullException(nameof(configure));
    configure(_metadataBuilder);
    return this;
  }

  /// <inheritdoc/>
  public IFlowthruBuilder ConfigureExecution(Action<ExecutionDefaults> configure)
  {
    if (configure is null) throw new ArgumentNullException(nameof(configure));
    // Defer to the standard Options pipeline so this composes with any
    // appsettings binding and with the validator registered in
    // AddFlowthru. Multiple ConfigureExecution calls stack as ordered
    // IConfigureOptions<ExecutionDefaults>.
    Services.Configure(configure);
    return this;
  }

  // ── Internal accessors used by FlowthruService ─────────────────────────

  internal IReadOnlyList<FlowRegistration> Flows => _flows;
  internal IReadOnlyList<IFlowValidationHook> ValidationHooks => _validationHooks;
  internal IReadOnlyList<IRegistrationValidationHook> RegistrationHooks => _registrationHooks;
  internal IReadOnlyList<InspectorRegistration> Inspectors => _inspectors;
  internal IReadOnlyList<Type> CatalogTypes => _catalogTypes;
  internal FlowthruMetadataBuilder MetadataBuilder => _metadataBuilder;

  private static void Validate(string label, object factory)
  {
    if (string.IsNullOrEmpty(label))
      throw new ArgumentException("Flow label must be non-empty.", nameof(label));
    if (factory is null) throw new ArgumentNullException(nameof(factory));
  }

  private FlowRegistration Add(string label, Func<IServiceProvider, BuiltFlow> resolver)
  {
    var registration = new FlowRegistration(label, resolver);
    _flows.Add(registration);
    return registration;
  }

  internal sealed class FlowRegistration : IFlowRegistration
  {
    public FlowRegistration(string label, Func<IServiceProvider, BuiltFlow> resolver)
    {
      Label = label;
      Resolver = resolver;
    }

    public string Label { get; }
    public string? Description { get; private set; }
    public Func<IServiceProvider, BuiltFlow> Resolver { get; }

    public IFlowRegistration WithDescription(string description)
    {
      if (description is null) throw new ArgumentNullException(nameof(description));
      Description = description;
      return this;
    }
  }

  internal sealed record InspectorRegistration(
    Type ServiceType,
    Func<IServiceProvider, FlowIO<Validated<PreFlightError, FlowUnit>>> Probe
  );
}
