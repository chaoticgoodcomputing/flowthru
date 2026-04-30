using Flowthru.Core.Data.Validation;
using Flowthru.Core.Effects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Flowthru.Core.Services;

/// <summary>
/// <see cref="IServiceCollection"/> extensions for registering Flowthru preflight
/// inspectors against external services. Sidecar registration is the single path —
/// services themselves never implement Flowthru types.
/// </summary>
/// <remarks>
/// <para>
/// Each registration occupies the <see cref="IFlowthruInspector{TService}"/> DI slot
/// for the chosen service type. Both overloads use <c>TryAddSingleton</c> semantics so
/// user re-registrations override extension defaults regardless of registration order.
/// </para>
/// <para>
/// <strong>Worked example (third-party SDK service):</strong>
/// </para>
/// <code>
/// services
///     .AddAWSService&lt;IAmazonLambda&gt;()
///     .AddFlowthruInspect&lt;IAmazonLambda&gt;((client, ct) =&gt;
///         FlowIO.LiftAsync(async () =&gt;
///         {
///             try
///             {
///                 await client.GetAccountSettingsAsync(ct);
///                 return ValidationResult.Success();
///             }
///             catch (Exception ex)
///             {
///                 return ValidationResult.FromException("Lambda", ex);
///             }
///         }));
/// </code>
/// </remarks>
public static class FlowthruInspectionExtensions
{
  /// <summary>
  /// Registers a delegate-based preflight inspector for <typeparamref name="TService"/>.
  /// </summary>
  /// <typeparam name="TService">The service type to inspect.</typeparam>
  /// <param name="services">The DI service collection.</param>
  /// <param name="probe">
  /// Inspection delegate. Receives the resolved service instance and a cancellation
  /// token; returns a <see cref="FlowIO{ValidationResult}"/> describing the probe outcome.
  /// </param>
  /// <returns>The same <paramref name="services"/> for chaining.</returns>
  public static IServiceCollection AddFlowthruInspect<TService>(
    this IServiceCollection services,
    Func<TService, CancellationToken, FlowIO<ValidationResult>> probe
  )
    where TService : notnull
  {
    if (services is null)
    {
      throw new ArgumentNullException(nameof(services));
    }
    if (probe is null)
    {
      throw new ArgumentNullException(nameof(probe));
    }

    services.TryAddSingleton<IFlowthruInspector<TService>>(_ => new DelegateInspector<TService>(probe));
    return services;
  }

  /// <summary>
  /// Registers a class-based preflight inspector for <typeparamref name="TService"/>.
  /// Use this overload when the inspector itself has DI dependencies that can't be
  /// captured cleanly in a delegate closure.
  /// </summary>
  /// <typeparam name="TService">The service type to inspect.</typeparam>
  /// <typeparam name="TInspector">The concrete inspector implementation.</typeparam>
  /// <param name="services">The DI service collection.</param>
  /// <returns>The same <paramref name="services"/> for chaining.</returns>
  public static IServiceCollection AddFlowthruInspect<TService, TInspector>(
    this IServiceCollection services
  )
    where TService : notnull
    where TInspector : class, IFlowthruInspector<TService>
  {
    if (services is null)
    {
      throw new ArgumentNullException(nameof(services));
    }

    services.TryAddSingleton<IFlowthruInspector<TService>, TInspector>();
    return services;
  }
}
