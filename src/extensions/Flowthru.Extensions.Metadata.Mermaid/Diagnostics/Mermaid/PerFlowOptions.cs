namespace Flowthru.Diagnostics.Mermaid;

/// <summary>
/// Per-flow rendering configuration for the Mermaid metadata provider.
/// Bundled so the provider's top-level builder doesn't need a separate
/// <c>WithPerFlowX</c> method for every knob. Configure via
/// <see cref="MermaidMetadataProviderBuilder.WithPerFlow"/>.
/// </summary>
public sealed class PerFlowOptions
{
  /// <summary>
  /// Whether the merged DAG file holds a per-flow rendering instead
  /// of the monolithic single-block view. Defaults to
  /// <see cref="PerFlowMode.Auto"/>.
  /// </summary>
  public PerFlowMode Mode { get; private set; } = PerFlowMode.Auto;

  /// <summary>
  /// Auto-mode threshold: per-flow rendering is chosen when the
  /// merged DAG contains at least this many distinct flow labels.
  /// Defaults to 4 — i.e. pipelines with more than 3 flows.
  /// </summary>
  public int AutoThreshold { get; private set; } = 4;

  /// <summary>
  /// Markdown heading level used to label each per-flow block in
  /// the rendered document (1 → <c>#</c>, 6 → <c>######</c>).
  /// Defaults to 4, which nests cleanly under the <c>### Diagram</c>
  /// parent heading used by the example READMEs' splice convention.
  /// </summary>
  public int HeadingLevel { get; private set; } = 4;

  /// <summary>Select the per-flow rendering mode.</summary>
  public PerFlowOptions WithMode(PerFlowMode mode)
  {
    Mode = mode;
    return this;
  }

  /// <summary>
  /// Override the <see cref="PerFlowMode.Auto"/> threshold. Must be
  /// at least 1.
  /// </summary>
  public PerFlowOptions WithAutoThreshold(int minFlows)
  {
    if (minFlows < 1)
    {
      throw new ArgumentOutOfRangeException(
        nameof(minFlows), minFlows, "Threshold must be at least 1."
      );
    }
    AutoThreshold = minFlows;
    return this;
  }

  /// <summary>
  /// Set the Markdown heading level for per-flow block labels. Must
  /// be in the range [1, 6].
  /// </summary>
  public PerFlowOptions WithHeadingLevel(int level)
  {
    if (level < 1 || level > 6)
    {
      throw new ArgumentOutOfRangeException(
        nameof(level), level, "Heading level must be between 1 and 6."
      );
    }
    HeadingLevel = level;
    return this;
  }
}
