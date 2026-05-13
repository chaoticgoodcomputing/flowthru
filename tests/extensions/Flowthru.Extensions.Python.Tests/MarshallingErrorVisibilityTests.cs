using Flowthru.Data.Schema;
using Flowthru.Step.Python.Internal;

namespace Flowthru.Extensions.Python.Tests;

/// <summary>
/// Regression: a property whose CLR type the marshaller cannot handle used
/// to surface end-to-end as the useless wrapper
/// <c>"Exception has been thrown by the target of an invocation."</c>
/// because <see cref="System.Reflection.MethodInfo.Invoke"/> swallowed the
/// inner <see cref="NotSupportedException"/> in a
/// <see cref="System.Reflection.TargetInvocationException"/> that no error
/// channel knew to unwrap. The fix is two-fold: <c>InvokeUnwrapping</c> in
/// <c>SubprocessPythonExecutor</c> re-throws the inner exception via
/// <c>ExceptionDispatchInfo</c>, and the marshaller itself enriches the
/// thrown message with the offending property name so the surface text
/// points at the cause.
/// </summary>
[TestFixture]
[Category("Python")]
public class MarshallingErrorVisibilityTests
{
  [FlowthruSchema]
  public partial record UnsupportedTypeSchema
  {
    public required int Id { get; init; }
    // DateOnly is recognized by FlowthruSchema as a flat scalar but is
    // intentionally outside the Arrow marshaller's supported set.
    public required DateOnly Day { get; init; }
  }

  [Test]
  public void ToRecordBatch_With_Unsupported_Property_Throws_NotSupported_With_Property_Name()
  {
    var rows = new[] { new UnsupportedTypeSchema { Id = 1, Day = new DateOnly(2024, 1, 1) } };

    var ex = Assert.Throws<NotSupportedException>(() => ArrowMarshaller.ToRecordBatch(rows));

    Assert.That(ex!.Message, Does.Contain("Day"),
      "The thrown message must name the offending property so the user can locate it without re-deriving the IPC protocol.");
    Assert.That(ex.Message, Does.Contain("DateOnly"),
      "The thrown message must name the offending type so the fix path (change-the-type vs. encode-as-string) is obvious.");
  }
}
