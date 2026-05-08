using System.Reflection;

namespace Flowthru.Data.Schema.Mapping;

/// <summary>
/// Kind-specific data for a <see cref="PropertyBinding"/> whose
/// <see cref="PropertyBinding.Kind"/> is <see cref="PropertyKind.IScalar"/>.
/// Provides the metadata format converters need to read the cell as the
/// backing type and construct the wrapper.
/// </summary>
/// <param name="ScalarType">The IScalar wrapper type (e.g. <c>CustomerId</c>).</param>
/// <param name="BackingType">The single primitive the wrapper round-trips through.</param>
/// <param name="ValueProperty">Reflection handle for the wrapper's value-accessor property.</param>
/// <param name="WrappingConstructor">Reflection handle for the wrapper's single-arg constructor taking the backing type.</param>
public sealed record IScalarBindingInfo(
  Type ScalarType,
  Type BackingType,
  PropertyInfo ValueProperty,
  ConstructorInfo WrappingConstructor
);
