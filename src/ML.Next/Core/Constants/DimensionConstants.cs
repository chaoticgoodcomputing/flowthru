namespace ML.Next.Core.Constants;

/// <summary>
/// Base class for type-level numeric constants.
/// This provides a way to pass compile-time constant values as generic type parameters.
/// </summary>
/// <typeparam name="T">The type of the constant value</typeparam>
/// <remarks>
/// Inspired by DotNext's Constant&lt;T&gt; pattern. Since the actual API isn't available
/// in DotNext 5.26.0, we implement our own lightweight version.
/// </remarks>
public abstract class Constant<T>
  where T : struct
{
  /// <summary>
  /// The constant value.
  /// </summary>
  public T Value { get; }

  /// <summary>
  /// Initializes a new constant with the specified value.
  /// </summary>
  /// <param name="value">The constant value</param>
  protected Constant(T value)
  {
    Value = value;
  }

  /// <summary>
  /// Implicit conversion to the underlying value type.
  /// </summary>
  public static implicit operator T(Constant<T> constant) => constant.Value;

  /// <summary>
  /// String representation of the constant.
  /// </summary>
  public override string ToString() => Value.ToString()!;
}

/// <summary>
/// Type-level dimension constants for vector columns.
/// These enable compile-time dimension tracking using phantom types.
/// </summary>
/// <remarks>
/// <para>
/// Usage example:
/// <code>
/// ColumnSpec&lt;float[], Dim4&gt; features; // 4-dimensional vector
/// </code>
/// </para>
/// <para>
/// The dimension value is accessible at both compile-time (via type parameter)
/// and runtime (via <c>new Dim4()</c> constructor).
/// </para>
/// </remarks>
public static class DimensionConstants
{
  /// <summary>1-dimensional (scalar treated as 1D vector)</summary>
  public sealed class Dim1 : Constant<long>
  {
    public Dim1()
      : base(1) { }
  }

  /// <summary>2-dimensional vector</summary>
  public sealed class Dim2 : Constant<long>
  {
    public Dim2()
      : base(2) { }
  }

  /// <summary>3-dimensional vector</summary>
  public sealed class Dim3 : Constant<long>
  {
    public Dim3()
      : base(3) { }
  }

  /// <summary>4-dimensional vector (common for Iris dataset)</summary>
  public sealed class Dim4 : Constant<long>
  {
    public Dim4()
      : base(4) { }
  }

  /// <summary>5-dimensional vector</summary>
  public sealed class Dim5 : Constant<long>
  {
    public Dim5()
      : base(5) { }
  }

  /// <summary>6-dimensional vector</summary>
  public sealed class Dim6 : Constant<long>
  {
    public Dim6()
      : base(6) { }
  }

  /// <summary>7-dimensional vector</summary>
  public sealed class Dim7 : Constant<long>
  {
    public Dim7()
      : base(7) { }
  }

  /// <summary>8-dimensional vector</summary>
  public sealed class Dim8 : Constant<long>
  {
    public Dim8()
      : base(8) { }
  }

  /// <summary>9-dimensional vector</summary>
  public sealed class Dim9 : Constant<long>
  {
    public Dim9()
      : base(9) { }
  }

  /// <summary>10-dimensional vector</summary>
  public sealed class Dim10 : Constant<long>
  {
    public Dim10()
      : base(10) { }
  }

  /// <summary>16-dimensional vector</summary>
  public sealed class Dim16 : Constant<long>
  {
    public Dim16()
      : base(16) { }
  }

  /// <summary>32-dimensional vector</summary>
  public sealed class Dim32 : Constant<long>
  {
    public Dim32()
      : base(32) { }
  }

  /// <summary>64-dimensional vector</summary>
  public sealed class Dim64 : Constant<long>
  {
    public Dim64()
      : base(64) { }
  }

  /// <summary>128-dimensional vector</summary>
  public sealed class Dim128 : Constant<long>
  {
    public Dim128()
      : base(128) { }
  }

  /// <summary>256-dimensional vector</summary>
  public sealed class Dim256 : Constant<long>
  {
    public Dim256()
      : base(256) { }
  }
}
