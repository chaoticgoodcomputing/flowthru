# <a id="Flowthru_Data"></a> Namespace Flowthru.Data

### Namespaces

 [Flowthru.Data.Capabilities](Flowthru.Data.Capabilities.md)

 [Flowthru.Data.Storage](Flowthru.Data.Storage.md)

 [Flowthru.Data.Validation](Flowthru.Data.Validation.md)

### Classes

 [CatalogEntries](Flowthru.Data.CatalogEntries.md)

Static factory methods for creating catalog entries with common configurations.

 [CatalogEntry<T\>](Flowthru.Data.CatalogEntry\-1.md)

Standard catalog entry implementation that delegates to a storage adapter.

 [DataCatalogBase](Flowthru.Data.DataCatalogBase.md)

Base class for strongly-typed catalog implementations with automatic property caching.

 [EnumerableCatalogEntries](Flowthru.Data.EnumerableCatalogEntries.md)

Extension point for <xref href="Flowthru.Data.CatalogEntries.Enumerable" data-throw-if-not-resolved="false"></xref> factory methods.

 [CatalogEntries.Single](Flowthru.Data.CatalogEntries.Single.md)

Factory methods for single (non-collection) values.

### Interfaces

 [ICatalogEntry<T\>](Flowthru.Data.ICatalogEntry\-1.md)

Unified catalog entry with cardinality encoded in the type parameter.

 [ICatalogEntry](Flowthru.Data.ICatalogEntry.md)

Non-generic base interface for catalog entries.
Provides untyped operations for internal use by the pipeline executor and mapping layer.

