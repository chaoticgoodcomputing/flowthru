using System.Reflection;

namespace Flowthru.Pipelines.Mapping;

/// <summary>
/// Base class for catalog mapping entries.
/// Represents a single mapping between a property and its data source/destination.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Design Pattern:</strong> This forms the base of a hierarchy implementing the
/// Strategy Pattern - different subclasses handle different types of mappings (catalog
/// entries vs. constant parameters).
/// </para>
/// </remarks>
internal abstract class CatalogMapping
{
    /// <summary>
    /// The property info for the property being mapped.
    /// </summary>
    public PropertyInfo Property { get; }

    protected CatalogMapping(PropertyInfo property)
    {
        Property = property ?? throw new ArgumentNullException(nameof(property));
    }

    /// <summary>
    /// Gets a descriptive string for this mapping (for error messages and logging).
    /// </summary>
    public abstract string Description { get; }
}
