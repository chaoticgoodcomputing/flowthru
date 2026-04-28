using Flowthru.Core.Steps;

namespace Flowthru.Core.Tests.Execution;

/// <summary>
/// Tests for the <see cref="NoData.Result"/> DX-sugar wrapper used in side-effect-only
/// step transforms. Confirms it returns a task containing exactly one <see cref="NoData.Value"/>.
/// </summary>
[TestFixture]
[Category("Execution")]
public class NoDataResultTests
{
  [Test]
  public async Task Result_ReturnsCompletedTaskContainingSingleValue()
  {
    var task = NoData.Result();
    var enumerable = await task;

    var list = enumerable.ToList();
    Assert.That(list, Has.Count.EqualTo(1));
    Assert.That(list[0], Is.SameAs(NoData.Value));
  }
}
