#if FUNIT_ENABLED
using Flowthru.FUnit;
#endif

namespace FlowthruCoverage.Flows.Coverage.Steps;

/// <summary>
/// Step helper that recognizes Cobertura entries belonging to compiler-synthesized members
/// (async state machines, cached lambda holders, display-class closures, lambda bodies).
///
/// Coverlet preserves these by design — async state machines in particular are explicitly
/// kept across <c>[CompilerGenerated]</c> exclusions so user-authored async bodies remain
/// measurable. The pipeline filters them out only at the method-aggregation layer, leaving
/// package-level percentages computed from the full instrumented surface.
/// </summary>
/// <remarks>
/// All four shapes share a single syntactic marker: an angle-bracket-wrapped segment in
/// either the containing type name (after the <c>/</c> nested-type separator) or the
/// method name itself. C# generics in Cobertura are spelled with backticks
/// (<c>Foo`1</c>), so a <c>Contains('&lt;')</c> check is unambiguous and false-positive-safe
/// against user code.
/// </remarks>
public static class CompilerGeneratedFilter
{
  /// <summary>
  /// Returns <c>true</c> when the given Cobertura class/method pair denotes a compiler-generated
  /// member that should not appear in the authored-method coverage report.
  /// </summary>
  /// <param name="className">
  /// Cobertura's fully-qualified class name (e.g. <c>Flowthru.Core.Cli.FlowthruCli/&lt;RunAsync&gt;d__5</c>).
  /// </param>
  /// <param name="methodName">
  /// Cobertura's method name (e.g. <c>MoveNext</c>, <c>&lt;ExecuteStepAsync&gt;b__50_0</c>).
  /// </param>
  public static bool IsCompilerGenerated(string className, string methodName) =>
    className.Contains('<') || methodName.Contains('<');

#if FUNIT_ENABLED
  /// <summary>FUnit tests for <see cref="CompilerGeneratedFilter"/>.</summary>
  public class Tests : FunitContext
  {
    /// <summary>
    /// Async state machines surface as <c>&lt;MethodName&gt;d__N</c> nested types whose
    /// body is reported under <c>MoveNext</c>. The class name carries the marker.
    /// </summary>
    [Test]
    public void AsyncStateMachine_MoveNext_IsFiltered()
    {
      var result = CompilerGeneratedFilter.IsCompilerGenerated(
        className: "Flowthru.Core.Cli.FlowthruCli/<RunAsync>d__5",
        methodName: "MoveNext"
      );

      Assert.That(result, Is.True);
    }

    /// <summary>
    /// Cached static lambdas land on the compiler-emitted <c>&lt;&gt;c</c> singleton. Both the
    /// class name and the method name carry markers — either alone is sufficient.
    /// </summary>
    [Test]
    public void CachedStaticLambda_IsFiltered()
    {
      var result = CompilerGeneratedFilter.IsCompilerGenerated(
        className: "Flowthru.Core.Flows.Flow/<>c",
        methodName: "<ExecuteStepAsync>b__50_0"
      );

      Assert.That(result, Is.True);
    }

    /// <summary>
    /// Generic-method cached lambdas land on <c>&lt;&gt;c__N`X</c> (one cache type per
    /// generic arity). Backtick arity does not affect the angle-bracket marker.
    /// </summary>
    [Test]
    public void CachedGenericLambda_IsFiltered()
    {
      var result = CompilerGeneratedFilter.IsCompilerGenerated(
        className: "Flowthru.FUnit.Samples.SampleBuilder/<>c__2`1",
        methodName: "<FromCsv>b__2_0"
      );

      Assert.That(result, Is.True);
    }

    /// <summary>
    /// Lambdas that capture local variables get a per-method <c>&lt;&gt;c__DisplayClassN_M</c>
    /// holder. The class name carries the angle-bracket marker.
    /// </summary>
    [Test]
    public void DisplayClassLambda_IsFiltered()
    {
      var result = CompilerGeneratedFilter.IsCompilerGenerated(
        className: "Flowthru.Core.Flows.Flow/<>c__DisplayClass50_0",
        methodName: "<ExecuteStepAsync>b__1"
      );

      Assert.That(result, Is.True);
    }

    /// <summary>
    /// A user-authored method literally named <c>MoveNext</c> (e.g. a custom enumerator)
    /// must NOT be filtered — the angle-bracket marker is the discriminator, not the name.
    /// </summary>
    [Test]
    public void UserMethodNamedMoveNext_IsNotFiltered()
    {
      var result = CompilerGeneratedFilter.IsCompilerGenerated(
        className: "MyApp.CustomEnumerator",
        methodName: "MoveNext"
      );

      Assert.That(result, Is.False);
    }

    /// <summary>
    /// Cobertura emits open generic types with backtick-arity (<c>Foo`1</c>), not angle
    /// brackets, so generic user code must never trigger the filter.
    /// </summary>
    [Test]
    public void GenericUserClass_IsNotFiltered()
    {
      var result = CompilerGeneratedFilter.IsCompilerGenerated(
        className: "MyApp.Container`1",
        methodName: "Add"
      );

      Assert.That(result, Is.False);
    }

    /// <summary>
    /// A user-authored async method on a generic class — both backtick arity and a synthesized
    /// state machine on the same path. The state machine is the entry that should be filtered;
    /// the user-authored entry-point method on the generic class is preserved.
    /// </summary>
    [Test]
    public void AsyncMethodOnGenericClass_OnlyStateMachineIsFiltered()
    {
      var userEntry = CompilerGeneratedFilter.IsCompilerGenerated(
        className: "MyApp.Container`1",
        methodName: "AddAsync"
      );
      var stateMachine = CompilerGeneratedFilter.IsCompilerGenerated(
        className: "MyApp.Container`1/<AddAsync>d__3",
        methodName: "MoveNext"
      );

      Assert.That(userEntry, Is.False);
      Assert.That(stateMachine, Is.True);
    }

    /// <summary>
    /// A plain user method on a plain class — neither field has angle brackets, so the
    /// predicate must return false.
    /// </summary>
    [Test]
    public void PlainUserMethod_IsNotFiltered()
    {
      var result = CompilerGeneratedFilter.IsCompilerGenerated(
        className: "Flowthru.Core.Effects.FlowUnit",
        methodName: "ToString"
      );

      Assert.That(result, Is.False);
    }
  }
#endif
}
