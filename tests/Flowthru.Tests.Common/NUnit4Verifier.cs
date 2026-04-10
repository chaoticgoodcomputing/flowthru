using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;

namespace Flowthru.Tests.Common;

/// <summary>
/// NUnit 4-compatible <see cref="IVerifier"/> for use with
/// <c>CSharpCodeFixTest&lt;TAnalyzer, TFix, NUnit4Verifier&gt;</c> and
/// <c>CSharpAnalyzerTest&lt;TAnalyzer, NUnit4Verifier&gt;</c>.
/// </summary>
/// <remarks>
/// The <c>NUnitVerifier</c> bundled with <c>Microsoft.CodeAnalysis.*.Testing.NUnit 1.1.2</c>
/// calls <c>Assert.That(ValueTuple, ...)</c> which was removed in NUnit 4.
/// This class replaces it using only stable NUnit 4 assertion overloads.
/// </remarks>
public sealed class NUnit4Verifier : IVerifier
{
  private readonly ImmutableStack<string> _context;

  public NUnit4Verifier()
    : this(ImmutableStack<string>.Empty) { }

  private NUnit4Verifier(ImmutableStack<string> context) => _context = context;

  private string Msg(string? extra) =>
    _context.IsEmpty
      ? extra ?? string.Empty
      : $"[{string.Join(" > ", _context.Reverse())}] {extra}";

  public void Empty<T>(string collectionName, IEnumerable<T> collection) =>
    Assert.That(collection, Is.Empty, Msg($"`{collectionName}` should be empty"));

  public void NotEmpty<T>(string collectionName, IEnumerable<T> collection) =>
    Assert.That(collection.Any(), Is.True, Msg($"`{collectionName}` should not be empty"));

  public void SequenceEqual<T>(
    IEnumerable<T> expected,
    IEnumerable<T> actual,
    IEqualityComparer<T>? comparer = null,
    string? message = null
  )
  {
    var expectedList = expected.ToList();
    var actualList = actual.ToList();
    if (comparer is null)
      Assert.That(actualList, Is.EqualTo(expectedList), Msg(message));
    else
      Assert.That(actualList.SequenceEqual(expectedList, comparer), Is.True, Msg(message));
  }

  public void Equal<T>(T expected, T actual, string? message = null) =>
    Assert.That(actual, Is.EqualTo(expected), Msg(message));

  public void True(bool assert, string? message = null) =>
    Assert.That(assert, Is.True, Msg(message));

  public void False(bool assert, string? message = null) =>
    Assert.That(assert, Is.False, Msg(message));

  public void Fail(string message) => Assert.Fail(Msg(message));

  public void LanguageIsSupported(string language)
  {
    if (language != LanguageNames.CSharp)
      Assert.Fail(Msg($"Language '{language}' is not supported; only C# is."));
  }

  public IVerifier PushContext(string context) => new NUnit4Verifier(_context.Push(context));
}
