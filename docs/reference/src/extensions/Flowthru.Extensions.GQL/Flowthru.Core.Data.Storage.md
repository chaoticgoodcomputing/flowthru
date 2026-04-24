# <a id="Flowthru_Core_Data_Storage"></a> Namespace Flowthru.Core.Data.Storage

### Classes

 [GqlEnumerableStorageAdapter<TResult, T\>](Flowthru.Core.Data.Storage.GqlEnumerableStorageAdapter\-2.md)

Storage adapter for a collection GraphQL query using a StrawberryShake client.
Supports both non-paginated queries (server returns all results in one response) and
paginated queries via <xref href="Flowthru.Extensions.GQL.Data.RelayPaginationStrategy%602" data-throw-if-not-resolved="false"></xref> or
<xref href="Flowthru.Extensions.GQL.Data.OffsetPaginationStrategy%602" data-throw-if-not-resolved="false"></xref>.

 [GqlQueryStorageAdapter<TResult, T\>](Flowthru.Core.Data.Storage.GqlQueryStorageAdapter\-2.md)

Storage adapter that holds a GqlQuery&lt;TResult,T&gt; handle.

 [GqlQueryStorageAdapter<TFilter, TResult, T\>](Flowthru.Core.Data.Storage.GqlQueryStorageAdapter\-3.md)

Storage adapter that holds a GqlQuery&lt;TFilter,TResult,T&gt; handle.

 [GqlStorageAdapter<TResult, T\>](Flowthru.Core.Data.Storage.GqlStorageAdapter\-2.md)

Storage adapter for a single-item GraphQL query using a StrawberryShake client.

