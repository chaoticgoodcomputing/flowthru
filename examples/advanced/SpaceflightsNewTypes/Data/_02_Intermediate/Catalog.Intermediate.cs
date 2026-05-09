using Flowthru.Data.Catalog;
using SpaceflightsNewTypes.Data._02_Intermediate.Schemas;

namespace SpaceflightsNewTypes.Data;

public partial class Catalog
{
  // ─────────────────────────────────────────────────────────────────────────
  // NOTE: Intermediate storage is held in memory rather than Parquet for this
  // example. The Parquet serializer's CLR-to-DataField mapping does not yet
  // unwrap IScalar NewTypes (a pre-existing gap, tracked separately) — writing
  // a `ShuttleId`-typed column to Parquet silently drops it. The starter
  // KedroSpaceflights example uses Parquet here because it stores raw `string`s.
  // Once the Parquet IScalar gap is closed, these can be reverted to
  // `Item.Of<...>(...).Parquet()` without changing the schemas.
  // ─────────────────────────────────────────────────────────────────────────

  /// <summary>Preprocessed company data with validated and strongly-typed fields.</summary>
  public IItem<IEnumerable<PreprocessedCompanySchema>> PreprocessedCompanies =>
    CreateItem(() => Item.Of<IEnumerable<PreprocessedCompanySchema>>("PreprocessedCompanies")
      .Memory()
      .Build());

  /// <summary>Preprocessed shuttle data with validated and strongly-typed fields.</summary>
  public IItem<IEnumerable<PreprocessedShuttleSchema>> PreprocessedShuttles =>
    CreateItem(() => Item.Of<IEnumerable<PreprocessedShuttleSchema>>("PreprocessedShuttles")
      .Memory()
      .Build());
}
