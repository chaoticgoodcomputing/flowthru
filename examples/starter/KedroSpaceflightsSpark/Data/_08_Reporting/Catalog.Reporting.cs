using Flowthru.Core.Data;
using KedroSpaceflightsSpark.Data._08_Reporting.Schemas;
using Plotly.NET;

namespace KedroSpaceflightsSpark.Data;

public partial class Catalog
{
  public IItem<IEnumerable<ShuttleCapacityReport>> ShuttleCapacityReport =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Json<ShuttleCapacityReport>(
          label: "ShuttleCapacityReport",
          filePath: $"{_basePath}/_08_Reporting/Datasets/shuttle_capacity_report.json"
        )
    );

  public IItem<GenericChart> ShuttlePassengerCapacityChart =>
    CreateItem(
      () => ItemFactory.Single.Memory<GenericChart>(label: "ShuttlePassengerCapacityChart")
    );

  public IItem<byte[]> ShuttlePassengerCapacityPlotPng =>
    CreateItem(
      () =>
        ItemFactory.Single.Binary(
          label: "ShuttlePassengerCapacityPlotPng",
          filePath: $"{_basePath}/_08_Reporting/Images/shuttle_passenger_capacity_plot.png"
        )
    );

  public IItem<GenericChart> ConfusionMatrixChart =>
    CreateItem(() => ItemFactory.Single.Memory<GenericChart>(label: "ConfusionMatrixChart"));

  public IItem<byte[]> ConfusionMatrixPlotPng =>
    CreateItem(
      () =>
        ItemFactory.Single.Binary(
          label: "ConfusionMatrixPlotPng",
          filePath: $"{_basePath}/_08_Reporting/Images/confusion_matrix_plot.png"
        )
    );
}
