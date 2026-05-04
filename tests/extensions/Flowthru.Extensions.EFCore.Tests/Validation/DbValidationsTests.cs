using Flowthru.Extensions.EFCore.Validation;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Extensions.EFCore.Tests.Validation;

/// <summary>
/// Tests for <see cref="DbValidations.CanConnect{TContext}"/>. SQLite's
/// <c>CanConnect</c> returns true for any parseable connection string (even
/// pointing at a not-yet-created file), so these tests verify the surrounding
/// behaviour: malformed configurations are caught, valid factories pass.
/// </summary>
[TestFixture]
[Category("EFCore")]
public class DbValidationsTests
{
  [Test]
  public void CanConnect_OnValidSqliteFactory_Passes()
  {
    var options = new DbContextOptionsBuilder<TestDbContext>()
      .UseSqlite("Data Source=:memory:")
      .Options;
    var factory = new TestDbContextFactory(options);

    var v = DbValidations.CanConnect(factory);

    Assert.That(v.IsValid, Is.True);
  }

  [Test]
  public void CanConnect_OnFactoryThatThrows_FailsWithStructuredFailure()
  {
    var factory = new ThrowingFactory();

    var v = DbValidations.CanConnect(factory);

    Assert.That(v.IsValid, Is.False);
    Assert.That(v.Failures, Has.Count.EqualTo(1));
    Assert.That(v.Failures[0].Source, Is.EqualTo(nameof(TestDbContext)));
    Assert.That(v.Failures[0].Message, Does.Contain("threw"));
    Assert.That(v.Failures[0].Exception, Is.Not.Null);
  }

  [Test]
  public void IsConfigured_OnValidFactory_Passes()
  {
    var options = new DbContextOptionsBuilder<TestDbContext>()
      .UseSqlite("Data Source=:memory:")
      .Options;
    var factory = new TestDbContextFactory(options);

    var v = DbValidations.IsConfigured(factory);

    Assert.That(v.IsValid, Is.True);
  }

  [Test]
  public void IsConfigured_OnFactoryPointingAtMissingFile_Passes()
  {
    // The whole point of IsConfigured: it doesn't care whether the file
    // exists. CanConnect would fail here on SQLite; IsConfigured passes
    // because the factory and connection string are both valid.
    var options = new DbContextOptionsBuilder<TestDbContext>()
      .UseSqlite($"Data Source=/tmp/flowthru-does-not-exist-{Guid.NewGuid():N}.db")
      .Options;
    var factory = new TestDbContextFactory(options);

    var v = DbValidations.IsConfigured(factory);

    Assert.That(v.IsValid, Is.True);
  }

  [Test]
  public void IsConfigured_OnFactoryThatThrows_Fails()
  {
    var factory = new ThrowingFactory();

    var v = DbValidations.IsConfigured(factory);

    Assert.That(v.IsValid, Is.False);
    Assert.That(v.Failures[0].Source, Is.EqualTo(nameof(TestDbContext)));
    Assert.That(v.Failures[0].Exception, Is.Not.Null);
  }

  /// <summary>
  /// Test double whose <c>CreateDbContext</c> always throws. Used to simulate
  /// a misconfigured factory (e.g., bad connection string, missing options).
  /// </summary>
  private sealed class ThrowingFactory : IDbContextFactory<TestDbContext>
  {
    public TestDbContext CreateDbContext() =>
      throw new InvalidOperationException("simulated configuration failure");
  }
}
