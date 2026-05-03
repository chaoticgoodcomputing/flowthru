using System.Diagnostics.CodeAnalysis;

namespace Flowthru.Core.Abstractions;

/// <summary>
/// Marks a schema property for automatic NewType generation via source generator.
/// The source generator will emit a <c>readonly record struct</c> NewType implementing
/// <see cref="IScalar"/> using the provided backing type, placed in a <c>Types</c> namespace
/// sibling to the schema.
/// </summary>
/// <remarks>
/// <para>
/// Usage: apply to a property of a <c>[FlowthruSchema]</c> type, where the property type
/// is the desired NewType name (as an unresolved identifier), and the constructor argument
/// specifies the backing type (e.g., <c>int</c>, <c>string</c>, <c>Guid</c>).
/// </para>
/// <para>
/// Example:
/// <code>
/// [FlowthruSchema]
/// public partial record OrderSchema
/// {
///     [FlowthruColumn(typeof(string))]
///     public required OrderId Id { get; init; }
/// }
/// </code>
/// The source generator emits:
/// <code>
/// public readonly record struct OrderId(string Value) : IScalar;
/// </code>
/// in namespace <c>MyProject.Data.Schemas.Types</c>.
/// </para>
/// <para>
/// The backing type must be a recognized scalar (CLR primitive, enum, <c>byte[]</c>,
/// BCL scalar struct, or <see cref="IScalar"/> implementor). Non-scalar types cause
/// compile-time error <c>FT1003</c>.
/// </para>
/// <para>
/// No implicit or explicit conversion operators are generated. Access the underlying value
/// via the <c>Value</c> property of the generated record struct.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[AttributeUsage(
  AttributeTargets.Property,
  Inherited = false,
  AllowMultiple = false
)]
public sealed class FlowthruColumnAttribute : Attribute
{
  /// <summary>
  /// The backing type for the generated NewType record struct.
  /// Must be a recognized scalar type (primitives, enums, <c>byte[]</c>, BCL scalar structs,
  /// or <see cref="IScalar"/> implementors).
  /// </summary>
  public Type BackingType { get; }

  /// <summary>
  /// Initializes a new instance of <see cref="FlowthruColumnAttribute"/>.
  /// </summary>
  /// <param name="backingType">The CLR type that will be wrapped by the generated NewType.</param>
  public FlowthruColumnAttribute(Type backingType)
  {
    BackingType = backingType;
  }
}
