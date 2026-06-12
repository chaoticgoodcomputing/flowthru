using Flowthru.Validation.Runtime;
using Flowthru.Validation.Runtime.Gql;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Flowthru.Hosting;

/// <summary>
/// <c>UseGql()</c> extension on <see cref="IFlowthruBuilder"/>. The GQL
/// extension brings no client of its own — the catalog author wires their
/// own StrawberryShake client and passes operation delegates — so this is
/// the one piece of host wiring it needs: registering the
/// <see cref="IServiceProfileContributor"/> that enforces opt-in endpoint
/// concurrency caps (ADR-0019, issue #104).
/// </summary>
public static class GqlFlowthruBuilderExtensions
{
  /// <summary>
  /// Enable GraphQL scheduler conflict gating. Registers the contributor
  /// that resolves an endpoint throttle declared via
  /// <c>IItem&lt;T&gt;.WithGqlConcurrency(...)</c> into an enforced
  /// capacity. Call it once when a catalog throttles a rate-limited GQL
  /// endpoint; without it, <c>WithGqlConcurrency</c> declarations resolve
  /// to unbounded and the cap is a no-op (default GQL behaviour).
  /// </summary>
  /// <remarks>
  /// Idempotent — the contributor is registered with
  /// <see cref="ServiceCollectionDescriptorExtensions.TryAddEnumerable(IServiceCollection, ServiceDescriptor)"/>
  /// semantics so repeated calls don't stack duplicates.
  /// </remarks>
  /// <param name="builder">The Flowthru builder.</param>
  public static IFlowthruBuilder UseGql(this IFlowthruBuilder builder)
  {
    if (builder is null) throw new ArgumentNullException(nameof(builder));

    builder.Services.TryAddEnumerable(
      ServiceDescriptor.Singleton<IServiceProfileContributor, GqlEndpointProfileContributor>());

    return builder;
  }
}
