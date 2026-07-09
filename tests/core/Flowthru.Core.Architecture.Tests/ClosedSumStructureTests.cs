using System.Reflection;
using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Flow;
using Flowthru.Prelude;
using Flowthru.Validation.PreFlight;
using Flowthru.Validation.Runtime;

namespace Flowthru.Core.Architecture.Tests;

/// <summary>
/// Asserts that every Flowthru closed sum is structured the way
/// FT0001 (and downstream pattern-matching consumers) expect:
/// abstract record umbrella + private constructor + at least two
/// nested <c>sealed record</c> derivations. The closed-sum invariant
/// per §2.5 — "no derived case can be added outside this file" — is
/// the property the structure is encoding.
/// </summary>
[TestFixture]
public class ClosedSumStructureTests
{
  private static IEnumerable<Type> KnownClosedSums => new[]
  {
    typeof(PreFlightError),
    typeof(RuntimeError),
    typeof(ServiceDependency),
    typeof(StepResult),
    typeof(EffResult<>),
    typeof(Validated<,>),
    typeof(DependencyAnalyzer.Result),
    typeof(ByteLocation),
  };

  [Test]
  [TestCaseSource(nameof(KnownClosedSums))]
  public void IsAbstract(Type type)
  {
    Assert.That(type.IsAbstract, Is.True,
      $"{type.Name} should be abstract — only nested sealed records can instantiate the umbrella.");
  }

  [Test]
  [TestCaseSource(nameof(KnownClosedSums))]
  public void IsRecord(Type type)
  {
    // Records always emit a synthetic Equals(<Type>) method.
    var hasRecordEquals = type
      .GetMethods(BindingFlags.Public | BindingFlags.Instance)
      .Any(m => m.Name == "Equals" && m.GetParameters().Length == 1);
    Assert.That(hasRecordEquals, Is.True,
      $"{type.Name} should be declared as a record (got class).");
  }

  [Test]
  [TestCaseSource(nameof(KnownClosedSums))]
  public void HasOnlyPrivateConstructors(Type type)
  {
    var publicCtors = type
      .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
      .ToList();
    Assert.That(publicCtors, Is.Empty,
      $"{type.Name} must not expose a public constructor — closed sums are constructed only "
      + "through their nested sealed-record cases.");
  }

  [Test]
  [TestCaseSource(nameof(KnownClosedSums))]
  public void HasAtLeastTwoNestedSealedRecords(Type type)
  {
    var cases = type
      .GetNestedTypes(BindingFlags.Public)
      .Where(IsSealedRecordInheritingFrom(type))
      .ToList();
    Assert.That(cases, Has.Count.GreaterThanOrEqualTo(2),
      $"{type.Name} should declare at least two nested sealed-record cases.");
  }

  private static Func<Type, bool> IsSealedRecordInheritingFrom(Type baseType) =>
    candidate =>
    {
      if (!candidate.IsSealed) return false;
      // Walk inheritance chain, treating open-generic equality through GetGenericTypeDefinition.
      var current = candidate.BaseType;
      while (current is not null)
      {
        var currentBase = current.IsGenericType ? current.GetGenericTypeDefinition() : current;
        var target = baseType.IsGenericType ? baseType.GetGenericTypeDefinition() : baseType;
        if (currentBase == target) return true;
        current = current.BaseType;
      }
      return false;
    };
}
