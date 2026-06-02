using Flowthru.Validation.Runtime;
using Flowthru.Validation.Runtime.EFCore;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Data.Storage.EFCore.Internal;

/// <summary>
/// Derives the scheduler conflict profile for the database a context
/// targets (ADR-0019). The conflict <em>identity</em> is the physical
/// database (provider + data source + database name), so two catalog
/// items that point at the same SQLite file serialize their writes even
/// when wired from separate factories. The per-operation <em>capacity</em>
/// is read from the provider: SQLite is single-writer (write capacity 1),
/// every other provider is left unbounded so a pooled server isn't
/// needlessly serialized — its own connection pool bounds it.
/// </summary>
internal static class EFCoreConflictProfile
{
  /// <summary>
  /// Probe <paramref name="context"/> for its conflict identity and
  /// provider-derived capacities. Reads configured metadata only — no
  /// connection is opened, so this is safe to call at adapter
  /// construction (where a context already exists for entity validation).
  /// </summary>
  public static EFCoreConflictInfo Probe(DbContext context)
  {
    string providerName;
    try { providerName = context.Database.ProviderName ?? "unknown"; }
    catch { providerName = "unknown"; }

    var isSqlite = providerName.Contains("Sqlite", StringComparison.OrdinalIgnoreCase);

    string identity;
    string display;
    try
    {
      // DataSource / Database identify the physical database without
      // exposing credentials (those live in the connection string's
      // User Id / Password, which we never read).
      var conn = context.Database.GetDbConnection();
      var dataSource = conn.DataSource ?? string.Empty;
      var database = conn.Database ?? string.Empty;
      display = (dataSource, database) switch
      {
        ("", "") => context.GetType().Name,
        ("", var d) => d,
        (var ds, "") => ds,
        var (ds, d) => $"{ds}/{d}",
      };
      identity = $"{providerName}|{display}";
    }
    catch
    {
      // Non-relational provider (e.g. EF Core InMemory) — no shared
      // connection to contend on. Key by provider + context type; the
      // capacity stays unbounded, so this never actually gates.
      display = context.GetType().Name;
      identity = $"{providerName}|{display}";
    }

    var writeCapacity = isSqlite ? 1 : int.MaxValue;
    const int readCapacity = int.MaxValue;

    var dependency = new ServiceDependency.External(
      new EFCoreDatabaseDependency(identity, display, writeCapacity, readCapacity));
    return new EFCoreConflictInfo(dependency, writeCapacity, readCapacity);
  }
}

/// <summary>The probe result: the conflict dependency plus the capacities to stamp onto <see cref="StorageTraits"/>.</summary>
internal readonly record struct EFCoreConflictInfo(
  ServiceDependency Dependency,
  int WriteCapacity,
  int ReadCapacity
);
