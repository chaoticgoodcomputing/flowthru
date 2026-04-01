using Flowthru.Extensions.Python.Execution;
using Flowthru.Extensions.Python.Nodes;
using Flowthru.Pipelines;
using RetailDataMultipipeline.Data;

namespace RetailDataMultipipeline.Pipelines.Graphing;

/// <summary>
/// Produces three Plotly line charts (PNG) from the consolidated all-countries weekly DTU dataset.
/// Each node reads <see cref="CoreCatalog.AllCountriesWeeklyDtu"/> and plots one metric
/// (revenue, transactions, unique customers) with one trace per country.
/// </summary>
public static class GraphingPipeline
{
  public static Pipeline Create(CoreCatalog catalog, IPythonExecutor executor)
  {
    return PipelineBuilder.CreatePipeline(pipeline =>
    {
      pipeline.AddPythonNode(
        label: "PlotDollarsChart",
        description: "Line chart of weekly GBP revenue per country (Plotly PNG).",
        module: "Pipelines.Graphing.Nodes.plot_dtu_charts",
        function: "plot_dollars_chart",
        input: catalog.AllCountriesWeeklyDtu,
        output: catalog.DollarsChart,
        executor: executor
      );

      pipeline.AddPythonNode(
        label: "PlotTransactionsChart",
        description: "Line chart of weekly transaction count per country (Plotly PNG).",
        module: "Pipelines.Graphing.Nodes.plot_dtu_charts",
        function: "plot_transactions_chart",
        input: catalog.AllCountriesWeeklyDtu,
        output: catalog.TransactionsChart,
        executor: executor
      );

      pipeline.AddPythonNode(
        label: "PlotUsersChart",
        description: "Line chart of weekly unique customers per country (Plotly PNG).",
        module: "Pipelines.Graphing.Nodes.plot_dtu_charts",
        function: "plot_users_chart",
        input: catalog.AllCountriesWeeklyDtu,
        output: catalog.UsersChart,
        executor: executor
      );
    });
  }
}
