using Flowthru.Data.Storage.EFCore;
using Flowthru.Extensions.EFCore.Tests.Backends;
using Flowthru.Tests.Kits.Prelude;

namespace Flowthru.Extensions.EFCore.Tests.Lifecycle;

/// <summary>
/// Runs <see cref="EphemeralResourceLaws{TBackend, TScope}"/> against
/// every <see cref="IEphemeralResourceBackend{TScope}"/> implementation
/// for the EF Core extension. One fixture per backend type via
/// <c>[TestFixture(typeof(...))]</c>; both fixtures run by default —
/// the Docker-dependent backend gates itself via
/// <see cref="TestCapabilities.Docker"/> and reports Inconclusive when
/// the daemon is absent, so no <c>--filter</c> flag is required for the
/// default test flow to work on Docker-less environments.
/// </summary>
/// <remarks>
/// This is the canonical reference for the backend matrix pattern —
/// other extensions targeting multiple real providers (HTTP, brokers,
/// alternative databases) can model their laws subclass on this shape.
/// </remarks>
/// <typeparam name="TBackend">
/// Backend under test. Bound by NUnit via the
/// <c>[TestFixture(typeof(TBackend))]</c> attributes on this class.
/// </typeparam>
[TestFixture(typeof(SqliteFileBackend))]
[TestFixture(typeof(PostgresContainerBackend))]
[Category("EFCore")]
[Category("Laws")]
public class EFCoreResourceLaws<TBackend>
  : EphemeralResourceLaws<TBackend, BackendScope<DbScope>>
  where TBackend : IEphemeralResourceBackend<BackendScope<DbScope>>, new()
{ }
