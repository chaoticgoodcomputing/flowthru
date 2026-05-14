using Flowthru.Data.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SpaceflightsHybridCatalog.Data._02_Intermediate.Schemas;
using SpaceflightsHybridCatalog.Data._03_Primary.Schemas;
using SpaceflightsHybridCatalog.Data._05_ModelInput.Schemas;
using SpaceflightsHybridCatalog.Data._06_Models.Schemas;
using SpaceflightsHybridCatalog.Data._07_ModelOutput.Schemas;

namespace SpaceflightsHybridCatalog.Data;

/// <summary>
/// Production-mode catalog: every intermediate item is persisted to SQLite via
/// EFCore. Suited to deployments where pipeline outputs need transactional
/// semantics, concurrent reader/writer access, or downstream querying.
/// </summary>
/// <remarks>
/// Schema creation is the host's responsibility — see Program.cs, which runs
/// <see cref="DatabaseFacade.EnsureCreated"/> at startup before the Flowthru
/// pre-flight hooks fire. The catalog itself is a pure projection over an
/// existing context factory.
/// </remarks>
public sealed class ProductionCatalog : Catalog
{
  private readonly IDbContextFactory<SpaceflightsDbContext> _contextFactory;

  public ProductionCatalog(
    string basePath,
    IDbContextFactory<SpaceflightsDbContext> contextFactory,
    IConfiguration configuration
  )
    : base(basePath, configuration)
  {
    _contextFactory = contextFactory;
  }

  public override IItem<IEnumerable<PreprocessedCompanySchema>> PreprocessedCompanies =>
    CreateItem(() => Item.Of<IEnumerable<PreprocessedCompanySchema>>("PreprocessedCompanies")
      .EFCoreQuery<PreprocessedCompanySchema, SpaceflightsDbContext>()
      .WithContextFactory(_contextFactory)
      .Build());

  public override IItem<IEnumerable<PreprocessedShuttleSchema>> PreprocessedShuttles =>
    CreateItem(() => Item.Of<IEnumerable<PreprocessedShuttleSchema>>("PreprocessedShuttles")
      .EFCoreQuery<PreprocessedShuttleSchema, SpaceflightsDbContext>()
      .WithContextFactory(_contextFactory)
      .Build());

  public override IItem<IEnumerable<ModelInputTableSchema>> ModelInputTable =>
    CreateItem(() => Item.Of<IEnumerable<ModelInputTableSchema>>("ModelInputTable")
      .EFCoreTable<ModelInputTableSchema, SpaceflightsDbContext>()
      .WithContextFactory(_contextFactory)
      .WithQuery(q => q.OrderBy(r => r.ShuttleId))
      .Build());

  public override IItem<IEnumerable<TrainingData>> TrainSplit =>
    CreateItem(() => Item.Of<IEnumerable<TrainingData>>("XTrain")
      .EFCoreTable<TrainingData, SpaceflightsDbContext>()
      .WithContextFactory(_contextFactory)
      .Build());

  public override IItem<IEnumerable<TestData>> TestSplit =>
    CreateItem(() => Item.Of<IEnumerable<TestData>>("XTest")
      .EFCoreTable<TestData, SpaceflightsDbContext>()
      .WithContextFactory(_contextFactory)
      .Build());

  public override IItem<LinearRegressionModel> Regressor =>
    CreateItem(() => Item.Of<LinearRegressionModel>("Regressor")
      .EFCoreEntity<LinearRegressionModel, SpaceflightsDbContext>()
      .WithContextFactory(_contextFactory)
      .Build());

  public override IItem<ModelMetrics> ModelMetrics =>
    CreateItem(() => Item.Of<ModelMetrics>("ModelMetrics")
      .EFCoreEntity<ModelMetrics, SpaceflightsDbContext>()
      .WithContextFactory(_contextFactory)
      .Build());

  public override IItem<IEnumerable<ModelPredictions>> ModelPredictions =>
    CreateItem(() => Item.Of<IEnumerable<ModelPredictions>>("ModelPredictions")
      .EFCoreTable<ModelPredictions, SpaceflightsDbContext>()
      .WithContextFactory(_contextFactory)
      .Build());
}
