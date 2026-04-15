using Flowthru.Core.Data;
using Flowthru.Extensions.Spark;
using Flowthru.Misc.DataFrames;
using KedroSpaceflightsSpark.Data._02_Intermediate.Schemas;
using SparkFactory = Flowthru.Extensions.Spark.ItemFactory;

namespace KedroSpaceflightsSpark.Data;

/// <summary>
/// Intermediate data layer: preprocessed typed datasets held as deferred Spark execution plans.
/// All items are in-memory TypedFrames — no file persistence at this layer.
/// </summary>
public partial class Catalog
{
  public IItem<TypedFrame<PreprocessedCompanySchema>> PreprocessedCompanies =>
    CreateItem(
      () => SparkFactory.Frame.Memory<PreprocessedCompanySchema>(label: "PreprocessedCompanies")
    );

  public IItem<TypedFrame<PreprocessedShuttleSchema>> PreprocessedShuttles =>
    CreateItem(
      () => SparkFactory.Frame.Memory<PreprocessedShuttleSchema>(label: "PreprocessedShuttles")
    );

  public IItem<TypedFrame<ParsedReviewSchema>> ParsedReviews =>
    CreateItem(() => SparkFactory.Frame.Memory<ParsedReviewSchema>(label: "ParsedReviews"));
}
