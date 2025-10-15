namespace Flowthru.Data;

/// <summary>
/// Builder for constructing DataCatalog instances using a fluent API.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Design Pattern:</strong> Builder Pattern - provides a fluent interface for
/// incrementally constructing a DataCatalog with multiple entries.
/// </para>
/// <para>
/// <strong>Usage Example:</strong>
/// <code>
/// var catalog = DataCatalogBuilder.BuildCatalog(builder =>
/// {
///     builder.Register("companies", new CsvCatalogEntry&lt;CompanySchema&gt;("data/companies.csv"));
///     builder.Register("shuttles", new ParquetCatalogEntry&lt;ShuttleSchema&gt;("data/shuttles.parquet"));
/// });
/// </code>
/// </para>
/// </remarks>
public class DataCatalogBuilder
{
    private readonly DataCatalog _catalog = new();

    /// <summary>
    /// Registers a catalog entry with the builder.
    /// </summary>
    /// <typeparam name="T">The type of data stored in the catalog entry</typeparam>
    /// <param name="key">Unique identifier for the catalog entry</param>
    /// <param name="entry">The catalog entry to register</param>
    /// <returns>This builder for method chaining</returns>
    public DataCatalogBuilder Register<T>(string key, ICatalogEntry<T> entry)
    {
        _catalog.Register(key, entry);
        return this;
    }

    /// <summary>
    /// Builds and returns the configured DataCatalog.
    /// </summary>
    /// <returns>The constructed DataCatalog</returns>
    public DataCatalog Build() => _catalog;

    /// <summary>
    /// Static factory method for building a catalog with a configuration action.
    /// </summary>
    /// <param name="configure">Action that configures the catalog builder</param>
    /// <returns>The constructed DataCatalog</returns>
    public static DataCatalog BuildCatalog(Action<DataCatalogBuilder> configure)
    {
        var builder = new DataCatalogBuilder();
        configure(builder);
        return builder.Build();
    }
}
