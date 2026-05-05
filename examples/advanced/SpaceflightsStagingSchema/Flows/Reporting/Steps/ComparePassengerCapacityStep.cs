using Flowthru.Core.Steps;
using Flowthru.Extensions.EFCore.Data;
using SpaceflightsStagingSchema.Data._02_Intermediate.Schemas;
using SpaceflightsStagingSchema.Data._08_Reporting.Schemas;

namespace SpaceflightsStagingSchema.Flows.Reporting.Steps;

/// <summary>
/// Aggregates passenger capacity by shuttle type from the production shuttle
/// table — the canonical source for shuttle metadata. Includes shuttles that
/// were never reviewed (and therefore aren't in the model input view).
/// </summary>
/// <remarks>
/// <para>
/// <strong>Server-side aggregation.</strong> The step casts its input to
/// <see cref="DbQuery{T}"/> and uses <see cref="DbQuery{T}.Project{TResult}"/>
/// to compose a SQL <c>GROUP BY</c> directly on the PostgreSQL server. The
/// returned <see cref="DbQuery{T}"/> only fires when iterated by the JSON
/// storage adapter — at which point one row per shuttle type comes back
/// over the wire, regardless of how many shuttles are in production.
/// </para>
/// <para>
/// Compare to the C# <c>GroupBy</c> equivalent: that path would materialize
/// every shuttle row in C# memory, then group. For multi-GB shuttle tables,
/// only the SQL-side path is viable.
/// </para>
/// </remarks>
[FlowthruStep]
public static class ComparePassengerCapacityStep
{
  public static Func<
    IEnumerable<PreprocessedShuttleSchema>,
    IEnumerable<ShuttleCapacityReport>
  > Create()
  {
    return (input) =>
    {
      var query = (DbQuery<PreprocessedShuttleSchema>)input;
      return query.Project<ShuttleCapacityReport>(ctx =>
        ctx.Set<PreprocessedShuttleSchema>()
          .GroupBy(s => s.ShuttleType)
          .Select(g => new ShuttleCapacityReport
          {
            ShuttleType = g.Key,
            AvgPassengerCapacity = g.Average(s => (decimal)s.PassengerCapacity),
          })
      );
    };
  }
}
