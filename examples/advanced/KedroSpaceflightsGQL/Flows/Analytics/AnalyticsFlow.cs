using Flowthru.Core.Flows;
using KedroSpaceflightsGQL.Data;
using KedroSpaceflightsGQL.Flows.Analytics.Steps;

namespace KedroSpaceflightsGQL.Flows.Analytics;

/// <summary>
/// Analytics pipeline demonstrating parameterized GQL catalog items.
/// </summary>
/// <remarks>
/// <para>
/// <strong>The problem this addresses:</strong> The GQL shuttle endpoint contains the full
/// fleet across all companies. Pulling it unfiltered and then post-filtering in a step wastes
/// network bandwidth and server resources — particularly when only a small subset is needed.
/// </para>
/// <para>
/// <strong>The pattern:</strong>
/// <list type="number">
/// <item>
///   <c>FindTopRatedCompany</c> is a pure transform: it reads all company records and selects
///   the highest-rated company's ID, writing it to the in-memory <c>TopRatedCompanyId</c>
///   catalog item.
/// </item>
/// <item>
///   <c>TopRatedCompanyShuttles</c> is a <em>parameterized</em> GQL catalog entry. Its adapter
///   is declared with <c>parameterSource: catalog.TopRatedCompanyId</c>. When the engine loads
///   this item, the adapter reads <c>TopRatedCompanyId</c> first and passes its value to
///   <c>GetShuttlesByCompanyId</c> — so only the relevant shuttles are fetched.
/// </item>
/// <item>
///   <c>AnalyzeTopCompanyFleet</c> is a pure transform: it receives a plain
///   <c>IEnumerable</c> and produces a report. It has no knowledge of parameterization.
/// </item>
/// </list>
/// </para>
/// <para>
/// <strong>DAG ordering:</strong> The dependency analyzer inspects
/// <c>TopRatedCompanyShuttles</c>'s adapter and discovers that it depends on
/// <c>TopRatedCompanyId</c>. It automatically adds <c>FindTopRatedCompany</c> as a
/// transitive dependency of <c>AnalyzeTopCompanyFleet</c> — no explicit ordering is declared
/// in this flow definition.
/// </para>
/// </remarks>
public static class AnalyticsFlow
{
  /// <summary>
  /// Creates the analytics pipeline.
  /// </summary>
  public static Flow Create(Catalog catalog)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      pipeline.AddStep(
        label: "FindTopRatedCompany",
        description: """
          Identifies the company with the highest rating across all GQL companies.
          GqlDatabaseSeeded is consumed as an explicit DAG gate ensuring Ingest has
          completed before this step executes.
        """,
        transform: FindTopRatedCompanyStep.Create(),
        input: (catalog.GqlDatabaseSeeded, catalog.Companies),
        output: catalog.TopRatedCompanyId
      );

      pipeline.AddStep(
        label: "AnalyzeTopCompanyFleet",
        description: """
          Computes a fleet summary for the top-rated company. The input catalog item
          (TopRatedCompanyShuttles) is parameterized: its adapter reads TopRatedCompanyId at load
          time and fires a filtered GQL query — only that company's shuttles are transferred.
          This step is a pure transform with no awareness of the filtering mechanism.
          DAG ordering (runs after FindTopRatedCompany) is enforced automatically via the
          adapter's ItemDependencies declaration.
        """,
        transform: AnalyzeTopCompanyShuttlesStep.Create(),
        input: catalog.TopRatedCompanyShuttles,
        output: catalog.TopRatedCompanyReport
      );
    });
  }
}
