namespace Flowthru.Validation.Runtime.EFCore;

/// <summary>
/// Conflict identity of the database an EF Core catalog item reads from
/// or writes to (ADR-0019). Surfaced through Core's
/// <see cref="ServiceDependency.External"/> variant so the
/// <c>ParallelFlowScheduler</c> can gate concurrent steps that contend on
/// the same database: a step's input items contribute a
/// <see cref="ConflictOp.Read"/> key, its output items a
/// <see cref="ConflictOp.Write"/> key.
/// </summary>
/// <remarks>
/// <para>
/// The adapter constructs this (never the catalog author), keying
/// <see cref="Identity"/> on the physical database — provider + data
/// source + database name — so two items on the same SQLite file serialize
/// their writes even when wired from separate factories. The capacities
/// are carried on the dependency itself because the resolving
/// <c>EFCoreDatabaseProfileContributor</c> sees only the dependency, not
/// the originating item.
/// </para>
/// </remarks>
internal sealed record EFCoreDatabaseDependency(
  string Identity,
  string Display,
  int WriteCapacity,
  int ReadCapacity
) : IExtensionServiceDependency, ICapacityConstrainable
{
  /// <inheritdoc/>
  public string DagId => $"efcore:{Identity}";

  /// <inheritdoc/>
  public string DisplayName => $"db:{Display}";

  /// <inheritdoc/>
  public string Category => "efcore";

  /// <inheritdoc/>
  public IExtensionServiceDependency ClampTo(int writeCapacity, int readCapacity) =>
    this with
    {
      WriteCapacity = Math.Min(WriteCapacity, writeCapacity),
      ReadCapacity = Math.Min(ReadCapacity, readCapacity),
    };
}
