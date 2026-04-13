using Flowthru.Core.Data.Storage;
using Flowthru.Extensions.GQL.Data;
using StrawberryShake;

namespace Flowthru.Extensions.GQL.Tests;

[TestFixture]
public class GqlEnumerableStorageAdapterTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Non-paginated Load
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Load_NonPaged_ReturnsSelectedCollection()
    {
        var users = new[] { new TestUser { Id = 1 }, new TestUser { Id = 2 } };
        var adapter = new GqlEnumerableStorageAdapter<TestPagedResult, TestUser>(
            label: "test",
            queryFunc: ct =>
                Task.FromResult<IOperationResult<TestPagedResult>>(
                    StubOperationResult<TestPagedResult>.Success(
                        new TestPagedResult { Nodes = users }
                    )
                ),
            selectData: r => r.Nodes
        );

        var result = (await adapter.Load().Run()).ToList();

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].Id, Is.EqualTo(1));
    }

    [Test]
    public void Load_NonPaged_QueryWithErrors_ThrowsGraphQLClientException()
    {
        var adapter = new GqlEnumerableStorageAdapter<TestPagedResult, TestUser>(
            label: "test",
            queryFunc: ct =>
                Task.FromResult<IOperationResult<TestPagedResult>>(
                    StubOperationResult<TestPagedResult>.WithErrors("Unauthorized")
                ),
            selectData: r => r.Nodes
        );

        Assert.ThrowsAsync<GraphQLClientException>(async () => await adapter.Load().Run());
    }

    [Test]
    public void Load_NonPaged_EmptyCollectionAllowEmptyFalse_ThrowsInvalidOperationException()
    {
        var adapter = new GqlEnumerableStorageAdapter<TestPagedResult, TestUser>(
            label: "test",
            queryFunc: ct =>
                Task.FromResult<IOperationResult<TestPagedResult>>(
                    StubOperationResult<TestPagedResult>.Success(
                        new TestPagedResult { Nodes = Array.Empty<TestUser>() }
                    )
                ),
            selectData: r => r.Nodes,
            allowEmptyData: false
        );

        Assert.ThrowsAsync<InvalidOperationException>(async () => await adapter.Load().Run());
    }

    [Test]
    public async Task Load_NonPaged_EmptyCollectionAllowEmptyTrue_ReturnsEmpty()
    {
        var adapter = new GqlEnumerableStorageAdapter<TestPagedResult, TestUser>(
            label: "test",
            queryFunc: ct =>
                Task.FromResult<IOperationResult<TestPagedResult>>(
                    StubOperationResult<TestPagedResult>.Success(
                        new TestPagedResult { Nodes = Array.Empty<TestUser>() }
                    )
                ),
            selectData: r => r.Nodes,
            allowEmptyData: true
        );

        var result = (await adapter.Load().Run()).ToList();

        Assert.That(result, Is.Empty);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Relay paginated Load
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Load_RelayPaged_IteratesTwoPages_ReturnsFlatList()
    {
        // Page 1: items 1-2, hasNextPage = true
        // Page 2: items 3-4, hasNextPage = false
        var callCount = 0;
        var pagedQueryFunc = (string? cursor, int pageSize, CancellationToken ct) =>
        {
            callCount++;
            var page =
                callCount == 1
                    ? new TestPagedResult
                    {
                        Nodes = new[] { new TestUser { Id = 1 }, new TestUser { Id = 2 } },
                        PageInfo = new StubPageInfo
                        {
                            HasNextPage = true,
                            EndCursor = "cursor-1",
                        },
                    }
                    : new TestPagedResult
                    {
                        Nodes = new[] { new TestUser { Id = 3 }, new TestUser { Id = 4 } },
                        PageInfo = new StubPageInfo { HasNextPage = false, EndCursor = null },
                    };

            return Task.FromResult<IOperationResult<TestPagedResult>>(
                StubOperationResult<TestPagedResult>.Success(page)
            );
        };

        var pagination = Pagination.Relay<TestPagedResult, TestUser>(
            getNodes: r => r.Nodes,
            getPageInfo: r =>
                r.PageInfo is { } pi ? new PageInfo(pi.HasNextPage, pi.EndCursor) : null
        );

        var adapter = new GqlEnumerableStorageAdapter<TestPagedResult, TestUser>(
            label: "test",
            pagedQueryFunc: pagedQueryFunc,
            pagination: pagination,
            pageSize: 2
        );

        var result = (await adapter.Load().Run()).ToList();

        Assert.That(result, Has.Count.EqualTo(4));
        Assert.That(callCount, Is.EqualTo(2));
        Assert.That(result.Select(u => u.Id), Is.EqualTo(new[] { 1, 2, 3, 4 }));
    }

    [Test]
    public void Load_RelayPaged_PageWithErrors_ThrowsWithPageContext()
    {
        var callCount = 0;
        var pagedQueryFunc = (string? cursor, int pageSize, CancellationToken ct) =>
        {
            callCount++;
            IOperationResult<TestPagedResult> result =
                callCount == 1
                    ? StubOperationResult<TestPagedResult>.Success(
                        new TestPagedResult
                        {
                            Nodes = new[] { new TestUser { Id = 1 } },
                            PageInfo = new StubPageInfo
                            {
                                HasNextPage = true,
                                EndCursor = "cursor-1",
                            },
                        }
                    )
                    : StubOperationResult<TestPagedResult>.WithErrors("Rate limit exceeded");

            return Task.FromResult(result);
        };

        var pagination = Pagination.Relay<TestPagedResult, TestUser>(
            getNodes: r => r.Nodes,
            getPageInfo: r =>
                r.PageInfo is { } pi ? new PageInfo(pi.HasNextPage, pi.EndCursor) : null
        );

        var adapter = new GqlEnumerableStorageAdapter<TestPagedResult, TestUser>(
            label: "test",
            pagedQueryFunc: pagedQueryFunc,
            pagination: pagination,
            pageSize: 1
        );

        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await adapter.Load().Run()
        );
        Assert.That(ex!.Message, Does.Contain("page 2"));
        Assert.That(ex.Message, Does.Contain("cursor-1"));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Offset paginated Load
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Load_OffsetPaged_IteratesTwoPages_ReturnsFlatList()
    {
        var pagedQueryFunc = (int offset, int limit, CancellationToken ct) =>
        {
            var items =
                offset == 0
                    ? new TestPagedResult
                    {
                        Nodes = new[] { new TestUser { Id = 1 }, new TestUser { Id = 2 } },
                        Total = 3,
                    }
                    : new TestPagedResult
                    {
                        Nodes = new[] { new TestUser { Id = 3 } },
                        Total = 3,
                    };

            return Task.FromResult<IOperationResult<TestPagedResult>>(
                StubOperationResult<TestPagedResult>.Success(items)
            );
        };

        var pagination = Pagination.Offset<TestPagedResult, TestUser>(
            getItems: r => r.Nodes,
            getTotal: r => r.Total
        );

        var adapter = new GqlEnumerableStorageAdapter<TestPagedResult, TestUser>(
            label: "test",
            pagedQueryFunc: pagedQueryFunc,
            pagination: pagination,
            pageSize: 2
        );

        var result = (await adapter.Load().Run()).ToList();

        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result.Select(u => u.Id), Is.EqualTo(new[] { 1, 2, 3 }));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Traits
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    public void Traits_CanWriteIsAlwaysFalse()
    {
        var adapter = new GqlEnumerableStorageAdapter<TestPagedResult, TestUser>(
            label: "test",
            queryFunc: ct =>
                Task.FromResult<IOperationResult<TestPagedResult>>(
                    StubOperationResult<TestPagedResult>.Success(new TestPagedResult())
                ),
            selectData: r => r.Nodes
        );

        Assert.That(adapter.Traits.CanWrite, Is.False);
    }

    [Test]
    public void Traits_RequiresNetworkIsAlwaysTrue()
    {
        var adapter = new GqlEnumerableStorageAdapter<TestPagedResult, TestUser>(
            label: "test",
            queryFunc: ct =>
                Task.FromResult<IOperationResult<TestPagedResult>>(
                    StubOperationResult<TestPagedResult>.Success(new TestPagedResult())
                ),
            selectData: r => r.Nodes
        );

        Assert.That(adapter.Traits.RequiresNetwork, Is.True);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // InspectShallow
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task InspectShallow_RelayPaged_EndpointReachable_ReturnsSuccess()
    {
        var pagination = Pagination.Relay<TestPagedResult, TestUser>(
            getNodes: r => r.Nodes,
            getPageInfo: r =>
                r.PageInfo is { } pi ? new PageInfo(pi.HasNextPage, pi.EndCursor) : null
        );

        var adapter = new GqlEnumerableStorageAdapter<TestPagedResult, TestUser>(
            label: "test",
            pagedQueryFunc: (cursor, pageSize, ct) =>
                Task.FromResult<IOperationResult<TestPagedResult>>(
                    StubOperationResult<TestPagedResult>.Success(
                        new TestPagedResult
                        {
                            Nodes = new[] { new TestUser { Id = 1 } },
                            PageInfo = new StubPageInfo { HasNextPage = false },
                        }
                    )
                ),
            pagination: pagination
        );

        var result = await adapter.InspectShallow(sampleSize: 1).Run();

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task InspectShallow_QueryThrows_ReturnsNotFoundFailure()
    {
        var adapter = new GqlEnumerableStorageAdapter<TestPagedResult, TestUser>(
            label: "test",
            queryFunc: ct => throw new HttpRequestException("Connection refused"),
            selectData: r => r.Nodes
        );

        var result = await adapter.InspectShallow(sampleSize: 1).Run();

        Assert.That(result.HasErrors, Is.True);
        Assert.That(
            result.Errors[0].ErrorType,
            Is.EqualTo(Flowthru.Core.Data.Validation.ValidationErrorType.NotFound)
        );
    }
}
