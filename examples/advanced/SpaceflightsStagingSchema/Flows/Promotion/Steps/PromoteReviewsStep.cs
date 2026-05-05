using Flowthru.Core.Steps;
using SpaceflightsStagingSchema.Data._02_Intermediate.Schemas;

namespace SpaceflightsStagingSchema.Flows.Promotion.Steps;

/// <summary>
/// Promotes staging reviews into production, filtered to those whose
/// <c>ShuttleId</c> exists in <c>production.Shuttles</c>.
/// </summary>
/// <remarks>
/// <para>
/// Promotion is <strong>not pure identity</strong>: production's FK
/// constraint <c>Reviews.ShuttleId → Shuttles.Id</c> rejects orphan rows.
/// Reviews referencing shuttles dropped during <c>PromoteShuttles</c>'s
/// FK-conformance filter would fail here too; this step trims them at the
/// promotion gate.
/// </para>
/// </remarks>
[FlowthruStep]
public static class PromoteReviewsStep
{
  public static Func<
    (
      IEnumerable<PreprocessedReviewSchema> Reviews,
      IEnumerable<PreprocessedShuttleSchema> Shuttles
    ),
    IEnumerable<PreprocessedReviewSchema>
  > Create() =>
    input =>
    {
      var validShuttleIds = new HashSet<string>(input.Shuttles.Select(s => s.Id));
      return input.Reviews.Where(r => validShuttleIds.Contains(r.ShuttleId));
    };
}
