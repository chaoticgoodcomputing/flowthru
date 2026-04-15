namespace Flowthru.Core.Steps.Factory;

/// <summary>
/// Factory for creating step instances using TypeActivator.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Design Pattern:</strong> Factory Pattern - provides a centralized location for
/// step instantiation logic.
/// </para>
/// <para>
/// This is a thin wrapper around TypeActivator, providing a domain-specific API for
/// creating steps. Could be extended in the future with:
/// - Step validation logic
/// - Pre/post-creation hooks
/// - Step decoration/wrapping
/// </para>
/// </remarks>
public static class StepFactory
{
    /// <summary>
    /// Creates a new instance of the specified step type.
    /// </summary>
    /// <typeparam name="TStep">The step type to instantiate</typeparam>
    /// <returns>A new step instance</returns>
    /// <remarks>
    /// <para>
    /// <strong>Requirements:</strong>
    /// - TStep must inherit from StepBase&lt;TInput, TOutput&gt;
    /// - TStep must have a parameterless constructor
    /// </para>
    /// <para>
    /// These requirements are enforced at compile-time via generic constraints in
    /// FlowBuilder.AddStep methods.
    /// </para>
    /// </remarks>
    public static TStep Create<TStep>()
      where TStep : new()
    {
        return TypeActivator.Create<TStep>();
    }
}
