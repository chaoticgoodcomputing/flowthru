using Flowthru.Core.Flows;

namespace Flowthru.Core.Services;

/// <summary>
/// Context passed to catalog-level pre-flight checks and resource lifecycle
/// hooks. Carries the identity of the merged flow being executed plus the
/// configuration and DI scope it was registered against.
/// </summary>
/// <remarks>
/// <para>
/// <strong>FlowLabel</strong> is the merged flow's display name — typically
/// the concrete flow name when one is being run alone, or
/// <c>"Pipeline"</c> when multiple flows are merged. Catalogs that need to
/// scope behaviour to a specific flow can branch on this value.
/// </para>
/// <para>
/// <strong>Services</strong> exposes the host's DI container so catalog
/// validators can resolve dependencies (configuration sections, sibling
/// services). Use it for read-only queries; resource acquisition state
/// belongs in the <see cref="Flowthru.Core.Effects.FlowResource{TScope}"/>
/// itself, not in DI.
/// </para>
/// </remarks>
/// <param name="FlowLabel">The merged flow's identifying label.</param>
/// <param name="Options">The execution options for the current run.</param>
/// <param name="Services">The host's service provider.</param>
public sealed record FlowExecutionContext(
  string FlowLabel,
  ExecutionOptions Options,
  IServiceProvider Services
);
