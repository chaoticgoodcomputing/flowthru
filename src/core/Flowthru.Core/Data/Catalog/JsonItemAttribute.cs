namespace Flowthru.Data.Catalog;

/// <summary>
/// Marks a partial <see cref="IItem{T}"/> property as JSON-backed. The
/// source generator emits the property body that wires
/// <c>ItemFactory.Enumerable.Json&lt;TRow&gt;</c> or
/// <c>ItemFactory.Singleton.Json&lt;T&gt;</c> based on the property's
/// declared type. The label is inferred from the property name; the
/// schema is inferred from the property type.
/// </summary>
/// <remarks>
/// <para>
/// Replaces the nested <c>CreateItem(() => ItemFactory.Enumerable.Json&lt;T&gt;(...))</c>
/// pattern from §1.4. For paths that need <c>BasePath</c>-style
/// interpolation, the manual <see cref="CatalogAbstract.CreateItem{T}"/>
/// fallback remains available — the attribute is for the common case
/// where the path is a literal string relative to the working directory.
/// </para>
/// <para>
/// Apply to <c>partial IItem&lt;TContainer&gt; { get; }</c> declarations
/// inside a <see cref="CatalogAbstract"/>-derived class. The generator
/// requires the property to be:
/// <list type="bullet">
///   <item>declared as <c>partial</c></item>
///   <item>typed <c>IItem&lt;IEnumerable&lt;TRow&gt;&gt;</c> for collections, or <c>IItem&lt;T&gt;</c> for singletons</item>
///   <item>declared inside a partial class derived from <c>CatalogAbstract</c></item>
/// </list>
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class JsonItemAttribute : Attribute
{
  /// <summary>The JSON file path (relative or absolute).</summary>
  public string Path { get; }

  public JsonItemAttribute(string path)
  {
    if (string.IsNullOrWhiteSpace(path))
    {
      throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));
    }
    Path = path;
  }
}
