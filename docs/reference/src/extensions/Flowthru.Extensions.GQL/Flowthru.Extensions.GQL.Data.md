# <a id="Flowthru_Extensions_GQL_Data"></a> Namespace Flowthru.Extensions.GQL.Data

### Classes

 [GqlItemFactory.Enumerable](Flowthru.Extensions.GQL.Data.GqlItemFactory.Enumerable.md)

Factory methods for <xref href="Flowthru.Core.Data.Item%601" data-throw-if-not-resolved="false"></xref> backed by a collection GraphQL query.

 [GqlItemFactory](Flowthru.Extensions.GQL.Data.GqlItemFactory.md)

Factory methods for creating collection GQL catalog entries.

 [GqlQuery<TResult, T\>](Flowthru.Extensions.GQL.Data.GqlQuery\-2.md)

A deferred GQL query handle — analogous to <code>TypedFrame&lt;T&gt;</code> in the Spark extension.

 [GqlQuery<TFilter, TResult, T\>](Flowthru.Extensions.GQL.Data.GqlQuery\-3.md)

A deferred GQL query handle that supports a typed filter input.

 [OffsetPaginationStrategy<TResult, T\>](Flowthru.Extensions.GQL.Data.OffsetPaginationStrategy\-2.md)

Offset-based pagination strategy. Calls the query function with advancing offsets
until all items indicated by <code>getTotal</code> have been fetched.

 [PageInfo](Flowthru.Extensions.GQL.Data.PageInfo.md)

Pagination metadata returned by a Relay-style GraphQL connection.

 [Pagination](Flowthru.Extensions.GQL.Data.Pagination.md)

Factory for creating pagination strategies for paginated GQL catalog entries.

 [PaginationStrategy<TResult, T\>](Flowthru.Extensions.GQL.Data.PaginationStrategy\-2.md)

Defines the pagination strategy used by a paginated GQL catalog entry.

 [GqlItemFactory.Query](Flowthru.Extensions.GQL.Data.GqlItemFactory.Query.md)

Factory methods for <xref href="Flowthru.Core.Data.Item%601" data-throw-if-not-resolved="false"></xref> backed by a deferred
<xref href="Flowthru.Extensions.GQL.Data.GqlQuery%602" data-throw-if-not-resolved="false"></xref> or <xref href="Flowthru.Extensions.GQL.Data.GqlQuery%603" data-throw-if-not-resolved="false"></xref> handle.

 [RelayPaginationStrategy<TResult, T\>](Flowthru.Extensions.GQL.Data.RelayPaginationStrategy\-2.md)

Relay cursor-based pagination strategy. Calls the query function with advancing
cursors until <code>HasNextPage</code> is false.

 [GqlItemFactory.Single](Flowthru.Extensions.GQL.Data.GqlItemFactory.Single.md)

Factory methods for <xref href="Flowthru.Core.Data.Item%601" data-throw-if-not-resolved="false"></xref> backed by a single-item GraphQL query.

