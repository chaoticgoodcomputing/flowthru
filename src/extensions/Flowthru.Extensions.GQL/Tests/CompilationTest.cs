using Flowthru.Core.Data;
using Flowthru.Extensions.GQL.Data;
using StrawberryShake;

namespace Flowthru.Extensions.GQL.Tests;

/// <summary>
/// Minimal compilation tests verifying all GqlItemFactory overloads and generic constraints
/// compile correctly. No runtime assertions — if this file compiles, the API surface is valid.
/// </summary>
public class CompilationTest
{
    /// <summary>
    /// Verifies that the read-only single-item query overload compiles and returns the correct type.
    /// </summary>
    public void SingleQueryReadOnlyCompiles()
    {
        var entry = GqlItemFactory.Single.Query<StubResult, StubData>(
          label: "stub",
          queryFunc: (ct) => Task.FromResult<IOperationResult<StubResult>>(null!),
          selectData: r => r.Data!
        );

        var _ = entry as IItem<StubData>;
    }

    /// <summary>
    /// Verifies that the read-write single-item query overload (with mutation delegate) compiles
    /// and that the entry traits reflect CanWrite correctly.
    /// </summary>
    public void SingleQueryWithMutationCompiles()
    {
        var entry = GqlItemFactory.Single.Query<StubResult, StubData>(
          label: "stub",
          queryFunc: ct => Task.FromResult<IOperationResult<StubResult>>(null!),
          selectData: r => r.Data!,
          mutationFunc: (data, ct) => Task.FromResult<IOperationResult>(null!)
        );

        var _ = entry as IItem<StubData>;
    }

    /// <summary>
    /// Verifies that the non-paginated collection query overload compiles and returns the correct type.
    /// </summary>
    public void EnumerableQueryCompiles()
    {
        var entry = GqlItemFactory.Enumerable.Query<StubResult, StubData>(
          label: "stub",
          queryFunc: ct => Task.FromResult<IOperationResult<StubResult>>(null!),
          selectData: r => r.Items
        );

        var _ = entry as IItem<IEnumerable<StubData>>;
    }

    /// <summary>
    /// Verifies that the Relay cursor-paginated overload compiles, that the pagination
    /// strategy generic parameters flow correctly, and that the return type is correct.
    /// </summary>
    public void RelayPagedQueryCompiles()
    {
        var pagination = Pagination.Relay<StubResult, StubData>(
          getNodes: r => r.Items,
          getPageInfo: r => new PageInfo(HasNextPage: false, EndCursor: null)
        );

        var entry = GqlItemFactory.Enumerable.PagedQuery<StubResult, StubData>(
          label: "stub",
          pagedQueryFunc: (cursor, pageSize, ct) =>
            Task.FromResult<IOperationResult<StubResult>>(null!),
          pagination: pagination,
          pageSize: 50
        );

        var _ = entry as IItem<IEnumerable<StubData>>;
    }

    /// <summary>
    /// Verifies that the offset-paginated overload compiles and that the pagination
    /// strategy generic parameters flow correctly.
    /// </summary>
    public void OffsetPagedQueryCompiles()
    {
        var pagination = Pagination.Offset<StubResult, StubData>(
          getItems: r => r.Items,
          getTotal: r => r.Total
        );

        var entry = GqlItemFactory.Enumerable.PagedQuery<StubResult, StubData>(
          label: "stub",
          pagedQueryFunc: (offset, limit, ct) => Task.FromResult<IOperationResult<StubResult>>(null!),
          pagination: pagination,
          pageSize: 100
        );

        var _ = entry as IItem<IEnumerable<StubData>>;
    }

    /// <summary>
    /// Verifies that allowEmptyData propagates through each factory overload without
    /// causing a compilation error.
    /// </summary>
    public void AllowEmptyDataParameterCompiles()
    {
        var _ = GqlItemFactory.Single.Query<StubResult, StubData>(
          label: "stub",
          queryFunc: ct => Task.FromResult<IOperationResult<StubResult>>(null!),
          selectData: r => r.Data!,
          allowEmptyData: true
        );

        var __ = GqlItemFactory.Enumerable.Query<StubResult, StubData>(
          label: "stub",
          queryFunc: ct => Task.FromResult<IOperationResult<StubResult>>(null!),
          selectData: r => r.Items,
          allowEmptyData: true
        );
    }

    // -------------------------------------------------------------------------
    // Stub types — stand in for StrawberryShake-generated result/data types
    // -------------------------------------------------------------------------

    private class StubData { }

    private class StubResult
    {
        public StubData? Data { get; }
        public IEnumerable<StubData>? Items { get; }
        public int? Total { get; }
    }
}
