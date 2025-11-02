namespace ML.Next.Core.Annotations;

/// <summary>
/// Marker interface for column annotation information.
/// </summary>
/// <remarks>
/// Annotations represent metadata about columns (normalization status, slot names, etc.)
/// that should be tracked through transformation pipelines.
/// </remarks>
public interface IAnnotations
{
}

/// <summary>
/// No annotations present.
/// </summary>
public struct NoAnnotations : IAnnotations
{
  public static readonly NoAnnotations Instance = new();
}
