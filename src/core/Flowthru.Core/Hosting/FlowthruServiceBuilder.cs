using Flowthru.Flow;
using Microsoft.Extensions.DependencyInjection;

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

  /// <inheritdoc/>
  public IFlowthruBuilder RegisterCatalog<TCatalog>(Func<IServiceProvider, TCatalog> factory)
    where TCatalog : class
  {
    if (factory is null) throw new ArgumentNullException(nameof(factory));
    Services.AddSingleton(factory);
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
    Func<IServiceProvider, FlowIO<Validated<PreFlightError, FlowUnit>>> validate
  )
  {
    if (string.IsNullOrWhiteSpace(hookId))
      throw new ArgumentException("Hook id required.", nameof(hookId));
    if (validate is null) throw new ArgumentNullException(nameof(validate));
    _registrationHooks.Add(new FunctionRegistrationValidationHook(hookId, validate));
    return this;
  }

  private sealed class FunctionRegistrationValidationHook : IRegistrationValidationHook
  {
    private readonly Func<IServiceProvider, FlowIO<Validated<PreFlightError, FlowUnit>>> _validate;
    public FunctionRegistrationValidationHook(
      string hookId,
      Func<IServiceProvider, FlowIO<Validated<PreFlightError, FlowUnit>>> validate
    )
    {
      HookId = hookId;
      _validate = validate;
    }
    public string HookId { get; }
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
