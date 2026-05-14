using Flowthru.Data.Catalog;
using Flowthru.Data.Catalog.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SpaceflightsStagingSchema.Flows.DataScience.Steps;
using SpaceflightsStagingSchema.Flows.Reporting.Steps;

namespace SpaceflightsStagingSchema.Data;

/// <summary>
/// Catalog of persistent production tables in PostgreSQL's <c>public</c>
/// schema. Schema lifecycle is migration-style — created once via
/// <c>EnsureCreated</c>, persists across runs (within the Testcontainers
/// session); no <c>FlowResource</c> is declared.
///
/// Also exposes the DataScience/Reporting option records bound from
/// configuration so steps can pull them as ordinary fingerprintable inputs.
/// </summary>
/// <remarks>
/// <para>
/// All items declare <see cref="Flowthru.Extensions.EFCore.Data.DbScope.Explicit(string)"/>
/// with <see cref="StagingCatalog.SharedScope"/>. The shared scope is what
/// activates server-side <c>INSERT-FROM-SELECT</c> when a promote step
/// outputs a <c>DbQuery</c> from <see cref="StagingCatalog"/> against an
/// item here.
/// </para>
/// <para>
/// DataScience and Reporting read from this catalog and write back to it.
/// Reporting reads from <see cref="Shuttles"/> and
/// <see cref="ModelPredictions"/> only — never staging, never the model
/// input view.
/// </para>
/// </remarks>
public partial class ProductionCatalog : CatalogAbstract
{
  private readonly IDbContextFactory<ProductionDbContext> _contextFactory;
  private readonly string _basePath;
  private readonly IConfiguration _configuration;

  public ProductionCatalog(
    IDbContextFactory<ProductionDbContext> contextFactory,
    string basePath,
    IConfiguration configuration
  )
  {
    _contextFactory = contextFactory;
    _basePath = basePath;
    _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

    using var ctx = contextFactory.CreateDbContext();
    ctx.Database.EnsureCreated();
  }

  /// <summary>Train/test split options sourced from <c>Flowthru:Flows:DataScience:ModelOptions</c>.</summary>
  public IItem<SplitDataStep.ModelOptions> ModelOptions =>
    CreateItem(() =>
      Item.Of<SplitDataStep.ModelOptions>("ModelOptions")
        .FromConfiguration(_configuration)
        .AtSection("Flowthru:Flows:DataScience:ModelOptions")
        .Build());

  /// <summary>Confusion-matrix options sourced from <c>Flowthru:Flows:Reporting:ConfusionMatrixOptions</c>.</summary>
  public IItem<CreateConfusionMatrixStep.Options> ConfusionMatrixOptions =>
    CreateItem(() =>
      Item.Of<CreateConfusionMatrixStep.Options>("ConfusionMatrixOptions")
        .FromConfiguration(_configuration)
        .AtSection("Flowthru:Flows:Reporting:ConfusionMatrixOptions")
        .Build());
}
