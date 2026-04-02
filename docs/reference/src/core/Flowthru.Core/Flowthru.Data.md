# <a id="Flowthru_Data"></a> Namespace Flowthru.Data

### Namespaces

 [Flowthru.Data.Capabilities](Flowthru.Data.Capabilities.md)

 [Flowthru.Data.Storage](Flowthru.Data.Storage.md)

 [Flowthru.Data.Validation](Flowthru.Data.Validation.md)

### Classes

 [CatalogAbstract](Flowthru.Data.CatalogAbstract.md)

Base class for strongly-typed catalog implementations with automatic property caching.

 [EnumerableItems](Flowthru.Data.EnumerableItems.md)

Extension point for <xref href="Flowthru.Data.Items.Enumerable" data-throw-if-not-resolved="false"></xref> factory methods.

 [Item<T\>](Flowthru.Data.Item\-1.md)

Standard catalog item implementation that delegates to a storage adapter.

 [Items](Flowthru.Data.Items.md)

Static factory methods for creating catalog entries with common configurations.

 [Items.Single](Flowthru.Data.Items.Single.md)

Factory methods for single (non-collection) values.

### Interfaces

 [IItem<T\>](Flowthru.Data.IItem\-1.md)

Unified catalog item with cardinality encoded in the type parameter.

 [IItem](Flowthru.Data.IItem.md)

Non-generic base interface for catalog items.
Provides untyped operations for internal use by the flow executor and mapping layer.

