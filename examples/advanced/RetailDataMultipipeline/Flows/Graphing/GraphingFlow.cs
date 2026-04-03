using Flowthru.Extensions.Python.Execution;
using Flowthru.Extensions.Python.Steps;
using Flowthru.Flows;
using RetailDataMultipipeline.Data;

namespace RetailDataMultipipeline.Flows.Graphing;

/// <summary>
/// Produces three Plotly line charts (PNG) from the consolidated all-countries weekly DTU dataset.
/// Each node reads <see cref="CoreCatalog.AllCountriesWeeklyDtu"/> and plots one metric
/// (revenue, transactions, unique customers) with one trace per country.
/// </summary>
public static class GraphingFlow
{
  public static Flow Create(CoreCatalog catalog, IPythonExecutor executor)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      pipeline.AddPythonStep(
        label: "PlotDollarsChart",
        description: "Line chart of weekly GBP revenue per country (Plotly PNG).",
        module: "Flows.Graphing.Steps.plot_dtu_charts",
        function: "plot_dollars_chart",
        input: catalog.AllCountriesWeeklyDtu,
        output: catalog.DollarsChart,
        executor: executor
      );

      pipeline.AddPythonStep(
        label: "PlotTransactionsChart",
        description: "Line chart of weekly transaction count per country (Plotly PNG).",
        module: "Flows.Graphing.Steps.plot_dtu_charts",
        function: "plot_transactions_chart",
        input: catalog.AllCountriesWeeklyDtu,
        output: catalog.TransactionsChart,
        executor: executor
      );

      pipeline.AddPythonStep(
        label: "PlotUsersChart",
        description: "Line chart of weekly unique customers per country (Plotly PNG).",
        module: "Flows.Graphing.Steps.plot_dtu_charts",
        function: "plot_users_chart",
        input: catalog.AllCountriesWeeklyDtu,
        output: catalog.UsersChart,
        executor: executor
      );
    });
  }
}
