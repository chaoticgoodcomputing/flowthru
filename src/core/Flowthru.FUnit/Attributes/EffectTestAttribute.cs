using System.Diagnostics.CodeAnalysis;

namespace Flowthru.FUnit;

/// <summary>
/// Links a test method to an effect node. Placeholder for future effect testing
/// support — no source generator behavior is attached to this attribute yet.
/// </summary>
// Coverage: Roslyn-only attribute — constructor never fires at runtime.
// Reserved for future EffectTest source generator support; no runtime reflection today.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class EffectTestAttribute : Attribute { }
