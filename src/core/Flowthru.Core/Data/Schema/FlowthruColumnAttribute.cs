using System.Diagnostics.CodeAnalysis;

namespace Flowthru.Data.Schema;

/// <summary>
/// Marks a schema property for automatic NewType generation. The source
/// generator emits a <c>readonly record struct</c> NewType implementing
/// <see cref="IScalar"/>, using the supplied <see cref="BackingType"/>,
/// placed in a <c>Types</c> namespace sibling to the schema.
/// </summary>
/// <remarks>
/// <para>
/// Apply to a property of a <c>[FlowthruSchema]</c>-attributed type whose
/// declared type is the desired NewType name. The constructor argument is
/// the backing primitive type the wrapper carries.
/// </para>
/// <para>
/// The backing type must be a recognized scalar (CLR primitive, enum,
/// <c>byte[]</c>, BCL scalar struct, or another <see cref="IScalar"/>
/// implementor). Non-scalar backing types fail with FT1003 at compile time.
/// No implicit/explicit conversion operators are generated; the underlying
/// value is accessed via the generated record struct's <c>Value</c>
/// property.
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
  /// <summary>The CLR type wrapped by the generated NewType.</summary>
  public Type BackingType { get; }

  public FlowthruColumnAttribute(Type backingType)
  {
    BackingType = backingType;
  }
}
