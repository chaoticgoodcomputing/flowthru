using Flowthru.Prelude;
using Flowthru.Validation.Runtime;

namespace Flowthru.Core.Tests.Prelude;

/// <summary>
/// Pins the closed-sum contract on <see cref="EffResult{A}"/>: the
/// <see cref="EffResult{A}.Success"/> and <see cref="EffResult{A}.Failure"/>
/// cases each carry their payload, the <c>IsSuccess</c> / <c>IsFailure</c>
/// discriminators agree with the runtime type, and <see cref="EffResult{A}.Match"/>
/// dispatches to the correct branch. The struct is the boundary type at
/// every <c>FlowIO.Run</c> call site, so its semantics are load-bearing
/// for downstream pattern matches.
/// </summary>
[TestFixture]
public class EffResultTests
{
  [Test]
  public void Success_CarriesValue()
  {
    var result = new EffResult<int>.Success(42);
    Assert.That(result.Value, Is.EqualTo(42));
  }

  [Test]
  public void Failure_CarriesError()
  {
    var error = new RuntimeError.External("source", new Exception("boom"));
    var result = new EffResult<int>.Failure(error);
    Assert.That(result.Error, Is.SameAs(error));
  }

  [Test]
  public void IsSuccess_TrueOnSuccess_FalseOnFailure()
  {
    EffResult<int> success = new EffResult<int>.Success(7);
    EffResult<int> failure = new EffResult<int>.Failure(
      new RuntimeError.InvariantViolated("check", "reason")
    );
    Assert.That(success.IsSuccess, Is.True);
    Assert.That(failure.IsSuccess, Is.False);
  }

  [Test]
  public void IsFailure_FalseOnSuccess_TrueOnFailure()
  {
    EffResult<int> success = new EffResult<int>.Success(7);
    EffResult<int> failure = new EffResult<int>.Failure(
      new RuntimeError.InvariantViolated("check", "reason")
    );
    Assert.That(success.IsFailure, Is.False);
    Assert.That(failure.IsFailure, Is.True);
  }

  [Test]
  public void Match_DispatchesToSuccessBranch()
  {
    EffResult<int> result = new EffResult<int>.Success(13);
    var matched = result.Match(
      onSuccess: v => $"got:{v}",
      onFailure: e => $"err:{e.Message}"
    );
    Assert.That(matched, Is.EqualTo("got:13"));
  }

  [Test]
  public void Match_DispatchesToFailureBranch()
  {
    EffResult<int> result = new EffResult<int>.Failure(
      new RuntimeError.InvariantViolated("check.A", "saw nothing")
    );
    var matched = result.Match(
      onSuccess: v => $"got:{v}",
      onFailure: e => $"err:{e.Message}"
    );
    Assert.That(matched, Does.StartWith("err:"));
    Assert.That(matched, Does.Contain("saw nothing"));
  }

  [Test]
  public void Records_SupportValueEquality()
  {
    var a = new EffResult<int>.Success(5);
    var b = new EffResult<int>.Success(5);
    Assert.That(a, Is.EqualTo(b));
  }
}
