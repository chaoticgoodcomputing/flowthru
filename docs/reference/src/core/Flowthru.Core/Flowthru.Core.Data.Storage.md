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

 [DirectoryStorageAdapter<T\>](Flowthru.Core.Data.Storage.DirectoryStorageAdapter\-1.md)

Single, format-agnostic storage adapter for a <xref href="Flowthru.Core.Data.Directory%601" data-throw-if-not-resolved="false"></xref> of same-schema
files. Format concerns are externalised through <code>perFileAdapter</code>: the directory
owns enumeration, save ordering, and target validation; the per-file adapter owns
serialisation for one path.

 [LocalFileWriteProbe](Flowthru.Core.Data.Storage.LocalFileWriteProbe.md)

Shared write-access probe for local filesystem paths.

 [MemoryStorageAdapter<T\>](Flowthru.Core.Data.Storage.MemoryStorageAdapter\-1.md)

Direct memory storage adapter that bypasses serialization.

 [PropertyMappingConfiguration](Flowthru.Core.Data.Storage.PropertyMappingConfiguration.md)

Describes how a format serializer handles property-to-field name mapping.

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

 [IFormatBase<TRow\>](Flowthru.Core.Data.Storage.IFormatBase\-1.md)

Common surface shared by <xref href="Flowthru.Core.Data.Storage.IFormatRowReader%601" data-throw-if-not-resolved="false"></xref> and
<xref href="Flowthru.Core.Data.Storage.IFormatRowWriter%601" data-throw-if-not-resolved="false"></xref>. Holds the metadata every format extension
declares regardless of read or write capability — runtime traits, row-shape
feature claims, and property-mapping strategy.

 [IFormatRowReader<TRow\>](Flowthru.Core.Data.Storage.IFormatRowReader\-1.md)

Format extension that can deserialize rows from a byte stream. Read-only formats
(e.g., Excel via ExcelDataReader) implement this interface and not
<xref href="Flowthru.Core.Data.Storage.IFormatRowWriter%601" data-throw-if-not-resolved="false"></xref> — their inability to write is a structural
fact carried in the type system, not a runtime trait check.

 [IFormatRowWriter<TRow\>](Flowthru.Core.Data.Storage.IFormatRowWriter\-1.md)

Format extension that can serialize rows to a byte stream. Write-only sinks (rare
— typically only logging-style formats) would implement this interface and not
<xref href="Flowthru.Core.Data.Storage.IFormatRowReader%601" data-throw-if-not-resolved="false"></xref>; in the current first-party suite, every
writer is also a reader and composes both via <xref href="Flowthru.Core.Data.Storage.IFormatSerializer%601" data-throw-if-not-resolved="false"></xref>.

 [IFormatSerializer<TRow\>](Flowthru.Core.Data.Storage.IFormatSerializer\-1.md)

Format extension that supports both reading and writing — the composition of
<xref href="Flowthru.Core.Data.Storage.IFormatRowReader%601" data-throw-if-not-resolved="false"></xref> and <xref href="Flowthru.Core.Data.Storage.IFormatRowWriter%601" data-throw-if-not-resolved="false"></xref>.

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

