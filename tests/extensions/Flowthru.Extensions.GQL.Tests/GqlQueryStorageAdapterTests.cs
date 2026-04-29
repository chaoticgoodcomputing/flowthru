using Flowthru.Core.Data;
using Flowthru.Core.Data.Storage;
using Flowthru.Core.Data.Validation;
using Flowthru.Extensions.GQL.Data;
using StrawberryShake;

namespace Flowthru.Extensions.GQL.Tests;

/// <summary>
/// Tests for <see cref="GqlQueryStorageAdapter{TResult, T}"/> — the deferred-handle adapter
/// for read-only GraphQL queries.
/// </summary>
/// <remarks>
/// This adapter does not fit the kit's <c>StorageAdapterConformance&lt;T&gt;</c> contract
/// because <see cref="IStorageAdapter{T}.Exists"/> is hard-coded to <c>true</c> (the handle
/// is always present at catalog construction time) and <see cref="IStorageAdapter{T}.Save"/>
/// always throws. The kit assumes Exists tracks data presence and that round-trip is
/// well-defined for write-capable adapters; neither holds here. So instead, a vanilla NUnit
/// fixture covers the adapter's actual contract: handle is returned without I/O, Save fails
/// with NotSupported, InspectShallow probes the endpoint, and Exists is consistently true.
/// </remarks>
[TestFixture]
public class GqlQueryStorageAdapterTests
{
  private static Item<GqlQuery<TestPagedResult, TestUser>> CreateNonPaged(
    Func<CancellationToken, Task<IOperationResult<TestPagedResult>>> queryFunc
  ) =>
    GqlItemFactory.Query.NonPaged<TestPagedResult, TestUser>(
      label: "test-query",
      queryFunc: queryFunc,
      selectData: r => r.Nodes
    );

  // ── Load returns the handle without I/O ──────────────────────────────────

  [Test]
  public async Task Load_ReturnsTheHandleWithoutInvokingQuery()
  {
    var queryInvocations = 0;
    var item = CreateNonPaged(ct =>
    {
      queryInvocations++;
      return Task.FromResult<IOperationResult<TestPagedResult>>(
        StubOperationResult<TestPagedResult>.Success(new TestPagedResult { Nodes = Array.Empty<TestUser>() })
      );
    });

    var handle = await item.Load().Run();

    Assert.That(handle, Is.Not.Null, "Load should yield the deferred query handle.");
    Assert.That(queryInvocations, Is.Zero, "Load should not invoke the query function.");
  }

  // ── Save always fails ─────────────────────────────────────────────────────

  [Test]
  public void Save_AlwaysThrowsNotSupportedException()
  {
    var item = CreateNonPaged(ct =>
      Task.FromResult<IOperationResult<TestPagedResult>>(
        StubOperationResult<TestPagedResult>.Success(new TestPagedResult())
      )
    );

    var dummyHandle = item.Load().Run().GetAwaiter().GetResult();

    Assert.ThrowsAsync<NotSupportedException>(async () => await item.Save(dummyHandle).Run());
  }

  // ── Exists is hard-coded true ─────────────────────────────────────────────

  [Test]
  public async Task Exists_AlwaysReturnsTrue()
  {
    var item = CreateNonPaged(_ =>
      throw new InvalidOperationException("Exists must not invoke queryFunc")
    );

    var exists = await item.Exists().Run();

    Assert.That(exists, Is.True);
  }

  // ── InspectShallow probes the endpoint ────────────────────────────────────

  [Test]
  public async Task InspectShallow_ReachableEndpoint_ReturnsSuccess()
  {
    var item = CreateNonPaged(ct =>
      Task.FromResult<IOperationResult<TestPagedResult>>(
        StubOperationResult<TestPagedResult>.Success(
          new TestPagedResult { Nodes = new[] { new TestUser { Id = 1, Name = "Alice" } } }
        )
      )
    );

    var result = await item.InspectShallow(sampleSize: 1).Run();

    Assert.That(result.IsValid, Is.True);
  }

  [Test]
  public async Task InspectShallow_UnreachableEndpoint_FailsWithExceptionOrNotFound()
  {
    var item = CreateNonPaged(_ => throw new HttpRequestException("Endpoint unreachable"));

    var result = await item.InspectShallow(sampleSize: 1).Run();

    Assert.That(result.IsValid, Is.False);
    // The adapter classifies failures as either NotFound (probe returned false) or
    // captures the exception message via ValidationResult.FromException.
    Assert.That(
      result.Errors,
      Has.Some.Matches<ValidationError>(e =>
        e.ErrorType == ValidationErrorType.NotFound
        || e.ErrorType == ValidationErrorType.InspectionFailure
      )
    );
  }

  // ── InspectTarget is trivially valid (read-only adapter) ─────────────────

  [Test]
  public async Task InspectTarget_AlwaysSucceedsTrivially()
  {
    var item = CreateNonPaged(ct =>
      Task.FromResult<IOperationResult<TestPagedResult>>(
        StubOperationResult<TestPagedResult>.Success(new TestPagedResult())
      )
    );

    var result = await item.InspectTarget().Run();

    Assert.That(result.IsValid, Is.True);
  }
}
