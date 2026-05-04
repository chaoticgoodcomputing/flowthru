using Flowthru.Core.Data;
using Flowthru.Core.Effects;
using Flowthru.Core.Services;
using Flowthru.Core.Validation;
using Flowthru.Extensions.EFCore.Data;
using Flowthru.Extensions.EFCore.Lifecycle;
using Flowthru.Extensions.EFCore.Validation;
using Microsoft.EntityFrameworkCore;

namespace SpaceflightsStagingSchema.Data;

/// <summary>
/// Catalog of intermediate and primary tables backed by an ephemeral SQLite
/// database. The database file is provisioned in pre-flight via the catalog's
/// <see cref="Resource"/> override and dropped on flow completion.
/// </summary>
/// <remarks>
/// <para>
/// This catalog exercises two new <see cref="CatalogAbstract"/> overrides:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///       <see cref="Validate"/> — applicative pre-flight check. Connection
///       reachability and parent-directory writability are verified together;
///       both errors are surfaced at once if both fail.
///     </description>
///   </item>
///   <item>
///     <description>
///       <see cref="Resource"/> — monadic acquire/release pair. The framework
///       composes this with other catalog resources via <c>Bind</c> and runs
///       the entire flow inside <c>FlowResource.Use(...)</c>, guaranteeing
///       LIFO unwind on success or failure.
///     </description>
///   </item>
/// </list>
/// </remarks>
public partial class StagingCatalog : CatalogAbstract
{
  private readonly IDbContextFactory<StagingDbContext> _contextFactory;
  private readonly string _dbPath;

  public StagingCatalog(string basePath, IDbContextFactory<StagingDbContext> contextFactory)
  {
    _contextFactory = contextFactory;
    _dbPath = Path.Combine(basePath, "staging.db");
    InitializeCatalogProperties();
  }

  /// <summary>
  /// Pre-flight validation. Runs in the applicative phase — multiple failures
  /// across catalogs accumulate and are reported together.
  /// </summary>
  public override FlowValidation Validate(FlowExecutionContext ctx) =>
    FlowValidation.Combine(
      // IsConfigured (not CanConnect) because staging.db doesn't exist yet —
      // it's created by the Resource's acquire effect, which runs *after*
      // Validate. CanConnect would produce a false negative on SQLite for
      // a file that doesn't yet exist.
      DbValidations.IsConfigured(_contextFactory),
      FsValidations.IsWritable(Path.GetDirectoryName(_dbPath)!)
    );

  /// <summary>
  /// Ephemeral resource: the staging SQLite database. Provisioned after
  /// validation succeeds and before external-input inspection; dropped on
  /// unwind. <c>PreserveOnFailure</c> keeps the database for debugging when
  /// a flow fails.
  /// </summary>
  public override FlowResource<DbScope> Resource =>
    EFCoreResources.EphemeralDatabase(_contextFactory, _dbPath, o =>
    {
      o.PreserveOnFailure = true;
    });
}
