using Flowthru.Core.Data;
using SpaceflightsNewTypes.Data._03_Primary.Schemas;

namespace SpaceflightsNewTypes.Data;

/// <summary>
/// Primary data layer: Domain model data.
/// Contains datasets structured according to the problem being solved, not the source system structure.
/// </summary>
public partial class Catalog
{
  /// <summary>
  /// Unified model input table combining shuttle, company, and review data.
  /// </summary>
  /// <remarks>
  /// In-memory storage rather than Parquet — see <c>Catalog.Intermediate.cs</c> for context
  /// on the Parquet/IScalar gap.
  /// </remarks>
  public IItem<IEnumerable<ModelInputTableSchema>> ModelInputTable =>
    CreateItem(() => ItemFactory.Enumerable.Memory<ModelInputTableSchema>(label: "ModelInputTable"));
}
