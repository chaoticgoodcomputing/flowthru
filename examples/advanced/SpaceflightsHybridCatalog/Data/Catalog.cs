using Flowthru.Data.Catalog;
using SpaceflightsHybridCatalog.Data._02_Intermediate.Schemas;
using SpaceflightsHybridCatalog.Data._03_Primary.Schemas;
using SpaceflightsHybridCatalog.Data._05_ModelInput.Schemas;
using SpaceflightsHybridCatalog.Data._06_Models.Schemas;
using SpaceflightsHybridCatalog.Data._07_ModelOutput.Schemas;

namespace SpaceflightsHybridCatalog.Data;

/// <summary>
/// Abstract catalog declaring every dataset the Spaceflights pipeline needs.
/// Shared items (raw inputs from filesystem, reporting outputs, in-memory
/// charts) live on the base class as concrete properties; items that differ
/// between backends are declared <c>abstract</c> and implemented by
/// <see cref="DevelopmentCatalog"/> (flat files) and
/// <see cref="ProductionCatalog"/> (EFCore tables).
/// </summary>
/// <remarks>
/// <para>
/// Flows take the abstract <see cref="Catalog"/> as a parameter, so swapping
/// the backing store is invisible to flow factories and steps. The runtime
/// resolves whichever subclass <c>Program.ConfigureServices</c> registered for
/// the current <c>ASPNETCORE_ENVIRONMENT</c>.
/// </para>
/// </remarks>
public abstract partial class Catalog : CatalogAbstract
{
  protected readonly string _basePath;

  protected Catalog(string basePath)
  {
    _basePath = basePath;
  }

  // ── Divergent items: each backend supplies its own implementation ─────

  public abstract IItem<IEnumerable<PreprocessedCompanySchema>> PreprocessedCompanies { get; }
  public abstract IItem<IEnumerable<PreprocessedShuttleSchema>> PreprocessedShuttles { get; }

  public abstract IItem<IEnumerable<ModelInputTableSchema>> ModelInputTable { get; }

  public abstract IItem<IEnumerable<TrainingData>> TrainSplit { get; }
  public abstract IItem<IEnumerable<TestData>> TestSplit { get; }

  public abstract IItem<LinearRegressionModel> Regressor { get; }

  public abstract IItem<ModelMetrics> ModelMetrics { get; }
  public abstract IItem<IEnumerable<ModelPredictions>> ModelPredictions { get; }
}
