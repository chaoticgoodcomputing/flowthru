using Flowthru.Core.Data.Validation;

namespace Flowthru.Core.Tests.Validation;

/// <summary>
/// Tests for <see cref="ValidationResult.AddError"/> (internal API) and the exception-message
/// composition path through <see cref="ValidationException.BuildMessage"/>. Confirms the
/// structured-fields composition that Phase 5 left intact (the deleted ToString overrides
/// turned out to be dead, so the existing composition logic handles message formatting
/// directly from <see cref="ValidationError"/> properties).
/// </summary>
[TestFixture]
[Category("Validation")]
public class ValidationResultExceptionCompositionTests
{
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
  public void ValidationException_Message_ComposesFromStructuredFields()
  {
    var result = new ValidationResult(
      new[]
      {
        new ValidationError(
          "ratings.csv",
          ValidationErrorType.NotFound,
          "File not found",
          "Path: /data/ratings.csv"
        ),
        new ValidationError(
          "shuttles.csv",
          ValidationErrorType.SchemaMismatch,
          "Column 'price' not found",
          "Expected columns: id, name, price"
        ),
      }
    );

    var ex = new ValidationException(result);

    // Each structured field appears in the composed message
    Assert.That(ex.Message, Contains.Substring("2 error(s)"));
    Assert.That(ex.Message, Contains.Substring("ratings.csv"));
    Assert.That(ex.Message, Contains.Substring("NotFound"));
    Assert.That(ex.Message, Contains.Substring("File not found"));
    Assert.That(ex.Message, Contains.Substring("/data/ratings.csv"));
    Assert.That(ex.Message, Contains.Substring("shuttles.csv"));
    Assert.That(ex.Message, Contains.Substring("SchemaMismatch"));
    Assert.That(ex.Message, Contains.Substring("Column 'price' not found"));
  }

  [Test]
  public void ValidationException_GroupsErrorsByCatalogKey()
  {
    var result = new ValidationResult(
      new[]
      {
        new ValidationError("item-A", ValidationErrorType.NotFound, "A missing"),
        new ValidationError("item-A", ValidationErrorType.SchemaMismatch, "A bad schema"),
        new ValidationError("item-B", ValidationErrorType.NotFound, "B missing"),
      }
    );

    var ex = new ValidationException(result);

    // item-A header appears once; both A errors fall under it
    var msg = ex.Message;
    var firstA = msg.IndexOf("item-A");
    var lastA = msg.LastIndexOf("item-A");
    Assert.That(firstA, Is.EqualTo(lastA), "item-A should appear exactly once as a group header.");
  }

  [Test]
  public void ThrowIfInvalid_NoErrors_DoesNothing()
  {
    var result = ValidationResult.Success();
    Assert.DoesNotThrow(() => result.ThrowIfInvalid());
  }

  [Test]
  public void ThrowIfInvalid_WithErrors_ThrowsValidationException()
  {
    var result = ValidationResult.Failure("x", ValidationErrorType.NotFound, "missing");

    Assert.That(() => result.ThrowIfInvalid(), Throws.TypeOf<ValidationException>());
  }
}
