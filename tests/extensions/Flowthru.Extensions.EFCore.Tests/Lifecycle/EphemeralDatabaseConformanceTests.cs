using Flowthru.Extensions.EFCore.Data;
using Flowthru.Tests.Kits.Effects;

namespace Flowthru.Extensions.EFCore.Tests.Lifecycle;

/// <summary>
/// Runs the kit's <see cref="EphemeralResourceConformance{TBackend, TScope}"/>
/// suite against the SQLite implementation of
/// <c>EFCoreResources.EphemeralDatabase</c>. Adds PG coverage by registering
/// an additional backend type when a Postgres-equipped backend lands.
/// </summary>
[TestFixture]
[Category("EFCore")]
public class EphemeralDatabaseConformanceTests
  : EphemeralResourceConformance<SqliteEphemeralDatabaseBackend, DbScope>;
