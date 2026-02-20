namespace Flowthru.Effects;

/// <summary>
/// Represents a void-like value for effect operations with no meaningful return value.
/// Similar to <c>Unit</c> in functional programming or <c>void</c> in imperative programming,
/// but usable as a type parameter.
/// </summary>
/// <remarks>
/// <para>
/// Use <see cref="FlowUnit"/> when an effect produces side effects but no meaningful result.
/// For example, <c>FlowIO&lt;FlowUnit&gt;</c> represents an I/O operation that doesn't return data.
/// </para>
/// <para>
/// <strong>Example:</strong>
/// </para>
/// <code>
/// FlowIO&lt;FlowUnit&gt; WriteLog(string message) =>
///     FlowIO.LiftAsync(async ct => {
///         await File.WriteAllTextAsync("log.txt", message, ct);
///         return FlowUnit.Default;
///     });
/// </code>
/// </remarks>
public readonly struct FlowUnit : IEquatable<FlowUnit>, IComparable<FlowUnit>
{
  /// <summary>
  /// The single instance of <see cref="FlowUnit"/>.
  /// </summary>
  public static readonly FlowUnit Default = new();

  /// <summary>
  /// Determines whether the specified <see cref="FlowUnit"/> is equal to the current instance.
  /// </summary>
  /// <param name="other">The <see cref="FlowUnit"/> to compare.</param>
  /// <returns>Always <c>true</c>, as all FlowUnit instances are equal.</returns>
  public bool Equals(FlowUnit other) => true;

  /// <summary>
  /// Determines whether the specified object is equal to the current instance.
  /// </summary>
  /// <param name="obj">The object to compare.</param>
  /// <returns><c>true</c> if <paramref name="obj"/> is a <see cref="FlowUnit"/>; otherwise, <c>false</c>.</returns>
  public override bool Equals(object? obj) => obj is FlowUnit;

  /// <summary>
  /// Returns the hash code for this instance.
  /// </summary>
  /// <returns>Always returns 0, as all FlowUnit instances are equal.</returns>
  public override int GetHashCode() => 0;

  /// <summary>
  /// Returns a string representation of this instance.
  /// </summary>
  /// <returns>Always returns "unit".</returns>
  public override string ToString() => "unit";

  /// <summary>
  /// Compares the current instance with another <see cref="FlowUnit"/>.
  /// </summary>
  /// <param name="other">The <see cref="FlowUnit"/> to compare.</param>
  /// <returns>Always 0, as all FlowUnit instances are equal.</returns>
  public int CompareTo(FlowUnit other) => 0;

  /// <summary>
  /// Determines whether two <see cref="FlowUnit"/> instances are equal.
  /// </summary>
  /// <param name="left">The first instance to compare.</param>
  /// <param name="right">The second instance to compare.</param>
  /// <returns>Always <c>true</c>.</returns>
  public static bool operator ==(FlowUnit left, FlowUnit right) => true;

  /// <summary>
  /// Determines whether two <see cref="FlowUnit"/> instances are not equal.
  /// </summary>
  /// <param name="left">The first instance to compare.</param>
  /// <param name="right">The second instance to compare.</param>
  /// <returns>Always <c>false</c>.</returns>
  public static bool operator !=(FlowUnit left, FlowUnit right) => false;

  /// <summary>
  /// Compares two <see cref="FlowUnit"/> instances.
  /// </summary>
  /// <param name="left">The first instance to compare.</param>
  /// <param name="right">The second instance to compare.</param>
  /// <returns>Always <c>false</c>.</returns>
  public static bool operator <(FlowUnit left, FlowUnit right) => false;

  /// <summary>
  /// Compares two <see cref="FlowUnit"/> instances.
  /// </summary>
  /// <param name="left">The first instance to compare.</param>
  /// <param name="right">The second instance to compare.</param>
  /// <returns>Always <c>true</c>.</returns>
  public static bool operator <=(FlowUnit left, FlowUnit right) => true;

  /// <summary>
  /// Compares two <see cref="FlowUnit"/> instances.
  /// </summary>
  /// <param name="left">The first instance to compare.</param>
  /// <param name="right">The second instance to compare.</param>
  /// <returns>Always <c>false</c>.</returns>
  public static bool operator >(FlowUnit left, FlowUnit right) => false;

  /// <summary>
  /// Compares two <see cref="FlowUnit"/> instances.
  /// </summary>
  /// <param name="left">The first instance to compare.</param>
  /// <param name="right">The second instance to compare.</param>
  /// <returns>Always <c>true</c>.</returns>
  public static bool operator >=(FlowUnit left, FlowUnit right) => true;
}
