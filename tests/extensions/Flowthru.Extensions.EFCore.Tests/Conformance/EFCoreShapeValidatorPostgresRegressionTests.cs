using Flowthru.Core.Data.Storage;
using Flowthru.Extensions.EFCore.Tests.Backends;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Extensions.EFCore.Tests.Conformance;

/// <summary>
/// Pinned-to-Postgres regression test for commit
/// <c>0cb460d9ef05ce13aadb0726fa10c8de4d850b0a</c>: <c>EFCoreShapeValidator</c> dropped
/// <c>CommandBehavior.KeyInfo</c> when running its NOT-NULL-column probe, which made
/// Npgsql return <c>DBNull</c> for every row's <c>AllowDBNull</c> column. The lenient
/// "unknown → assume nullable" default then false-flagged every NOT NULL entity property
/// against Postgres while leaving SQLite tests green.
/// </summary>
/// <remarks>
/// <para>
/// The conformance suite proper would have caught this — adapter construction calls
/// <c>ValidateEntityConfiguration</c>, which exercises the validator — but only once the
/// suite started running against Postgres. This standalone test makes the original bug
/// traceable in the commit history regardless of whether the conformance suite changes.
/// </para>
/// <para>
/// Tagged <c>Integration</c>: requires a Postgres container.
/// </para>
/// </remarks>
[TestFixture]
[Category("Integration")]
public class EFCoreShapeValidatorPostgresRegressionTests
{
  private PostgresContainerBackend _backend = null!;
  private DbContextOptions<TestDbContext> _options = null!;

  [OneTimeSetUp]
  public async Task SetUp()
  {
    _backend = new PostgresContainerBackend();
    _options = await _backend.StartAsync();
    await using var context = new TestDbContext(_options);
    await context.Database.EnsureCreatedAsync();
  }

  [OneTimeTearDown]
  public async Task TearDown()
  {
    await _backend.DisposeAsync();
  }

  [Test]
  public async Task ShapeValidator_AcceptsNotNullColumns_AgainstPostgres()
  {
    // TestEntity has Name as a non-nullable string. Pre-fix, Npgsql's GetSchemaTable returned
    // DBNull for AllowDBNull on every column without CommandBehavior.KeyInfo, which combined
    // with the validator's old "unknown → assume nullable" default to flag Name as a
    // nullability mismatch and refuse to construct the adapter.
    using var context = new TestDbContext(_options);
    context.TestEntities.Add(new TestEntity { Id = 1, Name = "Alice" });
    await context.SaveChangesAsync();

    // Construction calls ValidateEntityConfiguration → EFCoreShapeValidator.GetSchemaInfo.
    // If the bug regresses (KeyInfo dropped or "unknown → assume nullable" restored),
    // construction throws ArgumentException with a nullability-mismatch message.
    Assert.DoesNotThrow(
      () =>
      {
        _ = new EFCoreStorageAdapter<TestEntity>(() => new TestDbContext(_options));
      },
      "EFCoreShapeValidator should accept TestEntity's non-nullable Name column against Postgres. "
        + "If this throws, either CommandBehavior.KeyInfo was removed from the schema-probe call "
        + "or the ParseAllowDbNull default reverted to assume-nullable. See commit 0cb460d9."
    );
  }
}
