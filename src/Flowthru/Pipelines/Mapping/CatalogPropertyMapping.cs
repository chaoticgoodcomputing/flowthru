using System.Reflection;
using Flowthru.Data;

namespace Flowthru.Pipelines.Mapping;

/// <summary>
/// Mapping entry that connects a property to a catalog entry.
/// Used for bidirectional data flow (input loading and output saving).
/// </summary>
internal class CatalogPropertyMapping : CatalogMapping {
  /// <summary>
  /// The catalog entry that this property maps to/from.
  /// </summary>
  public ICatalogEntry CatalogEntry { get; }

  public CatalogPropertyMapping(PropertyInfo property, ICatalogEntry catalogEntry)
      : base(property) {
    CatalogEntry = catalogEntry ?? throw new ArgumentNullException(nameof(catalogEntry));
  }

  /// <inheritdoc/>
  public override string Description =>
      $"Property '{Property.Name}' mapped to catalog entry '{CatalogEntry.Key}'";
}
