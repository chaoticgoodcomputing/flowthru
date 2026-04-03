# <a id="Flowthru_Data_Storage"></a> Namespace Flowthru.Data.Storage

### Namespaces

 [Flowthru.Data.Storage.Container](Flowthru.Data.Storage.Container.md)

 [Flowthru.Data.Storage.Format](Flowthru.Data.Storage.Format.md)

 [Flowthru.Data.Storage.Medium](Flowthru.Data.Storage.Medium.md)

 [Flowthru.Data.Storage.Strategies](Flowthru.Data.Storage.Strategies.md)

### Classes

 [BinaryFileStorageAdapter](Flowthru.Data.Storage.BinaryFileStorageAdapter.md)

Storage adapter for binary files with byte array content.

 [ComposedStorageAdapter<TContainer, TRow\>](Flowthru.Data.Storage.ComposedStorageAdapter\-2.md)

Composed storage adapter that delegates to medium, format, and container layers.

 [MemoryStorageAdapter<T\>](Flowthru.Data.Storage.MemoryStorageAdapter\-1.md)

Direct memory storage adapter that bypasses serialization.

 [NullStorageAdapter<T\>](Flowthru.Data.Storage.NullStorageAdapter\-1.md)

Null storage adapter for side-effect-only nodes that produce no meaningful data.

 [PropertyMappingConfiguration](Flowthru.Data.Storage.PropertyMappingConfiguration.md)

Describes how a format serializer handles property-to-field name mapping.

 [SchemaActivator](Flowthru.Data.Storage.SchemaActivator.md)

Factory for creating schema instances, supporting both traditional parameterless constructors
and modern C# features like required members and positional records.

 [SingletonJsonStorageAdapter<T\>](Flowthru.Data.Storage.SingletonJsonStorageAdapter\-1.md)

Direct JSON file storage for singleton objects (not collections).

 [TextFileStorageAdapter](Flowthru.Data.Storage.TextFileStorageAdapter.md)

Storage adapter for plain text files with string content.

### Interfaces

 [IContainerAdapter<TContainer, TRow\>](Flowthru.Data.Storage.IContainerAdapter\-2.md)

Interface for container adaptation - converts between streaming rows and in-memory containers.

 [IFormatSerializer<TRow\>](Flowthru.Data.Storage.IFormatSerializer\-1.md)

Interface for format serialization - handles row-based serialization/deserialization.

 [IStorageAdapter<T\>](Flowthru.Data.Storage.IStorageAdapter\-1.md)

Interface for high-level storage operations - abstracts Load/Save with any storage implementation.

 [IStorageMedium](Flowthru.Data.Storage.IStorageMedium.md)

Interface for storage medium - handles raw byte stream I/O.

### Enums

 [PropertyMappingStrategy](Flowthru.Data.Storage.PropertyMappingStrategy.md)

Property mapping strategy used by a format serializer.

