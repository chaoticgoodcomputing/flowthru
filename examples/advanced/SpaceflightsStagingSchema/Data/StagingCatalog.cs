using Flowthru.Core.Data;
using Flowthru.Core.Effects;
using Flowthru.Core.Services;
using Flowthru.Extensions.EFCore.Data;
using Flowthru.Extensions.EFCore.Lifecycle;
using Flowthru.Extensions.EFCore.Validation;
using Microsoft.EntityFrameworkCore;

namespace SpaceflightsStagingSchema.Data;

/// <summary>
/// Catalog of intermediate tables in the ephemeral PostgreSQL <c>staging</c>
/// schema. The schema is provisioned in pre-flight via the catalog's
/// <see cref="Resource"/> override and dropped on flow completion.
/// </summary>
/// <remarks>
/// <para>
/// Items use <see cref="DbScope.Explicit(string)"/> with the
/// <see cref="SharedScope"/> name shared across <see cref="ProductionCatalog"/>.
/// Identical scope on both sides of the staging→production boundary unlocks
/// the framework's fused <c>INSERT-FROM-SELECT</c> path during promotion: no
/// rows materialize in C#; the database performs the JOIN and the INSERT in a
/// single server-side statement.
/// </para>
/// </remarks>
public partial class StagingCatalog : CatalogAbstract
{
  /// <summary>
  /// Shared <see cref="DbScope"/> name for staging and production items.
  /// </summary>
  public const string SharedScope = "spaceflights";

  private readonly IDbContextFactory<StagingDbContext> _contextFactory;

  public StagingCatalog(IDbContextFactory<StagingDbContext> contextFactory)
  {
    _contextFactory = contextFactory;
    InitializeCatalogProperties();
  }

  /// <summary>
  /// Pre-flight validation. Verifies that the PostgreSQL database is
  /// reachable — the schema doesn't exist yet (the resource creates it), but
  /// the host database does, so a real connection check is appropriate here.
  /// </summary>
  public override FlowValidation Validate(FlowExecutionContext ctx) =>
    DbValidations.CanConnect(_contextFactory);

  /// <summary>
  /// Ephemeral resource: the <c>staging</c> schema and its tables. Acquire
  /// drops any leftover schema and recreates it with tables generated from
  /// <see cref="StagingDbContext"/>'s model. Release drops the schema, unless
  /// <c>PreserveOnFailure</c> is set and the flow body threw.
  /// </summary>
  public override FlowResource<DbScope> Resource =>
    EFCoreResources.EphemeralSchema(_contextFactory, StagingDbContext.SchemaName, o =>
    {
      o.PreserveOnFailure = true;
    });
}
