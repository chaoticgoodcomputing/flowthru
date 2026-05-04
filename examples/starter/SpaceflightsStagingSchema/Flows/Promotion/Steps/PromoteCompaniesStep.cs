using Flowthru.Core.Steps;
using SpaceflightsStagingSchema.Data._02_Intermediate.Schemas;

namespace SpaceflightsStagingSchema.Flows.Promotion.Steps;

/// <summary>
/// Promotes staging companies into production. The transform is identity —
/// the meaningful work is the cross-catalog write, which goes through
/// production's PK constraint (Companies.Id is unique).
/// </summary>
[FlowthruStep]
public static class PromoteCompaniesStep
{
  public static Func<IEnumerable<PreprocessedCompanySchema>, IEnumerable<PreprocessedCompanySchema>> Create() =>
    rows => rows;
}
