using System.Diagnostics.CodeAnalysis;

namespace Flowthru.Core.Steps;

/// <summary>
/// Marker attribute identifying a class as a Flowthru step definition.
/// </summary>
/// <remarks>
/// <para>
/// This attribute enables step discovery by source generators and tooling:
/// </para>
/// <list type="bullet">
/// <item>FUnit source generators use it to discover steps and warn about missing tests.</item>
/// <item>Future Flowthru source generators may use it for compile-time validation
/// (e.g., verifying that <c>Create()</c> returns a compatible function signature).</item>
/// </list>
/// <para>
/// Follows the same pattern as <c>[FlowthruSchema]</c> — a core marker attribute
/// that downstream generators consume.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [FlowthruStep]
/// public static class EvaluateModelStep
/// {
///     public static Func&lt;
///         (IEnumerable&lt;PredictionSchema&gt;, IEnumerable&lt;TargetLabelSchema&gt;),
///         MetricsSchema
///     &gt; Create() =&gt; ...;
/// }
/// </code>
/// </example>
// Coverage: Roslyn-only attribute — constructor never fires at runtime.
// Consumed by FUnit source generators via Roslyn semantic models (queried by full
// type-name string match in StepTestRegistryGenerator and FunitDiagnosticAnalyzer).
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class FlowthruStepAttribute : Attribute { }
