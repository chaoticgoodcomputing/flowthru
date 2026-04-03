# <a id="Flowthru_Data"></a> Namespace Flowthru.Data

### Namespaces

 [Flowthru.Data.Capabilities](Flowthru.Data.Capabilities.md)

 [Flowthru.Data.Storage](Flowthru.Data.Storage.md)

 [Flowthru.Data.Validation](Flowthru.Data.Validation.md)

### Classes

 [CatalogAbstract](Flowthru.Data.CatalogAbstract.md)

Base class for strongly-typed catalog implementations with automatic property caching.

 [EnumerableItemFactory](Flowthru.Data.EnumerableItemFactory.md)

Extension point for <xref href="Flowthru.Data.ItemFactory.Enumerable" data-throw-if-not-resolved="false"></xref> factory methods.

 [Item<T\>](Flowthru.Data.Item\-1.md)

Standard catalog item implementation that delegates to a storage adapter.

 [ItemFactory](Flowthru.Data.ItemFactory.md)

Static factory methods for creating catalog entries with common configurations.

 [ItemFactory.Single](Flowthru.Data.ItemFactory.Single.md)

Factory methods for single (non-collection) values.

### Interfaces

 [IItem](Flowthru.Data.IItem.md)

Non-generic base interface for catalog items.
Provides untyped operations for internal use by the Flow executor and mapping layer.

 [IItem<T\>](Flowthru.Data.IItem\-1.md)

Unified catalog item with cardinality encoded in the type parameter.

