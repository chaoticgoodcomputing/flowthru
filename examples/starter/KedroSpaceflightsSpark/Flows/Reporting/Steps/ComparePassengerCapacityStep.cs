using Flowthru.Core.Steps;
using Flowthru.Misc.DataFrames;
using KedroSpaceflightsSpark.Data._02_Intermediate.Schemas;
using KedroSpaceflightsSpark.Data._08_Reporting.Schemas;

namespace KedroSpaceflightsSpark.Flows.Reporting.Steps;

/// <summary>
/// Aggregates average passenger capacity by shuttle type using a Spark GroupBy.
///
/// The input is a TypedFrame. The step chains GroupBy.Aggregate on the distributed frame,
/// then calls ToList() to materialize the result. Materialization triggers the Spark action
/// and hydrates the collected rows into typed records.
/// </summary>
[FlowthruStep]
public static class ComparePassengerCapacityStep
{
    public static Func<
      TypedFrame<PreprocessedShuttleSchema>,
      IEnumerable<ShuttleCapacityReport>
    > Create()
    {
        return (input) =>
          input
            .GroupBy(s => s.ShuttleType)
            .Aggregate(ctx => new ShuttleCapacityReport
            {
                ShuttleType = ctx.Key,
                AvgPassengerCapacity = ctx.Avg(s => (double)s.PassengerCapacity),
            })
            .OrderBy(r => r.ShuttleType)
            .ToList();
    }
}
