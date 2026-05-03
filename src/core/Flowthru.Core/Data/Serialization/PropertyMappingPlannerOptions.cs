namespace Flowthru.Core.Data.Serialization;

/// <summary>
/// Options that adjust how <see cref="PropertyMappingPlanner"/> classifies and emits
/// bindings. Defaults match the conservative behavior used by most format extensions.
/// </summary>
public sealed class PropertyMappingPlannerOptions
{
  /// <summary>
  /// String values that nullable properties should treat as <c>null</c> on read. The
  /// default is the single empty string — matching CsvHelper's traditional behavior of
  /// reading an empty cell as a null on nullable fields. Format extensions that need
  /// richer null-sentinel behavior (e.g., treating <c>"NA"</c> or <c>"\\N"</c> as null
  /// for some upstream sources) pass an extended list.
  /// </summary>
  public IReadOnlyList<string> NullSentinels { get; init; } = new[] { string.Empty };
}
