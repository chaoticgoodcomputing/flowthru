# <a id="Flowthru_Core_Data_Storage"></a> Namespace Flowthru.Core.Data.Storage

### Namespaces

 [Flowthru.Core.Data.Storage.Container](Flowthru.Core.Data.Storage.Container.md)

 [Flowthru.Core.Data.Storage.Format](Flowthru.Core.Data.Storage.Format.md)

 [Flowthru.Core.Data.Storage.Medium](Flowthru.Core.Data.Storage.Medium.md)

 [Flowthru.Core.Data.Storage.Strategies](Flowthru.Core.Data.Storage.Strategies.md)

### Classes

 [BinaryFileStorageAdapter](Flowthru.Core.Data.Storage.BinaryFileStorageAdapter.md)

Storage adapter for binary files with byte array content.

 [ComposedStorageAdapter<TContainer, TRow\>](Flowthru.Core.Data.Storage.ComposedStorageAdapter\-2.md)

Composed storage adapter that delegates to medium, format, and container layers.

 [ConfigurationStorageAdapter<T\>](Flowthru.Core.Data.Storage.ConfigurationStorageAdapter\-1.md)

Read-only storage adapter that binds an <xref href="Microsoft.Extensions.Configuration.IConfiguration" data-throw-if-not-resolved="false"></xref> section to a typed POCO.

 [LocalFileWriteProbe](Flowthru.Core.Data.Storage.LocalFileWriteProbe.md)

Shared write-access probe for local filesystem paths.

 [MemoryStorageAdapter<T\>](Flowthru.Core.Data.Storage.MemoryStorageAdapter\-1.md)

Direct memory storage adapter that bypasses serialization.

 [NullStorageAdapter<T\>](Flowthru.Core.Data.Storage.NullStorageAdapter\-1.md)

Null storage adapter for side-effect-only nodes that produce no meaningful data.

 [PropertyMappingConfiguration](Flowthru.Core.Data.Storage.PropertyMappingConfiguration.md)

Describes how a format serializer handles property-to-field name mapping.

 [ReadOnlyDirectoryStorageAdapter<TItem\>](Flowthru.Core.Data.Storage.ReadOnlyDirectoryStorageAdapter\-1.md)

Abstract base for read-only storage adapters that aggregate all files of a given
pattern within a directory into a single item sequence.

 [SchemaActivator](Flowthru.Core.Data.Storage.SchemaActivator.md)

Factory for creating schema instances, supporting both traditional parameterless constructors
and modern C# features like required members and positional records.

 [SingletonJsonStorageAdapter<T\>](Flowthru.Core.Data.Storage.SingletonJsonStorageAdapter\-1.md)

Direct JSON file storage for singleton objects (not collections).

 [StorageMediumResolver](Flowthru.Core.Data.Storage.StorageMediumResolver.md)

Default implementation of <xref href="Flowthru.Core.Data.Storage.IStorageMediumResolver" data-throw-if-not-resolved="false"></xref>.

 [TextFileStorageAdapter](Flowthru.Core.Data.Storage.TextFileStorageAdapter.md)

Storage adapter for plain text files with string content.

### Interfaces

 [IContainerAdapter<TContainer, TRow\>](Flowthru.Core.Data.Storage.IContainerAdapter\-2.md)

Interface for container adaptation - converts between streaming rows and in-memory containers.

 [IFormatSerializer<TRow\>](Flowthru.Core.Data.Storage.IFormatSerializer\-1.md)

Interface for format serialization - handles row-based serialization/deserialization.

 [IHasEfficientCount](Flowthru.Core.Data.Storage.IHasEfficientCount.md)

Optional interface for storage adapters that can return a row count without
materializing the full dataset.

 [IStorageAdapter<T\>](Flowthru.Core.Data.Storage.IStorageAdapter\-1.md)

Interface for high-level storage operations - abstracts Load/Save with any storage implementation.

 [IStorageMedium](Flowthru.Core.Data.Storage.IStorageMedium.md)

Interface for storage medium - handles raw byte stream I/O.

 [IStorageMediumProvider](Flowthru.Core.Data.Storage.IStorageMediumProvider.md)

Factory for creating <xref href="Flowthru.Core.Data.Storage.IStorageMedium" data-throw-if-not-resolved="false"></xref> instances for a specific URI scheme.

 [IStorageMediumResolver](Flowthru.Core.Data.Storage.IStorageMediumResolver.md)

Resolves the appropriate <xref href="Flowthru.Core.Data.Storage.IStorageMedium" data-throw-if-not-resolved="false"></xref> for a given file path or URI string.

### Enums

 [PropertyMappingStrategy](Flowthru.Core.Data.Storage.PropertyMappingStrategy.md)

Property mapping strategy used by a format serializer.

