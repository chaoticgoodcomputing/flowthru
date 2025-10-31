using LanguageExt;

namespace Flowthru.ML.Ext.Core.Annotations;

/// <summary>
/// Annotation indicating a column has been normalized.
/// </summary>
public readonly record struct NormalizedAnnotation : IAnnotations {
  /// <summary>
  /// Whether the column is normalized.
  /// </summary>
  public bool IsNormalized { get; init; }

  /// <summary>
  /// The normalization method used (MinMax, MeanVariance, etc.).
  /// </summary>
  public Option<string> Method { get; init; }

  /// <summary>
  /// Creates a normalized annotation.
  /// </summary>
  public static NormalizedAnnotation Create(bool isNormalized, string? method = null) =>
      new() {
        IsNormalized = isNormalized,
        Method = method != null ? Option<string>.Some(method) : Option<string>.None
      };
}

/// <summary>
/// Annotation containing slot names for vector columns.
/// </summary>
public readonly record struct SlotNamesAnnotation : IAnnotations {
  /// <summary>
  /// The names for each slot in the vector.
  /// </summary>
  public Seq<string> SlotNames { get; init; }

  /// <summary>
  /// Creates a slot names annotation.
  /// </summary>
  public static SlotNamesAnnotation Create(params string[] slotNames) =>
      new() { SlotNames = LanguageExt.Seq.create(slotNames) };

  /// <summary>
  /// Creates a slot names annotation from a sequence.
  /// </summary>
  public static SlotNamesAnnotation Create(Seq<string> slotNames) =>
      new() { SlotNames = slotNames };
}

/// <summary>
/// Annotation for key type columns with cardinality information.
/// </summary>
public readonly record struct KeyTypeAnnotation : IAnnotations {
  /// <summary>
  /// The cardinality (count) of the key type.
  /// </summary>
  public Option<ulong> Cardinality { get; init; }

  /// <summary>
  /// Creates a key type annotation.
  /// </summary>
  public static KeyTypeAnnotation Create(ulong? cardinality = null) =>
      new() {
        Cardinality = cardinality.HasValue
              ? Option<ulong>.Some(cardinality.Value)
              : Option<ulong>.None
      };
}

/// <summary>
/// Combines two annotation types.
/// </summary>
public readonly record struct AnnotationSet<T1, T2> : IAnnotations
    where T1 : struct, IAnnotations
    where T2 : struct, IAnnotations {
  public T1 First { get; init; }
  public T2 Second { get; init; }

  public static AnnotationSet<T1, T2> Create(T1 first, T2 second) =>
      new() { First = first, Second = second };
}
