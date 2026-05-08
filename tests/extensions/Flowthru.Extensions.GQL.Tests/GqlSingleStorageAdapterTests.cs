using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Gql;
using Flowthru.Extensions.GQL.Tests.Fixtures;
using Flowthru.Prelude;
using StrawberryShake;

namespace Flowthru.Extensions.GQL.Tests;

/// <summary>
/// Unit tests for <see cref="GqlSingleStorageAdapter{TResult,T}"/> —
/// single-item GraphQL adapter with optional mutation support.
/// </summary>
[TestFixture]
public class GqlSingleStorageAdapterTests
{
  private static GqlSingleStorageAdapter<TestSingleResult, TestUser> ReadOnly(
    TestUser? user = null,
    bool allowEmptyData = false
  ) =>
    new(
      label: "user",
      queryFunc: _ => Task.FromResult<IOperationResult<TestSingleResult>>(
        // Null user → null envelope (Data is null), consistent with the
        // adapter's "no data" path; otherwise wrap in a populated envelope.
        user is null
          ? StubOperationResult<TestSingleResult>.EmptyData()
          : StubOperationResult<TestSingleResult>.Success(new TestSingleResult { User = user })
      ),
      selectData: r => r.User!,
      allowEmptyData: allowEmptyData
    );

  // ── Traits ────────────────────────────────────────────────────────────

  [Test]
  public void Traits_NoMutation_IsReadOnly()
  {
    var adapter = ReadOnly(new TestUser { Id = 1, Name = "Alice" });
    Assert.That(adapter.Traits.CanWrite, Is.False);
    Assert.That(adapter.Traits.CanRead, Is.True);
    Assert.That(adapter.Traits.IsPersistent, Is.False);
  }

  [Test]
  public void Traits_WithMutation_IsReadWrite()
  {
    var adapter = new GqlSingleStorageAdapter<TestSingleResult, TestUser>(
      label: "user",
      queryFunc: _ => Task.FromResult<IOperationResult<TestSingleResult>>(
        StubOperationResult<TestSingleResult>.Success(new TestSingleResult { User = new() { Id = 1, Name = "x" } })
      ),
      selectData: r => r.User!,
      mutationFunc: (_, _) =>
        Task.FromResult<IOperationResult>(StubOperationResult<TestSingleResult>.Success(new()))
    );
    Assert.That(adapter.Traits.CanWrite, Is.True);
  }

  // ── Load ──────────────────────────────────────────────────────────────

  [Test]
  public async Task Load_SuccessfulQuery_ReturnsProjectedData()
  {
    var adapter = ReadOnly(new TestUser { Id = 42, Name = "Alice" });
    var result = await adapter.Load().Run();

    Assert.That(result, Is.InstanceOf<EffResult<TestUser>.Success>());
    var success = (EffResult<TestUser>.Success)result;
    Assert.That(success.Value.Id, Is.EqualTo(42));
    Assert.That(success.Value.Name, Is.EqualTo("Alice"));
  }

  [Test]
  public async Task Load_QueryReturnsErrors_FailsWithExternalError()
  {
    var adapter = new GqlSingleStorageAdapter<TestSingleResult, TestUser>(
      label: "user",
      queryFunc: _ => Task.FromResult<IOperationResult<TestSingleResult>>(
        StubOperationResult<TestSingleResult>.WithErrors("schema mismatch")
      ),
      selectData: r => r.User!
    );

    var result = await adapter.Load().Run();
    Assert.That(result, Is.InstanceOf<EffResult<TestUser>.Failure>());
  }

  [Test]
  public async Task Load_NullDataNoErrors_FailsWithExternalError()
  {
    var adapter = ReadOnly(user: null);
    var result = await adapter.Load().Run();
    Assert.That(result, Is.InstanceOf<EffResult<TestUser>.Failure>(),
      "Null data with no GraphQL errors should surface as a typed failure (not throw).");
  }

  // ── Save ──────────────────────────────────────────────────────────────

  [Test]
  public async Task Save_NoMutation_FailsWithExternalError()
  {
    var adapter = ReadOnly(new TestUser { Id = 1, Name = "x" });
    var result = await adapter.Save(new TestUser { Id = 99, Name = "y" }).Run();
    Assert.That(result, Is.InstanceOf<EffResult<FlowUnit>.Failure>(),
      "Save on a read-only GQL adapter should fail rather than throw.");
  }

  [Test]
  public async Task Save_WithMutation_RoundtripsThroughDelegate()
  {
    TestUser? captured = null;
    var adapter = new GqlSingleStorageAdapter<TestSingleResult, TestUser>(
      label: "user",
      queryFunc: _ => Task.FromResult<IOperationResult<TestSingleResult>>(
        StubOperationResult<TestSingleResult>.Success(new TestSingleResult { User = new() { Id = 1, Name = "x" } })
      ),
      selectData: r => r.User!,
      mutationFunc: (data, _) =>
      {
        captured = data;
        return Task.FromResult<IOperationResult>(StubOperationResult<TestSingleResult>.Success(new()));
      }
    );

    var result = await adapter.Save(new TestUser { Id = 99, Name = "y" }).Run();
    Assert.That(result, Is.InstanceOf<EffResult<FlowUnit>.Success>());
    Assert.That(captured, Is.Not.Null);
    Assert.That(captured!.Id, Is.EqualTo(99));
  }

  // ── Exists ────────────────────────────────────────────────────────────

  [Test]
  public async Task Exists_QuerySucceedsWithData_ReturnsTrue()
  {
    var adapter = ReadOnly(new TestUser { Id = 1, Name = "x" });
    var result = await adapter.Exists().Run();
    Assert.That(((EffResult<bool>.Success)result).Value, Is.True);
  }

  [Test]
  public async Task Exists_QueryThrows_ReturnsFalseInsteadOfPropagating()
  {
    var adapter = new GqlSingleStorageAdapter<TestSingleResult, TestUser>(
      label: "user",
      queryFunc: _ => throw new HttpRequestException("Endpoint unreachable"),
      selectData: r => r.User!
    );

    var result = await adapter.Exists().Run();
    Assert.That(result, Is.InstanceOf<EffResult<bool>.Success>(),
      "Exists() should resolve network failures to Success(false), not propagate.");
    Assert.That(((EffResult<bool>.Success)result).Value, Is.False);
  }

  // ── InspectShallow ────────────────────────────────────────────────────

  [Test]
  public async Task InspectShallow_EndpointUnreachable_ReportsNotFound()
  {
    var adapter = new GqlSingleStorageAdapter<TestSingleResult, TestUser>(
      label: "user",
      queryFunc: _ => throw new HttpRequestException("Endpoint unreachable"),
      selectData: r => r.User!
    );

    var result = await adapter.InspectShallow(0).Run();
    var validation = ((EffResult<ValidationResult>.Success)result).Value;
    Assert.That(validation.HasErrors, Is.True);
    Assert.That(validation.Errors[0].ErrorType, Is.EqualTo(ValidationErrorType.NotFound));
  }

  [Test]
  public async Task InspectShallow_QueryReturnsErrors_ReportsInspectionFailure()
  {
    var adapter = new GqlSingleStorageAdapter<TestSingleResult, TestUser>(
      label: "user",
      queryFunc: _ => Task.FromResult<IOperationResult<TestSingleResult>>(
        StubOperationResult<TestSingleResult>.WithErrors("oops")
      ),
      selectData: r => r.User!
    );

    var result = await adapter.InspectShallow(0).Run();
    var validation = ((EffResult<ValidationResult>.Success)result).Value;
    Assert.That(validation.HasErrors, Is.True);
    Assert.That(validation.Errors[0].ErrorType, Is.EqualTo(ValidationErrorType.InspectionFailure));
  }

  [Test]
  public async Task InspectShallow_NullDataDisallowed_ReportsEmptyDataset()
  {
    var adapter = ReadOnly(user: null, allowEmptyData: false);
    var result = await adapter.InspectShallow(0).Run();
    var validation = ((EffResult<ValidationResult>.Success)result).Value;
    Assert.That(validation.HasErrors, Is.True);
    Assert.That(validation.Errors[0].ErrorType, Is.EqualTo(ValidationErrorType.EmptyDataset));
  }

  [Test]
  public async Task InspectShallow_NullDataAllowed_Succeeds()
  {
    var adapter = ReadOnly(user: null, allowEmptyData: true);
    var result = await adapter.InspectShallow(0).Run();
    var validation = ((EffResult<ValidationResult>.Success)result).Value;
    Assert.That(validation.IsValid, Is.True);
  }

  [Test]
  public async Task InspectShallow_SuccessfulQuery_Succeeds()
  {
    var adapter = ReadOnly(new TestUser { Id = 1, Name = "x" });
    var result = await adapter.InspectShallow(0).Run();
    var validation = ((EffResult<ValidationResult>.Success)result).Value;
    Assert.That(validation.IsValid, Is.True);
  }

  // ── InspectTarget ─────────────────────────────────────────────────────

  [Test]
  public async Task InspectTarget_AlwaysSucceeds()
  {
    var adapter = ReadOnly(new TestUser { Id = 1, Name = "x" });
    var result = await adapter.InspectTarget().Run();
    var validation = ((EffResult<ValidationResult>.Success)result).Value;
    Assert.That(validation.IsValid, Is.True,
      "Mutation targets cannot be probed without side effects.");
  }
}
