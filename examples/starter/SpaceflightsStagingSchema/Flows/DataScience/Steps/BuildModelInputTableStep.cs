using Flowthru.Core.Steps;
using Flowthru.Extensions.EFCore.Data;
using SpaceflightsStagingSchema.Data;
using SpaceflightsStagingSchema.Data._02_Intermediate.Schemas;
using SpaceflightsStagingSchema.Data._03_Primary.Schemas;

namespace SpaceflightsStagingSchema.Flows.DataScience.Steps;

/// <summary>
/// Composes the model input table as a deferred SQL join over the three
/// FK-constrained production source tables.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Why this is a step, not a catalog property.</strong> The decision
/// "to make the model input table, you join shuttles, companies, and reviews
/// on these keys" is transformation logic. Catalogs publish raw data handles;
/// the join belongs in a step. The catalog exposes <c>Companies</c>,
/// <c>Shuttles</c>, and <c>Reviews</c> as deferred queryables; this step
/// composes them into a single derived query.
/// </para>
/// <para>
/// <strong>How the composition works.</strong> The three input
/// <see cref="IEnumerable{T}"/> values are each backed at runtime by a
/// <see cref="DbQuery{T}"/>. The step casts one of them to access
/// <see cref="DbQuery{T}.Project{TResult}"/>, which builds a new deferred
/// query of <see cref="ModelInputTableSchema"/> on the same scope. The
/// projection lambda references <c>ctx.Set&lt;T&gt;()</c> for all three
/// source tables — that's how IQueryable joins are expressed; you cannot
/// imperatively combine queryables, only describe a single LINQ expression
/// against the shared <see cref="Microsoft.EntityFrameworkCore.DbSet{TEntity}"/>s.
/// </para>
/// <para>
/// <strong>Why the unused inputs matter.</strong> <c>companies</c> and
/// <c>reviews</c> appear unused in the lambda body but are essential as
/// <em>DAG dependency markers</em>: the framework will not run this step
/// until all three production tables have been promoted. Removing them from
/// the input tuple would let the step run before promotion completes, and
/// the SQL join would fail at materialization time instead of failing fast
/// in pre-flight.
/// </para>
/// <para>
/// <strong>No materialization.</strong> The returned
/// <see cref="DbQuery{T}"/> is an <see cref="IEnumerable{T}"/>, but it
/// doesn't pull rows. The downstream <c>SplitDataStep</c> iterates it,
/// which triggers the SQL join via
/// <see cref="DbQuery{T}.GetEnumerator"/>. Nothing is stored to disk.
/// </para>
/// </remarks>
[FlowthruStep]
public static class BuildModelInputTableStep
{
  public static Func<
    (
      IEnumerable<PreprocessedShuttleSchema> Shuttles,
      IEnumerable<PreprocessedCompanySchema> Companies,
      IEnumerable<PreprocessedReviewSchema> Reviews
    ),
    IEnumerable<ModelInputTableSchema>
  > Create()
  {
    return (input) =>
    {
      // Cast one input to DbQuery to access .Project<>. Any of the three works
      // — they share the same DbScope, so any handle yields the same projected
      // scope. Using shuttles is arbitrary.
      var shuttlesQuery = (DbQuery<PreprocessedShuttleSchema>)input.Shuttles;

      // Compose the join as a single LINQ expression against the production
      // DbContext's DbSets. EF translates this to a SQL JOIN at materialization.
      return shuttlesQuery.Project<ModelInputTableSchema>(ctx =>
        from s in ctx.Set<PreprocessedShuttleSchema>()
        join c in ctx.Set<PreprocessedCompanySchema>() on s.CompanyId equals c.Id
        join r in ctx.Set<PreprocessedReviewSchema>() on s.Id equals r.ShuttleId
        orderby s.Id
        select new ModelInputTableSchema
        {
          ShuttleId = s.Id,
          ShuttleType = s.ShuttleType,
          CompanyId = s.CompanyId,
          Engines = s.Engines,
          PassengerCapacity = s.PassengerCapacity,
          Crew = s.Crew,
          DCheckComplete = s.DCheckComplete,
          MoonClearanceComplete = s.MoonClearanceComplete,
          Price = s.Price,
          IataApproved = c.IataApproved,
          CompanyRating = c.CompanyRating,
          ReviewScoresRating = r.ReviewScoresRating,
        }
      );
    };
  }
}
