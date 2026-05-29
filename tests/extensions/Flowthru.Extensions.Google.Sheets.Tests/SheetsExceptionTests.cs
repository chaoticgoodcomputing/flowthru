using Flowthru.Data.Schema;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Sheets;
using Flowthru.Data.Storage.Sheets.Internal;

namespace Flowthru.Extensions.Google.Sheets.Tests;

/// <summary>
/// The neutral gateway-failure taxonomy is pure data the retry layer and the
/// adapter branch on, so its provenance codes, transient/permanent contract, and
/// ceiling-detail messaging are asserted directly — no live <c>SheetsService</c>
/// needed (the live gateway only maps Google's HTTP status onto these types).
/// </summary>
[TestFixture]
public sealed class SheetsExceptionTests
{
  private const string SpreadsheetId = "ss-err";

  // ── SheetsWriteCeilingException (the runtime FTGS1608 ceiling) ────────────

  [TestCase(SheetsWriteCeiling.PayloadTooLarge, "single-batch size")]
  [TestCase(SheetsWriteCeiling.ProcessingTimeout, "processing timeout")]
  public void WriteCeiling_CarriesCodeSpreadsheetAndCeilingSpecificDetail(
    SheetsWriteCeiling ceiling, string expectedDetail)
  {
    var cause = new InvalidOperationException("boom");
    var ex = new SheetsWriteCeilingException(SpreadsheetId, ceiling, cause);

    Assert.Multiple(() =>
    {
      Assert.That(ex.SpreadsheetId, Is.EqualTo(SpreadsheetId));
      Assert.That(ex.Ceiling, Is.EqualTo(ceiling));
      Assert.That(ex.InnerException, Is.SameAs(cause), "the underlying cause is chained");
      Assert.That(ex.Message, Does.Contain(SheetsWriteCeilingException.Code));
      Assert.That(ex.Message, Does.Contain(SpreadsheetId));
      Assert.That(ex.Message, Does.Contain(expectedDetail),
        "each ceiling explains its own actionable cause");
    });
  }

  [Test]
  public void WriteCeiling_IsNotTransient_SoTheRetryLayerWontRetryIt()
  {
    var ex = new SheetsWriteCeilingException(
      SpreadsheetId, SheetsWriteCeiling.PayloadTooLarge, new Exception());
    Assert.That(ex, Is.Not.InstanceOf<SheetsRateLimitException>(),
      "an over-ceiling write fails the same way on retry, so it must be permanent");
  }

  [Test]
  public void WriteCeiling_NullSpreadsheetId_Throws()
  {
    Assert.Throws<ArgumentNullException>(
      () => new SheetsWriteCeilingException(null!, SheetsWriteCeiling.PayloadTooLarge, new Exception()));
  }

  // ── SheetsRateLimitException (the transient 429 shape) ────────────────────

  [Test]
  public void RateLimit_DefaultCtor_HasMessage_AndNoHint()
  {
    var ex = new SheetsRateLimitException();
    Assert.Multiple(() =>
    {
      Assert.That(ex.Message, Is.Not.Empty);
      Assert.That(ex.RetryAfter, Is.Null);
    });
  }

  [Test]
  public void RateLimit_MessageOnly_CarriesMessage_NoHint()
  {
    var ex = new SheetsRateLimitException("slow down");
    Assert.Multiple(() =>
    {
      Assert.That(ex.Message, Is.EqualTo("slow down"));
      Assert.That(ex.RetryAfter, Is.Null);
    });
  }

  [Test]
  public void RateLimit_WithInnerException_ChainsCause_NoHint()
  {
    var cause = new Exception("429");
    var ex = new SheetsRateLimitException("mapped", cause);
    Assert.Multiple(() =>
    {
      Assert.That(ex.InnerException, Is.SameAs(cause));
      Assert.That(ex.RetryAfter, Is.Null);
    });
  }

  [Test]
  public void RateLimit_WithHint_ExposesRetryAfter()
  {
    var ex = new SheetsRateLimitException("wait", TimeSpan.FromSeconds(12));
    Assert.That(ex.RetryAfter, Is.EqualTo(TimeSpan.FromSeconds(12)));
  }

  // ── SheetsSpreadsheetAccessException (the permanent 404/403 shape) ────────

  [TestCase(SheetsSpreadsheetAccessFailure.NotFound)]
  [TestCase(SheetsSpreadsheetAccessFailure.AccessDenied)]
  [TestCase(SheetsSpreadsheetAccessFailure.Unknown)]
  public void Access_CarriesSpreadsheetIdAndFailure(SheetsSpreadsheetAccessFailure failure)
  {
    var ex = new SheetsSpreadsheetAccessException(SpreadsheetId, failure, "msg");
    Assert.Multiple(() =>
    {
      Assert.That(ex.SpreadsheetId, Is.EqualTo(SpreadsheetId));
      Assert.That(ex.Failure, Is.EqualTo(failure));
      Assert.That(ex.Message, Is.EqualTo("msg"));
    });
  }

  [Test]
  public void Access_WithInnerException_ChainsCause()
  {
    var cause = new Exception("403");
    var ex = new SheetsSpreadsheetAccessException(
      SpreadsheetId, SheetsSpreadsheetAccessFailure.AccessDenied, "denied", cause);
    Assert.That(ex.InnerException, Is.SameAs(cause));
  }

  [Test]
  public void Access_NullSpreadsheetId_Throws()
  {
    Assert.Throws<ArgumentNullException>(
      () => new SheetsSpreadsheetAccessException(
        null!, SheetsSpreadsheetAccessFailure.NotFound, "msg"));
    Assert.Throws<ArgumentNullException>(
      () => new SheetsSpreadsheetAccessException(
        null!, SheetsSpreadsheetAccessFailure.NotFound, "msg", new Exception()));
  }

  [Test]
  public void Access_IsNotTransient()
  {
    var ex = new SheetsSpreadsheetAccessException(
      SpreadsheetId, SheetsSpreadsheetAccessFailure.NotFound, "msg");
    Assert.That(ex, Is.Not.InstanceOf<SheetsRateLimitException>(),
      "a missing/forbidden spreadsheet is permanent, not retryable");
  }

  // ── Design-time / constraint: byte[] has no faithful column type ──────────

  public sealed class BlobRow : IFlatSchema
  {
    public byte[] Payload { get; set; } = Array.Empty<byte>();
  }

  [Test]
  public void SchemaBuilder_RejectsByteArrayColumn_WithActionableMessage()
  {
    // A byte[] column has no Sheets column type; create-if-absent must reject it
    // rather than silently stringify a blob. This is the store-model consequence
    // the ADR calls out (one phase later than a design-time constraint, but loud).
    var ex = Assert.Throws<SchemaMismatchException>(
      () => SheetsSchemaBuilder.BuildFromRow<BlobRow>());
    Assert.That(ex!.Message, Does.Contain("byte[]"));
    Assert.That(ex.Message, Does.Contain("Payload"));
  }
}
