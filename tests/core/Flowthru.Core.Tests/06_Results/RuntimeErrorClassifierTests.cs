using System.Net.Sockets;
using Flowthru.Core.Results;

namespace Flowthru.Core.Tests.Results;

/// <summary>
/// Tests for <see cref="RuntimeErrorClassifier"/>.
/// </summary>
[TestFixture]
[Category("Results")]
[Category("RuntimeErrorClassifier")]
public class RuntimeErrorClassifierTests
{
  // ─────────────────────────────────────────────────────────────────────────
  // Known external types → ExternalError
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void Classify_HttpRequestException_ReturnsExternalError()
  {
    var result = RuntimeErrorClassifier.Classify(new HttpRequestException("connection refused"));

    Assert.That(result, Is.EqualTo(ErrorClassification.ExternalError));
  }

  [Test]
  public void Classify_SocketException_ReturnsExternalError()
  {
    var result = RuntimeErrorClassifier.Classify(
      new SocketException((int)SocketError.ConnectionRefused)
    );

    Assert.That(result, Is.EqualTo(ErrorClassification.ExternalError));
  }

  [Test]
  public void Classify_IOException_ReturnsExternalError()
  {
    var result = RuntimeErrorClassifier.Classify(new IOException("disk read error"));

    Assert.That(result, Is.EqualTo(ErrorClassification.ExternalError));
  }

  [Test]
  public void Classify_OutOfMemoryException_ReturnsExternalError()
  {
    var result = RuntimeErrorClassifier.Classify(new OutOfMemoryException());

    Assert.That(result, Is.EqualTo(ErrorClassification.ExternalError));
  }

  [Test]
  public void Classify_OperationCanceledException_ReturnsExternalError()
  {
    var result = RuntimeErrorClassifier.Classify(new OperationCanceledException());

    Assert.That(result, Is.EqualTo(ErrorClassification.ExternalError));
  }

  [Test]
  public void Classify_TaskCanceledException_ReturnsExternalError()
  {
    var result = RuntimeErrorClassifier.Classify(new TaskCanceledException());

    Assert.That(result, Is.EqualTo(ErrorClassification.ExternalError));
  }

  [Test]
  public void Classify_TimeoutException_ReturnsExternalError()
  {
    var result = RuntimeErrorClassifier.Classify(new TimeoutException());

    Assert.That(result, Is.EqualTo(ErrorClassification.ExternalError));
  }

  [Test]
  public void Classify_UnauthorizedAccessException_ReturnsExternalError()
  {
    var result = RuntimeErrorClassifier.Classify(new UnauthorizedAccessException());

    Assert.That(result, Is.EqualTo(ErrorClassification.ExternalError));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Subclass of a known external type → ExternalError (tests the base-type walk)
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void Classify_DirectoryNotFoundExceptionInheritsIOException_ReturnsExternalError()
  {
    // DirectoryNotFoundException : IOException
    var result = RuntimeErrorClassifier.Classify(new DirectoryNotFoundException("/missing/path"));

    Assert.That(result, Is.EqualTo(ErrorClassification.ExternalError));
  }

  [Test]
  public void Classify_FileNotFoundExceptionInheritsIOException_ReturnsExternalError()
  {
    // FileNotFoundException : IOException
    var result = RuntimeErrorClassifier.Classify(
      new FileNotFoundException("not found", "data.csv")
    );

    Assert.That(result, Is.EqualTo(ErrorClassification.ExternalError));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // External type wrapped as inner exception → ExternalError
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void Classify_ExternalInnerException_ReturnsExternalError()
  {
    var inner = new IOException("disk error");
    var outer = new InvalidOperationException("wrapped", inner);

    var result = RuntimeErrorClassifier.Classify(outer);

    Assert.That(result, Is.EqualTo(ErrorClassification.ExternalError));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Unknown exception types → PossibleFrameworkBug
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void Classify_InvalidOperationException_ReturnsPossibleFrameworkBug()
  {
    var result = RuntimeErrorClassifier.Classify(new InvalidOperationException("unexpected state"));

    Assert.That(result, Is.EqualTo(ErrorClassification.PossibleFrameworkBug));
  }

  [Test]
  public void Classify_NullReferenceException_ReturnsPossibleFrameworkBug()
  {
    var result = RuntimeErrorClassifier.Classify(new NullReferenceException());

    Assert.That(result, Is.EqualTo(ErrorClassification.PossibleFrameworkBug));
  }

  [Test]
  public void Classify_ArithmeticException_ReturnsPossibleFrameworkBug()
  {
    var result = RuntimeErrorClassifier.Classify(new ArithmeticException("overflow"));

    Assert.That(result, Is.EqualTo(ErrorClassification.PossibleFrameworkBug));
  }

  [Test]
  public void Classify_GenericExceptionWithNoInnerException_ReturnsPossibleFrameworkBug()
  {
    var result = RuntimeErrorClassifier.Classify(new Exception("something went wrong"));

    Assert.That(result, Is.EqualTo(ErrorClassification.PossibleFrameworkBug));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // FlowExecutionEscapedException short-circuit
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void Classify_FlowExecutionEscapedException_ReturnsPossibleFrameworkBug_RegardlessOfInner()
  {
    // The marker exception means a runtime failure escaped the FlowResult
    // contract. Even when the underlying cause is an allowlisted type
    // (cancellation), the escape itself is a framework bug — the classifier
    // must short-circuit before walking the inner.
    var leakedCancellation = new FlowExecutionEscapedException(
      "Flow aborted by an unexpected cancellation.",
      new TaskCanceledException()
    );

    var result = RuntimeErrorClassifier.Classify(leakedCancellation);

    Assert.That(result, Is.EqualTo(ErrorClassification.PossibleFrameworkBug));
  }

  [Test]
  public void Classify_FlowExecutionEscapedException_WithExternalInner_StillFrameworkBug()
  {
    // Sanity: even an inner type that's *normally* external (IOException)
    // shouldn't downgrade the classification, since the escape itself is
    // unexpected.
    var escaped = new FlowExecutionEscapedException(
      "Flow execution failed unexpectedly.",
      new IOException("disk read error")
    );

    var result = RuntimeErrorClassifier.Classify(escaped);

    Assert.That(result, Is.EqualTo(ErrorClassification.PossibleFrameworkBug));
  }
}
