using System.Diagnostics.CodeAnalysis;

namespace Flowthru.Step.Testing;

/// <summary>
/// Links a test method to the step type it exercises. The namespace
/// and attribute name pair the FUnit test framework unambiguously
/// with the step archetype it tests.
/// </summary>
/// <param name="stepType">
/// The step class annotated with <c>[FlowthruStep]</c> that this
/// test exercises.
/// </param>
/// <remarks>
/// <para>
/// Consumed by <c>FUnit.SourceGenerators</c> to build the
/// <c>StepTestRegistry</c> and emit per-test-framework runner classes
/// (NUnit / xUnit / MSTest) so <c>dotnet test</c> discovers the test
/// without any framework attribute appearing in user code. The
/// constructor never runs at runtime — the attribute is consumed
/// exclusively via Roslyn semantic models.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class FUnitStepTestAttribute(Type stepType) : Attribute
{
  /// <summary>The step type this test exercises.</summary>
  public Type StepType { get; } = stepType;
}
