using Flowthru.Step;
using SpaceflightsStagingSchema.Data._02_Intermediate.Schemas;

namespace SpaceflightsStagingSchema.Flows.Promotion.Steps;

/// <summary>
/// Promotes staging shuttles into production, filtered to those whose
/// <c>CompanyId</c> exists in <c>production.Companies</c>.
/// </summary>
/// <remarks>
/// <para>
/// Promotion is <strong>not pure identity</strong>: production's FK
/// constraint <c>Shuttles.CompanyId → Companies.Id</c> rejects orphan rows.
/// This step does the FK-conformance filter explicitly so the SQL insert
/// succeeds — staging is the unconstrained scratchpad, production is the
/// integrity-enforced zone, and this transformation is the gate between
/// the two.
/// </para>
/// <para>
/// Taking <c>production.Companies</c> as input both threads the Companies
/// rows for the filter and declares the DAG dependency that forces
/// <c>PromoteCompanies</c> to complete first.
/// </para>
/// </remarks>
[FlowthruStep]
public static class PromoteShuttlesStep
{
  public static Func<
    (
      IEnumerable<PreprocessedShuttleSchema> Shuttles,
      IEnumerable<PreprocessedCompanySchema> Companies
    ),
    IEnumerable<PreprocessedShuttleSchema>
  > Create() =>
    input =>
    {
      var validCompanyIds = new HashSet<string>(input.Companies.Select(c => c.Id));
      return input.Shuttles.Where(s => validCompanyIds.Contains(s.CompanyId));
    };
}
