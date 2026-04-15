using Flowthru.Core.Data;
using Flowthru.Misc.DataFrames;
using KedroSpaceflightsSpark.Data._08_Reporting.Schemas;
using Plotly.NET;
using CoreFactory = Flowthru.Core.Data.ItemFactory;
using SparkFactory = Flowthru.Extensions.Spark.ItemFactory;

namespace KedroSpaceflightsSpark.Data;

public partial class Catalog
{
    public IItem<IEnumerable<ShuttleCapacityReport>> ShuttleCapacityReport =>
      CreateItem(
        () =>
          CoreFactory.Enumerable.Json<ShuttleCapacityReport>(
            label: "ShuttleCapacityReport",
            filePath: $"{_basePath}/_08_Reporting/Datasets/shuttle_capacity_report.json"
          )
      );

    public IItem<GenericChart> ShuttlePassengerCapacityChart =>
      CreateItem(
        () => CoreFactory.Single.Memory<GenericChart>(label: "ShuttlePassengerCapacityChart")
      );

    public IItem<byte[]> ShuttlePassengerCapacityPlotPng =>
      CreateItem(
        () =>
          CoreFactory.Single.Binary(
            label: "ShuttlePassengerCapacityPlotPng",
            filePath: $"{_basePath}/_08_Reporting/Images/shuttle_passenger_capacity_plot.png"
          )
      );

    public IItem<GenericChart> ConfusionMatrixChart =>
      CreateItem(() => CoreFactory.Single.Memory<GenericChart>(label: "ConfusionMatrixChart"));

    public IItem<byte[]> ConfusionMatrixPlotPng =>
      CreateItem(
        () =>
          CoreFactory.Single.Binary(
            label: "ConfusionMatrixPlotPng",
            filePath: $"{_basePath}/_08_Reporting/Images/confusion_matrix_plot.png"
          )
      );

    public IItem<TypedFrame<ShuttlePriceRankSchema>> ShuttlePriceRanks =>
      CreateItem(() => SparkFactory.Frame.Memory<ShuttlePriceRankSchema>(label: "ShuttlePriceRanks"));
}
