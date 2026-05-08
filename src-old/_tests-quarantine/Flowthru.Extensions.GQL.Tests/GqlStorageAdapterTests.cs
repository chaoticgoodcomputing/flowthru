using Flowthru.Core.Data.Storage;
using Flowthru.Extensions.GQL.Data;
using StrawberryShake;

namespace Flowthru.Extensions.GQL.Tests;

[TestFixture]
public class GqlStorageAdapterTests
{
  // ─────────────────────────────────────────────────────────────────────────
  // Load
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task Load_SuccessfulQuery_ReturnsSelectedData()
  {
    var expected = new TestUser { Id = 1, Name = "Alice" };
    var adapter = new GqlStorageAdapter<TestPagedResult, TestUser>(
      label: "test",
      queryFunc: ct =>
        Task.FromResult<IOperationResult<TestPagedResult>>(
          StubOperationResult<TestPagedResult>.Success(
            new TestPagedResult { Nodes = new[] { expected } }
          )
        ),
      selectData: r => r.Nodes![0]
    );

    var result = await adapter.Load().Run();

    Assert.That(result.Id, Is.EqualTo(1));
    Assert.That(result.Name, Is.EqualTo("Alice"));
  }

  [Test]
  public void Load_QueryWithErrors_ThrowsGraphQLClientException()
  {
    var adapter = new GqlStorageAdapter<TestPagedResult, TestUser>(
      label: "test",
      queryFunc: ct =>
        Task.FromResult<IOperationResult<TestPagedResult>>(
          StubOperationResult<TestPagedResult>.WithErrors("Unauthorized")
        ),
      selectData: r => r.Nodes![0]
    );

    Assert.ThrowsAsync<GraphQLClientException>(async () => await adapter.Load().Run());
  }

  [Test]
  public void Load_NullData_ThrowsInvalidOperationException()
  {
    var adapter = new GqlStorageAdapter<TestPagedResult, TestUser>(
      label: "test",
      queryFunc: ct =>
        Task.FromResult<IOperationResult<TestPagedResult>>(
          StubOperationResult<TestPagedResult>.Success(null!)
        ),
      selectData: r => r.Nodes![0]
    );

    Assert.ThrowsAsync<InvalidOperationException>(async () => await adapter.Load().Run());
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Save
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void Save_NoMutationDelegate_ThrowsInvalidOperationException()
  {
    var adapter = new GqlStorageAdapter<TestPagedResult, TestUser>(
      label: "test",
      queryFunc: ct =>
        Task.FromResult<IOperationResult<TestPagedResult>>(
          StubOperationResult<TestPagedResult>.Success(new TestPagedResult())
        ),
      selectData: r => r.Nodes![0]
    );

    Assert.ThrowsAsync<InvalidOperationException>(
      async () => await adapter.Save(new TestUser()).Run()
    );
  }

  [Test]
  public async Task Save_WithMutationDelegate_InvokesMutationAndSucceeds()
  {
    var mutationCalled = false;
    var adapter = new GqlStorageAdapter<TestPagedResult, TestUser>(
      label: "test",
      queryFunc: ct =>
        Task.FromResult<IOperationResult<TestPagedResult>>(
          StubOperationResult<TestPagedResult>.Success(new TestPagedResult())
        ),
      selectData: r => r.Nodes![0],
      mutationFunc: (data, ct) =>
      {
        mutationCalled = true;
        return Task.FromResult<IOperationResult>(
          StubOperationResult<TestPagedResult>.Success(new TestPagedResult())
        );
      }
    );

    await adapter.Save(new TestUser { Id = 1 }).Run();

    Assert.That(mutationCalled, Is.True);
  }

  [Test]
  public void Save_MutationReturnsErrors_ThrowsGraphQLClientException()
  {
    var adapter = new GqlStorageAdapter<TestPagedResult, TestUser>(
      label: "test",
      queryFunc: ct =>
        Task.FromResult<IOperationResult<TestPagedResult>>(
          StubOperationResult<TestPagedResult>.Success(new TestPagedResult())
        ),
      selectData: r => r.Nodes![0],
      mutationFunc: (data, ct) =>
        Task.FromResult<IOperationResult>(
          StubOperationResult<TestPagedResult>.WithErrors("Forbidden")
        )
    );

    Assert.ThrowsAsync<GraphQLClientException>(
      async () => await adapter.Save(new TestUser()).Run()
    );
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Traits
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void Traits_NoMutationDelegate_CanWriteIsFalse()
  {
    var adapter = new GqlStorageAdapter<TestPagedResult, TestUser>(
      label: "test",
      queryFunc: ct =>
        Task.FromResult<IOperationResult<TestPagedResult>>(
          StubOperationResult<TestPagedResult>.Success(new TestPagedResult())
        ),
      selectData: r => r.Nodes![0]
    );

    Assert.That(adapter.Traits.CanWrite, Is.False);
  }

  [Test]
  public void Traits_WithMutationDelegate_CanWriteIsTrue()
  {
    var adapter = new GqlStorageAdapter<TestPagedResult, TestUser>(
      label: "test",
      queryFunc: ct =>
        Task.FromResult<IOperationResult<TestPagedResult>>(
          StubOperationResult<TestPagedResult>.Success(new TestPagedResult())
        ),
      selectData: r => r.Nodes![0],
      mutationFunc: (data, ct) =>
        Task.FromResult<IOperationResult>(
          StubOperationResult<TestPagedResult>.Success(new TestPagedResult())
        )
    );

    Assert.That(adapter.Traits.CanWrite, Is.True);
  }

  [Test]
  public void Traits_RequiresNetworkIsAlwaysTrue()
  {
    var adapter = new GqlStorageAdapter<TestPagedResult, TestUser>(
      label: "test",
      queryFunc: ct =>
        Task.FromResult<IOperationResult<TestPagedResult>>(
          StubOperationResult<TestPagedResult>.Success(new TestPagedResult())
        ),
      selectData: r => r.Nodes![0]
    );

    Assert.That(adapter.Traits.RequiresNetwork, Is.True);
  }

  // ─────────────────────────────────────────────────────────────────────────
  // InspectShallow
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task InspectShallow_SuccessfulQuery_ReturnsSuccess()
  {
    var adapter = new GqlStorageAdapter<TestPagedResult, TestUser>(
      label: "test",
      queryFunc: ct =>
        Task.FromResult<IOperationResult<TestPagedResult>>(
          StubOperationResult<TestPagedResult>.Success(new TestPagedResult())
        ),
      selectData: r => r.Nodes![0]
    );

    var result = await adapter.InspectShallow(sampleSize: 1).Run();

    Assert.That(result.IsValid, Is.True);
  }

  [Test]
  public async Task InspectShallow_QueryThrows_ReturnsNotFoundFailure()
  {
    var adapter = new GqlStorageAdapter<TestPagedResult, TestUser>(
      label: "test",
      queryFunc: ct => throw new HttpRequestException("Connection refused"),
      selectData: r => r.Nodes![0]
    );

    var result = await adapter.InspectShallow(sampleSize: 1).Run();

    Assert.That(result.HasErrors, Is.True);
    Assert.That(
      result.Errors[0].ErrorType,
      Is.EqualTo(Flowthru.Core.Data.Validation.ValidationErrorType.NotFound)
    );
  }

  [Test]
  public async Task InspectShallow_QueryReturnsErrors_ReturnsInspectionFailure()
  {
    var adapter = new GqlStorageAdapter<TestPagedResult, TestUser>(
      label: "test",
      queryFunc: ct =>
        Task.FromResult<IOperationResult<TestPagedResult>>(
          StubOperationResult<TestPagedResult>.WithErrors("Unauthorized")
        ),
      selectData: r => r.Nodes![0]
    );

    var result = await adapter.InspectShallow(sampleSize: 1).Run();

    Assert.That(result.HasErrors, Is.True);
    Assert.That(
      result.Errors[0].ErrorType,
      Is.EqualTo(Flowthru.Core.Data.Validation.ValidationErrorType.InspectionFailure)
    );
  }

  [Test]
  public async Task InspectShallow_NullDataAllowEmptyFalse_ReturnsEmptyDatasetFailure()
  {
    var adapter = new GqlStorageAdapter<TestPagedResult, TestUser>(
      label: "test",
      queryFunc: ct =>
        Task.FromResult<IOperationResult<TestPagedResult>>(
          StubOperationResult<TestPagedResult>.Success(null!)
        ),
      selectData: r => r.Nodes![0],
      allowEmptyData: false
    );

    var result = await adapter.InspectShallow(sampleSize: 1).Run();

    Assert.That(result.HasErrors, Is.True);
    Assert.That(
      result.Errors[0].ErrorType,
      Is.EqualTo(Flowthru.Core.Data.Validation.ValidationErrorType.EmptyDataset)
    );
  }

  [Test]
  public async Task InspectShallow_NullDataAllowEmptyTrue_ReturnsSuccess()
  {
    var adapter = new GqlStorageAdapter<TestPagedResult, TestUser>(
      label: "test",
      queryFunc: ct =>
        Task.FromResult<IOperationResult<TestPagedResult>>(
          StubOperationResult<TestPagedResult>.Success(null!)
        ),
      selectData: r => r.Nodes![0],
      allowEmptyData: true
    );

    var result = await adapter.InspectShallow(sampleSize: 1).Run();

    Assert.That(result.IsValid, Is.True);
  }

  // ── InspectTarget ───────────────────────────────────────────────────────

  [Test]
  public async Task InspectTarget_AlwaysReturnsSuccess()
  {
    // GQL mutations cannot be probed without side effects — InspectTarget is a no-op.
    var adapter = new GqlStorageAdapter<TestPagedResult, TestUser>(
      label: "test",
      queryFunc: ct =>
        Task.FromResult<IOperationResult<TestPagedResult>>(
          StubOperationResult<TestPagedResult>.Success(new TestPagedResult())
        ),
      selectData: r => r.Nodes![0]
    );

    var result = await adapter.InspectTarget().Run();

    Assert.That(result.IsValid, Is.True);
  }
}
