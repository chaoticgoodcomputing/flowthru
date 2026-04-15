using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;

namespace Flowthru.Analyzers.Tests;

/// <summary>
/// An <see cref="IVerifier"/> implementation compatible with NUnit 4.x.
/// </summary>
/// <remarks>
/// <c>Microsoft.CodeAnalysis.CSharp.Analyzer.Testing.NUnit</c> 1.1.2 ships a
/// <c>NUnitVerifier</c> built against NUnit 3 API, which is binary-incompatible with
/// NUnit 4. This verifier reimplements the same interface against NUnit 4 assertion APIs.
/// </remarks>
internal sealed class NUnit4Verifier : IVerifier
{
  private readonly string _context;

  public NUnit4Verifier()
    : this(string.Empty) { }

  private NUnit4Verifier(string context)
  {
    _context = context;
  }

  public void Empty<T>(string collectionName, IEnumerable<T> collection)
  {
    var list = collection.ToList();
    Assert.That(list, Is.Empty, FormatMessage($"'{collectionName}' should be empty."));
  }

  public void NotEmpty<T>(string collectionName, IEnumerable<T> collection)
  {
    var list = collection.ToList();
    Assert.That(list, Is.Not.Empty, FormatMessage($"'{collectionName}' should not be empty."));
  }

  public void LanguageIsSupported(string language)
  {
    Assert.That(
      language,
      Is.EqualTo("C#"),
      FormatMessage($"Language '{language}' is not supported by this verifier (expected C#).")
    );
  }

  public void Equal<T>(T expected, T actual, string? message = null)
  {
    Assert.That(actual, Is.EqualTo(expected), FormatMessage(message ?? string.Empty));
  }

  public void True(bool assert, string? message = null)
  {
    Assert.That(assert, Is.True, FormatMessage(message ?? string.Empty));
  }

  public void False(bool assert, string? message = null)
  {
    Assert.That(assert, Is.False, FormatMessage(message ?? string.Empty));
  }

  public void SequenceEqual<T>(
    IEnumerable<T> expected,
    IEnumerable<T> actual,
    IEqualityComparer<T>? comparer = null,
    string? message = null
  )
  {
    var expectedList = expected.ToList();
    var actualList = actual.ToList();
    Assert.That(
      actualList,
      Is.EqualTo(expectedList).Using(comparer ?? EqualityComparer<T>.Default),
      FormatMessage(message ?? string.Empty)
    );
  }

  [DoesNotReturn]
  public void Fail(string? message = null)
  {
    Assert.Fail(FormatMessage(message ?? string.Empty));
    throw new InvalidOperationException("unreachable");
  }

  public IVerifier PushContext(string context)
  {
    var combined = string.IsNullOrEmpty(_context) ? context : $"{_context} > {context}";
    return new NUnit4Verifier(combined);
  }

  private string FormatMessage(string message) =>
    string.IsNullOrEmpty(_context) ? message : $"[{_context}] {message}";
}
