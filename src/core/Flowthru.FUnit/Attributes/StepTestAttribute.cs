namespace Flowthru.FUnit;

/// <summary>
/// Links a test method to the step type it exercises.
/// Consumed by <c>Flowthru.FUnit.SourceGenerators</c> to build the
/// <c>StepTestRegistry</c> and emit <c>FU001</c> warnings for uncovered steps.
/// </summary>
/// <param name="stepType">
/// The step class annotated with <c>[FlowthruStep]</c> that this test exercises.
/// </param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class StepTestAttribute(Type stepType) : Attribute
{
  /// <summary>The step type this test exercises.</summary>
  public Type StepType { get; } = stepType;
}
