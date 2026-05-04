using Flowthru.Core.Data;
using Plotly.NET;
using SpaceflightsStagingSchema.Data._08_Reporting.Schemas;

namespace SpaceflightsStagingSchema.Data;

public partial class ProductionCatalog
{
  private readonly string _basePath = Path.Combine(Directory.GetCurrentDirectory(), "Data");

  /// <summary>Passenger capacity report grouped by shuttle type (filesystem JSON).</summary>
  public IItem<IEnumerable<ShuttleCapacityReport>> ShuttleCapacityReport =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Json<ShuttleCapacityReport>(
          label: "ShuttleCapacityReport",
          filePath: $"{_basePath}/_08_Reporting/Datasets/shuttle_capacity_report.json"
        )
    );

  /// <summary>Confusion matrix heatmap (in-memory chart object).</summary>
  public IItem<GenericChart> ConfusionMatrixChart =>
    CreateItem(() => ItemFactory.Single.Memory<GenericChart>(label: "ConfusionMatrixChart"));

  /// <summary>Passenger capacity bar chart (in-memory chart object).</summary>
  public IItem<GenericChart> ShuttlePassengerCapacityChart =>
    CreateItem(
      () => ItemFactory.Single.Memory<GenericChart>(label: "ShuttlePassengerCapacityChart")
    );
}
