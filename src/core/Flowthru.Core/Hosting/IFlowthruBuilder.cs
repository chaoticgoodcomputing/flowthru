using Flowthru.Flow;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Hosting;

/// <summary>
/// DI-time builder for assembling the parts of an
/// <see cref="IFlowthruService"/>: any number of catalog factories,
/// one or more flow factories, optional metadata providers,
/// <see cref="IFlowValidationHook"/> registrations, and
/// <see cref="IFlowServiceInspector{T}"/> sidecars.
/// </summary>
/// <remarks>
/// <para>
/// Per §2.6 / §2.7, catalogs are values resolved from the host's
/// <see cref="IServiceProvider"/>. Each
/// <see cref="RegisterCatalog{TCatalog}"/> call adds a catalog as a
/// singleton in DI; flows declare which catalogs they need by
/// parameter list, and the framework resolves each catalog from DI
/// before invoking the flow factory. Multiple catalogs in one host
/// is the canonical multi-domain authoring shape (see
/// SpaceflightsDistributed) — DataProcessing, DataScience, and
/// Reporting catalogs co-exist and flows compose across them.
/// </para>
/// <para>
/// The catalog-arity-N <c>RegisterFlow</c> overloads (1..5) are
/// hand-written for the common cases; the source generator would
/// extend this matrix mechanically if needed.
/// </para>
/// </remarks>
public interface IFlowthruBuilder
{
  /// <summary>The host's <see cref="IServiceCollection"/> — extensions register additional services here.</summary>
  IServiceCollection Services { get; }

  /// <summary>
  /// Register a catalog factory as a DI singleton. The factory runs
  /// once on first resolution with the host's
  /// <see cref="IServiceProvider"/> available for service injection.
  /// May be called any number of times — each <typeparamref name="TCatalog"/>
  /// is registered independently and resolved by type when a flow
  /// declares it as a parameter.
  /// </summary>
  /// <typeparam name="TCatalog">
  /// The catalog type. May be a <c>CatalogAbstract</c> subclass, a
  /// configuration record, or any DI-friendly reference type — the
  /// principle is "DI-resolvable values that flow into <c>Create</c>
  /// methods" per §2.6.
  /// </typeparam>
  IFlowthruBuilder RegisterCatalog<TCatalog>(Func<IServiceProvider, TCatalog> factory)
    where TCatalog : class;

  /// <summary>Register a zero-catalog flow.</summary>
  IFlowRegistration RegisterFlow(string label, Func<BuiltFlow> factory);

  /// <summary>Register a flow that depends on one catalog.</summary>
  IFlowRegistration RegisterFlow<T1>(string label, Func<T1, BuiltFlow> factory)
    where T1 : class;

  /// <summary>Register a flow that depends on two catalogs.</summary>
  IFlowRegistration RegisterFlow<T1, T2>(string label, Func<T1, T2, BuiltFlow> factory)
    where T1 : class
    where T2 : class;

  /// <summary>Register a flow that depends on three catalogs.</summary>
  IFlowRegistration RegisterFlow<T1, T2, T3>(string label, Func<T1, T2, T3, BuiltFlow> factory)
    where T1 : class
    where T2 : class
    where T3 : class;

  /// <summary>Register a flow that depends on four catalogs.</summary>
  IFlowRegistration RegisterFlow<T1, T2, T3, T4>(
    string label,
    Func<T1, T2, T3, T4, BuiltFlow> factory
  )
    where T1 : class
    where T2 : class
    where T3 : class
    where T4 : class;

  /// <summary>Register a flow that depends on five catalogs.</summary>
  IFlowRegistration RegisterFlow<T1, T2, T3, T4, T5>(
    string label,
    Func<T1, T2, T3, T4, T5, BuiltFlow> factory
  )
    where T1 : class
    where T2 : class
    where T3 : class
    where T4 : class
    where T5 : class;

  /// <summary>Register a per-flow pre-flight <see cref="IFlowValidationHook"/>.</summary>
  IFlowthruBuilder RegisterValidationHook(IFlowValidationHook hook);

  /// <summary>
  /// Register a host-startup <see cref="IRegistrationValidationHook"/>
  /// that runs once per process before any flow's pre-flight pipeline.
  /// Use for "this entire host is misconfigured" checks (bad
  /// connection strings, missing DI services, etc.).
  /// </summary>
  IFlowthruBuilder RegisterValidationHook(IRegistrationValidationHook hook);

  /// <summary>
  /// Function-shaped registration of a host-startup hook — the lambda
  /// receives the configured DI provider and returns an
  /// aggregating <see cref="Validated{TError, TValue}"/>.
  /// </summary>
  IFlowthruBuilder RegisterValidationHook(
    string hookId,
    Func<IServiceProvider, FlowIO<Validated<PreFlightError, FlowUnit>>> validate
  );

  /// <summary>
  /// Register a service-reachability inspector for
  /// <typeparamref name="TService"/>. The host resolves the service
  /// from DI and passes it to <paramref name="inspector"/> at
  /// pre-flight time. Use this overload for non-trivial inspectors
  /// that benefit from being a real type.
  /// </summary>
  IFlowthruBuilder AddFlowServiceInspector<TService>(IFlowServiceInspector<TService> inspector)
    where TService : class;

  /// <summary>
  /// Function-shaped registration of a service inspector — for
  /// declarative one-line probes. The probe receives the resolved
  /// service and a cancellation token; return
  /// <see cref="Inspect.Pass"/> on success or
  /// <see cref="Inspect.Fail(string, string?)"/> on failure.
  /// </summary>
  IFlowthruBuilder AddFlowServiceInspector<TService>(
    Func<TService, CancellationToken, Task<InspectionResult>> probe
  )
    where TService : class;

  /// <summary>
  /// Configure the <see cref="FlowthruMetadataBuilder"/> the host
  /// will use to orchestrate metadata providers.
  /// </summary>
  IFlowthruBuilder ConfigureMetadata(Action<FlowthruMetadataBuilder> configure);
}
