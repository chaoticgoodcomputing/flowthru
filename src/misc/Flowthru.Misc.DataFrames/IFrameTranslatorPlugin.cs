using System.Reflection;

namespace Flowthru.DataFrames;

/// <summary>
/// Translates .NET member access (property or field) into a native column expression.
/// </summary>
/// <remarks>
/// Providers register implementations to teach the expression visitor how to handle
/// property reads beyond direct schema properties — for example, translating
/// <c>string.Length</c> into a native string-length function.
/// </remarks>
public interface IFrameMemberTranslator
{
  /// <summary>
  /// Attempts to translate a member access into a native expression.
  /// </summary>
  /// <param name="member">The property or field being accessed.</param>
  /// <param name="instance">
  /// The translated native expression for the instance, or <c>null</c> for static members.
  /// </param>
  /// <returns>A native expression, or <c>null</c> if this translator does not handle the member.</returns>
  object? Translate(MemberInfo member, object? instance);
}

/// <summary>
/// Translates .NET method calls into native frame operations.
/// </summary>
/// <remarks>
/// Providers register implementations to teach the expression visitor how to handle
/// method calls — for example, translating <c>Math.Abs(x)</c> into a native
/// absolute-value function.
/// </remarks>
public interface IFrameMethodTranslator
{
  /// <summary>
  /// Attempts to translate a method call into a native expression.
  /// </summary>
  /// <param name="method">The method being called.</param>
  /// <param name="instance">
  /// The translated native expression for the instance, or <c>null</c> for static methods.
  /// </param>
  /// <param name="arguments">The translated native expressions for each argument.</param>
  /// <returns>A native expression, or <c>null</c> if this translator does not handle the method.</returns>
  object? Translate(MethodInfo method, object? instance, IReadOnlyList<object?> arguments);
}
