# <a id="Flowthru_Extensions_GQL_Data"></a> Namespace Flowthru.Extensions.GQL.Data

### Classes

 [GqlItemFactory.Enumerable](Flowthru.Extensions.GQL.Data.GqlItemFactory.Enumerable.md)

Factory methods for <xref href="Flowthru.Core.Data.Item%601" data-throw-if-not-resolved="false"></xref> backed by a collection GraphQL query.

 [GqlItemFactory](Flowthru.Extensions.GQL.Data.GqlItemFactory.md)

Factory methods for creating collection GQL catalog entries.

 [OffsetPaginationStrategy<TResult, T\>](Flowthru.Extensions.GQL.Data.OffsetPaginationStrategy\-2.md)

Offset-based pagination strategy. Calls the query function with advancing offsets
until all items indicated by <code>getTotal</code> have been fetched.

 [PageInfo](Flowthru.Extensions.GQL.Data.PageInfo.md)

Pagination metadata returned by a Relay-style GraphQL connection.

 [Pagination](Flowthru.Extensions.GQL.Data.Pagination.md)

Factory for creating pagination strategies for paginated GQL catalog entries.

 [PaginationStrategy<TResult, T\>](Flowthru.Extensions.GQL.Data.PaginationStrategy\-2.md)

Defines the pagination strategy used by a paginated GQL catalog entry.

 [RelayPaginationStrategy<TResult, T\>](Flowthru.Extensions.GQL.Data.RelayPaginationStrategy\-2.md)

Relay cursor-based pagination strategy. Calls the query function with advancing
cursors until <code>HasNextPage</code> is false.

 [GqlItemFactory.Single](Flowthru.Extensions.GQL.Data.GqlItemFactory.Single.md)

Factory methods for <xref href="Flowthru.Core.Data.Item%601" data-throw-if-not-resolved="false"></xref> backed by a single-item GraphQL query.

