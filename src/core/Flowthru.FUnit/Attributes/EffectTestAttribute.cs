namespace Flowthru.FUnit;

/// <summary>
/// Links a test method to an effect node. Placeholder for future effect testing
/// support — no source generator behavior is attached to this attribute yet.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class EffectTestAttribute : Attribute { }
