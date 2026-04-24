# <a id="Flowthru_Extensions_EFCore_Data"></a> Namespace Flowthru.Extensions.EFCore.Data

### Classes

 [DbQuery<T\>](Flowthru.Extensions.EFCore.Data.DbQuery\-1.md)

A deferred EF Core query handle — analogous to <code>TypedFrame&lt;T&gt;</code> in the Spark extension.

 [DbScope](Flowthru.Extensions.EFCore.Data.DbScope.md)

Identifies which database instance a <xref href="Flowthru.Extensions.EFCore.Data.DbQuery%601" data-throw-if-not-resolved="false"></xref> or
<xref href="Flowthru.Core.Data.Storage.DbQueryStorageAdapter%601" data-throw-if-not-resolved="false"></xref> is associated with,
enabling the fused INSERT-FROM-SELECT save path when source and destination share the same DB.

 [EFCoreItemFactory](Flowthru.Extensions.EFCore.Data.EFCoreItemFactory.md)

Factory methods for creating <xref href="Flowthru.Core.Data.Item%601" data-throw-if-not-resolved="false"></xref> instances with Entity Framework Core storage adapters.
This partial class focuses on single-entity storage; see <xref href="Flowthru.Extensions.EFCore.Data.EFCoreItemFactory.Enumerable" data-throw-if-not-resolved="false"></xref> for
collections of entities.

 [EFCoreItemFactory.Enumerable](Flowthru.Extensions.EFCore.Data.EFCoreItemFactory.Enumerable.md)

Factory methods for creating enumerable <xref href="Flowthru.Core.Data.Item%601" data-throw-if-not-resolved="false"></xref> instances with Entity Framework Core storage adapters.

 [EFCoreItemFactory.Query](Flowthru.Extensions.EFCore.Data.EFCoreItemFactory.Query.md)

Factory methods for <code>IItem&lt;IEnumerable&lt;T&gt;&gt;</code> entries backed by a deferred
<xref href="Flowthru.Extensions.EFCore.Data.DbQuery%601" data-throw-if-not-resolved="false"></xref> handle.

 [EFCoreItemFactory.Single](Flowthru.Extensions.EFCore.Data.EFCoreItemFactory.Single.md)

Factory methods for creating single-entity <xref href="Flowthru.Core.Data.Item%601" data-throw-if-not-resolved="false"></xref> instances with Entity Framework Core storage adapters.

