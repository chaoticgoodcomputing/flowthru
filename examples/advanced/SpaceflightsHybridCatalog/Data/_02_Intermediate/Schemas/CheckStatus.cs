using Flowthru.Data.Schema;

namespace SpaceflightsHybridCatalog.Data._02_Intermediate.Schemas;

/// <summary>
/// Categorical status of a maintenance check, with an explicit string mapping for the
/// raw "t"/"f" flag format used by upstream data sources.
/// </summary>
/// <remarks>
/// Demonstrates the <c>[SerializedEnum]</c> pattern for the file-backed Development
/// catalog. In the Production (EFCore) catalog the same enum is mapped via
/// <c>HasConversion&lt;string&gt;()</c> in <see cref="SpaceflightsHybridCatalog.Data.SpaceflightsDbContext"/>
/// — the on-disk DB representation stores the enum member name ("Complete"/"Incomplete"),
/// while the on-disk file representation stores the original "t"/"f" flag.
/// </remarks>
public enum CheckStatus
{
  [SerializedEnum("t")]
  Complete,

  [SerializedEnum("f")]
  Incomplete,
}
