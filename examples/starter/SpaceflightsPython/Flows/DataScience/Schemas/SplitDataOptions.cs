using Flowthru.Data.Schema;

namespace SpaceflightsPython.Flows.DataScience.Schemas;

/// <summary>
/// Train/test split options for the Python <c>split_data</c> step.
/// Sourced from <c>Flowthru:Flows:DataScience:SplitDataOptions</c> in
/// <c>appsettings.json</c> via <see cref="Flowthru.Data.Catalog.Configuration.ConfigurationItem{T}"/>
/// and marshalled into Python as a JSON scalar (Phase 9 singleton path).
/// </summary>
/// <remarks>
/// The record carries <c>[FlowthruSchema]</c> so the Python source
/// generator can resolve the schema name referenced from the
/// <c>@step(inputs=["SplitDataOptions"])</c> decorator. At runtime the
/// executor's <c>ClassifyType</c> returns <c>scalar</c> because the
/// record is not <c>IEnumerable</c>; encoding then flows through the
/// JSON path rather than Arrow IPC. Property names round-trip
/// verbatim — Python reads <c>options["TestSize"]</c> etc.
/// </remarks>
[FlowthruSchema]
public partial record SplitDataOptions
{
  /// <summary>Proportion of rows to allocate to the test split.</summary>
  public double TestSize { get; init; } = 0.2;

  /// <summary>Random seed for reproducible shuffling.</summary>
  public int RandomState { get; init; } = 3;

  /// <summary>Feature columns extracted from the model input table.</summary>
  public string[] Features { get; init; } = Array.Empty<string>();
}
