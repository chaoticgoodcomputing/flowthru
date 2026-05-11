namespace Flowthru.Diagnostics;

/// <summary>
/// Pre-run metadata source — produces a side-effect (typically a
/// file or document) describing the about-to-run flow before any
/// step executes. JSON manifest exporters, Mermaid diagram emitters,
/// and row-count baselines all implement this interface.
/// </summary>
/// <remarks>
/// <para>
/// Per §2.10, providers live in extension packages
/// (<c>Flowthru.Diagnostics.Mermaid</c>,
/// <c>Flowthru.Diagnostics.Json</c>,
/// <c>Flowthru.Diagnostics.Run</c>); Core ships only the contract,
/// the registration entry point, and <see cref="FlowthruMetadataBuilder"/>
/// for orchestration.
/// </para>
/// <para>
/// <strong>Why <see cref="FlowMetadataContext"/>, not just
/// <see cref="Flowthru.Flow.BuiltFlow"/>.</strong> A complete metadata
/// surface must let third-party providers do whatever they need with
/// the run — including rendering the full merged DAG with the active
/// slice highlighted, filtering out inactive nodes, or annotating
/// edges that cross flow-of-origin boundaries. The context envelope
/// carries the merged topology, the slice the host is actually
/// running, and the requested flow label, so providers never have to
/// reverse-engineer those facts from a lossy <see cref="Flowthru.Flow.BuiltFlow"/>
/// view.
/// </para>
/// </remarks>
public interface IMetadataProvider
{
  /// <summary>
  /// Stable provider identifier — used as the dispatcher key when the
  /// host orchestrates multiple providers and as a label in post-run
  /// reports.
  /// </summary>
  string ProviderId { get; }

  /// <summary>
  /// Emit metadata for <paramref name="ctx"/>. Returns a successful
  /// effect when the artifact was produced; failures surface as
  /// <c>RuntimeError.External</c>.
  /// </summary>
  FlowIO<FlowUnit> Emit(FlowMetadataContext ctx);
}

/// <summary>
/// Post-run metadata source — receives the
/// <see cref="Flowthru.Flow.FlowResult"/> alongside the same static
/// context the pre-run providers saw, so the provider can emit
/// step-timing baselines, row-count diffs, or run summaries after
/// execution completes.
/// </summary>
public interface IPostRunMetadataProvider
{
  /// <summary>Stable provider identifier — see <see cref="IMetadataProvider.ProviderId"/>.</summary>
  string ProviderId { get; }

  /// <summary>Emit metadata for <paramref name="ctx"/>.</summary>
  FlowIO<FlowUnit> Emit(FlowRunMetadataContext ctx);
}
