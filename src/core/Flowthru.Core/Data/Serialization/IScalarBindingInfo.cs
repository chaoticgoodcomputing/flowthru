using System.Reflection;

namespace Flowthru.Core.Data.Serialization;

/// <summary>
/// Kind-specific data for a <see cref="PropertyBinding"/> whose
/// <see cref="PropertyBinding.Kind"/> is <see cref="PropertyKind.IScalar"/>.
/// </summary>
/// <param name="ScalarType">
/// The IScalar wrapper type (the user-defined NewType — e.g.,
/// <c>record struct CustomerId(string Value) : IScalar</c>).
/// </param>
/// <param name="BackingType">
/// The single primitive type the wrapper round-trips through (e.g., <c>string</c> for
/// <c>CustomerId(string Value)</c>). Format converters read/write the cell as this type.
/// </param>
/// <param name="ValueProperty">
/// Reflection handle for the wrapper's single public readable property — used by writers
/// to extract the backing value (<c>scalarInstance.Value</c> in the canonical case).
/// </param>
/// <param name="WrappingConstructor">
/// Reflection handle for the wrapper's single-arg constructor that takes a
/// <see cref="BackingType"/> — used by readers to construct the wrapper from a parsed
/// backing value.
/// </param>
public sealed record IScalarBindingInfo(
  Type ScalarType,
  Type BackingType,
  PropertyInfo ValueProperty,
  ConstructorInfo WrappingConstructor
);
