using Flowthru.Data.Storage;

namespace Flowthru.Core.Tests.Validation;

/// <summary>
/// Pins the public + internal API surface of <see cref="ValidationResult"/> —
/// the storage-inspection container that adapters return from
/// <see cref="IStorageAdapter{T}.InspectShallow"/> and friends.
/// </summary>
/// <remarks>
/// <para>
/// Ported from the legacy <c>ValidationResultExceptionCompositionTests</c>.
/// The FP rewrite split the old "exception+composition+throw" surface in two:
/// the storage layer now owns a pure <see cref="ValidationResult"/> container
/// (no <c>ThrowIfInvalid</c>, no <c>ValidationException</c>), and the
/// pre-flight pipeline lifts each adapter result into a
/// <c>Validated&lt;PreFlightError, FlowUnit&gt;</c> applicative for
/// accumulation. The structured-message + catalog-key-grouping concerns now
/// live on the <c>PreFlightError</c> closed sum (per-case <c>.Message</c>
/// getter) — covered by the pre-flight-side tests.
/// </para>
/// <para>
/// What remains here is the container-level invariants: error accumulation,
/// merge composition, factory shapes, and the exception-classification step
/// in <see cref="ValidationResult.FromException"/>.
/// </para>
/// </remarks>
[TestFixture]
[Category("Validation")]
public class ValidationResultExceptionCompositionTests
{
  [Test]
  public void Success_HasNoErrors()
  {
    var result = ValidationResult.Success();

    Assert.That(result.IsValid, Is.True);
    Assert.That(result.HasErrors, Is.False);
    Assert.That(result.ErrorCount, Is.EqualTo(0));
    Assert.That(result.Errors, Is.Empty);
  }

  [Test]
  public void Failure_ProducesSingleErrorWithStructuredFields()
  {
    var result = ValidationResult.Failure(
      catalogKey: "ratings.csv",
      errorType: ValidationErrorType.NotFound,
      message: "File not found",
      details: "Path: /data/ratings.csv"
    );

    Assert.That(result.IsValid, Is.False);
    Assert.That(result.HasErrors, Is.True);
    Assert.That(result.ErrorCount, Is.EqualTo(1));

    var error = result.Errors[0];
    Assert.That(error.CatalogKey, Is.EqualTo("ratings.csv"));
    Assert.That(error.ErrorType, Is.EqualTo(ValidationErrorType.NotFound));
    Assert.That(error.Message, Is.EqualTo("File not found"));
    Assert.That(error.Details, Is.EqualTo("Path: /data/ratings.csv"));
  }

  [Test]
  public void AddError_AppendsToErrorsList()
  {
    var result = new ValidationResult();
    var error = new ValidationError(
      "catalog-item",
      ValidationErrorType.NotFound,
      "Test message"
    );

    result.AddError(error);

    Assert.That(result.HasErrors, Is.True);
    Assert.That(result.ErrorCount, Is.EqualTo(1));
    Assert.That(result.Errors[0], Is.SameAs(error));
  }

  [Test]
  public void AddError_NullArgument_ThrowsArgumentNull()
  {
    var result = new ValidationResult();

    Assert.That(() => result.AddError(null!), Throws.ArgumentNullException);
  }

  [Test]
  public void Merge_AppendsErrorsFromOther_PreservingOrder()
  {
    var first = new ValidationResult(new[]
    {
      new ValidationError("item-A", ValidationErrorType.NotFound, "A missing"),
    });
    var second = new ValidationResult(new[]
    {
      new ValidationError("item-A", ValidationErrorType.SchemaMismatch, "A bad schema"),
      new ValidationError("item-B", ValidationErrorType.NotFound, "B missing"),
    });

    first.Merge(second);

    Assert.That(first.ErrorCount, Is.EqualTo(3));
    Assert.That(first.Errors.Select(e => e.Message),
      Is.EqualTo(new[] { "A missing", "A bad schema", "B missing" }));
  }

  [Test]
  public void Merge_NullArgument_ThrowsArgumentNull()
  {
    var result = new ValidationResult();

    Assert.That(() => result.Merge(null!), Throws.ArgumentNullException);
  }

  [Test]
  public void FromException_SchemaMismatchException_ClassifiesAsSchemaMismatch()
  {
    var ex = new SchemaMismatchException("expected col 'price' not found");
    var result = ValidationResult.FromException("shuttles.csv", ex);

    Assert.That(result.HasErrors, Is.True);
    var error = result.Errors.Single();
    Assert.That(error.CatalogKey, Is.EqualTo("shuttles.csv"));
    Assert.That(error.ErrorType, Is.EqualTo(ValidationErrorType.SchemaMismatch));
    Assert.That(error.Message, Is.EqualTo("expected col 'price' not found"));
    // Details carries the full ToString() of the exception for diagnostic context.
    Assert.That(error.Details, Does.Contain("SchemaMismatchException"));
  }

  [Test]
  public void FromException_UnknownException_FallsThroughToInspectionFailure()
  {
    var ex = new InvalidOperationException("adapter went bang");
    var result = ValidationResult.FromException("widget", ex);

    var error = result.Errors.Single();
    Assert.That(error.ErrorType, Is.EqualTo(ValidationErrorType.InspectionFailure),
      "Unrecognised exception types must fall through to InspectionFailure — " +
      "the catch-all category for adapter failures Core can't classify further.");
    Assert.That(error.Message, Is.EqualTo("adapter went bang"));
  }

  [Test]
  public void Constructor_NullErrors_ThrowsArgumentNull()
  {
    Assert.That(() => new ValidationResult(errors: null!), Throws.ArgumentNullException);
  }

  [Test]
  public void Constructor_TakesSnapshotOfErrors_NotLiveReference()
  {
    var mutable = new List<ValidationError>
    {
      new("item-A", ValidationErrorType.NotFound, "first"),
    };
    var result = new ValidationResult(mutable);

    mutable.Add(new ValidationError("item-B", ValidationErrorType.NotFound, "second"));

    Assert.That(result.ErrorCount, Is.EqualTo(1),
      "ValidationResult should snapshot the input enumerable so adapter " +
      "code can mutate the source list without corrupting prior results.");
  }
}
