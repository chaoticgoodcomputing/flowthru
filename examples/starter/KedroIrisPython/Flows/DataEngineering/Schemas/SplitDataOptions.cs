using Flowthru.Data.Schema;

namespace KedroIrisPython.Flows.DataEngineering.Schemas;

/// <summary>
/// Train/test split options for the Iris Python <c>split_data</c> step.
/// Sourced from <c>Flowthru:Flows:DataEngineering:SplitDataOptions</c>
/// via <see cref="Flowthru.Data.Catalog.Configuration.ConfigurationItem{T}"/>
/// and marshalled to Python as a JSON scalar (Phase 9 singleton path).
/// </summary>
[FlowthruSchema]
public partial record SplitDataOptions
{
  /// <summary>Proportion of rows to allocate to the test split.</summary>
  public double TestDataRatio { get; init; } = 0.2;

  /// <summary>Random seed for reproducible shuffling.</summary>
  public int RandomState { get; init; } = 42;
}
