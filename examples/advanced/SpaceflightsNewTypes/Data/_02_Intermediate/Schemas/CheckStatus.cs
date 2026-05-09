using Flowthru.Data.Schema;

namespace SpaceflightsNewTypes.Data._02_Intermediate.Schemas;

/// <summary>
/// Categorical status of a maintenance check, with an explicit string mapping for the
/// raw "t"/"f" flag format used by upstream data sources.
/// </summary>
/// <remarks>
/// Demonstrates the <c>[SerializedEnum]</c> pattern: each enum member declares its on-disk
/// string representation, and Flowthru's enum infrastructure handles round-tripping through
/// any catalog format. Using an enum here (instead of <c>bool</c>) makes the data shape
/// self-documenting and lets future statuses be added without breaking existing data.
/// </remarks>
public enum CheckStatus
{
  /// <summary>Check has been completed.</summary>
  [SerializedEnum("t")]
  Complete,

  /// <summary>Check has not been completed.</summary>
  [SerializedEnum("f")]
  Incomplete,
}
