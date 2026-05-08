namespace Flowthru.Diagnostics;

/// <summary>
/// Orchestrates a list of <see cref="IMetadataProvider"/> and
/// <see cref="IPostRunMetadataProvider"/> for a single flow run.
/// The host (<c>FlowthruService</c>) holds an instance, configures
/// providers via DI, and invokes
/// <see cref="EmitPreRun"/> / <see cref="EmitPostRun"/> at the
/// appropriate run-lifecycle points.
/// </summary>
public sealed class FlowthruMetadataBuilder
{
  private readonly List<IMetadataProvider> _preRun = new();
  private readonly List<IPostRunMetadataProvider> _postRun = new();

  /// <summary>Register a pre-run provider (runs before flow execution).</summary>
  public FlowthruMetadataBuilder AddProvider(IMetadataProvider provider)
  {
    if (provider is null) throw new ArgumentNullException(nameof(provider));
    _preRun.Add(provider);
    return this;
  }

  /// <summary>Register a post-run provider (runs after flow execution).</summary>
  public FlowthruMetadataBuilder AddProvider(IPostRunMetadataProvider provider)
  {
    if (provider is null) throw new ArgumentNullException(nameof(provider));
    _postRun.Add(provider);
    return this;
  }

  /// <summary>Pre-run providers in registration order.</summary>
  public IReadOnlyList<IMetadataProvider> PreRunProviders => _preRun;

  /// <summary>Post-run providers in registration order.</summary>
  public IReadOnlyList<IPostRunMetadataProvider> PostRunProviders => _postRun;

  /// <summary>
  /// Run every pre-run provider against <paramref name="ctx"/>.
  /// Failures aggregate into the returned effect's failure.
  /// </summary>
  public FlowIO<FlowUnit> EmitPreRun(FlowMetadataContext ctx)
  {
    if (ctx is null) throw new ArgumentNullException(nameof(ctx));
    return RunAll(_preRun.Select(p => p.Emit(ctx)));
  }

  /// <summary>Run every post-run provider with <paramref name="ctx"/>.</summary>
  public FlowIO<FlowUnit> EmitPostRun(FlowRunMetadataContext ctx)
  {
    if (ctx is null) throw new ArgumentNullException(nameof(ctx));
    return RunAll(_postRun.Select(p => p.Emit(ctx)));
  }

  private static FlowIO<FlowUnit> RunAll(IEnumerable<FlowIO<FlowUnit>> effects)
  {
    var seq = effects.ToList();
    if (seq.Count == 0) return FlowIO.Pure(FlowUnit.Default);
    return seq.Aggregate((acc, next) => acc.Bind(_ => next));
  }
}
