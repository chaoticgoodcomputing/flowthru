# <a id="Flowthru_Core_Data"></a> Namespace Flowthru.Core.Data

### Namespaces

 [Flowthru.Core.Data.Capabilities](Flowthru.Core.Data.Capabilities.md)

 [Flowthru.Core.Data.Storage](Flowthru.Core.Data.Storage.md)

 [Flowthru.Core.Data.Validation](Flowthru.Core.Data.Validation.md)

### Classes

 [CatalogAbstract](Flowthru.Core.Data.CatalogAbstract.md)

Base class for strongly-typed catalog implementations with automatic property caching.

 [EnumerableItemFactory](Flowthru.Core.Data.EnumerableItemFactory.md)

Extension point for <xref href="Flowthru.Core.Data.ItemFactory.Enumerable" data-throw-if-not-resolved="false"></xref> factory methods.

 [Item<T\>](Flowthru.Core.Data.Item\-1.md)

Standard catalog item implementation that delegates to a storage adapter.

 [ItemFactory](Flowthru.Core.Data.ItemFactory.md)

Static factory methods for creating catalog entries with common configurations.

 [ItemFactory.Single](Flowthru.Core.Data.ItemFactory.Single.md)

Factory methods for single (non-collection) values.

### Interfaces

 [IItem](Flowthru.Core.Data.IItem.md)

Non-generic base interface for catalog items — a specialization of <xref href="Flowthru.Core.Graph.INode" data-throw-if-not-resolved="false"></xref>
for data I/O nodes backed by storage adapters.

 [IItem<T\>](Flowthru.Core.Data.IItem\-1.md)

Typed catalog item — a specialization of <xref href="Flowthru.Core.Graph.INode%601" data-throw-if-not-resolved="false"></xref> for data I/O.

