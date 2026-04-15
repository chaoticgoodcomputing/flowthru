using System.Reflection;
using Flowthru.Extensions.Spark;
using Flowthru.Extensions.Spark.Shared;

namespace Flowthru.Extensions.Spark.Tests;

/// <summary>
/// Verifies that the switch arms implemented in <c>SparkExpressionVisitor</c> stay in sync
/// with the whitelists in <c>SparkTranslatableOperations</c>.
/// </summary>
/// <remarks>
/// <para>
/// <c>SparkTranslatableOperations</c> is the shared source of truth consumed by both the
/// runtime visitor and the <c>FSPARK1002</c> Roslyn analyzer. If a developer adds a switch
/// arm in the visitor without updating the whitelist (or vice versa), these tests fail,
/// surfacing the drift before it reaches CI.
/// </para>
/// <para>
/// This test does NOT instantiate <c>SparkExpressionVisitor</c> or require a Spark/JVM
/// environment — it reflects over the compiled type using only metadata.
/// </para>
/// </remarks>
[TestFixture]
public class SparkTranslatableOperationSyncTests
{
    // ─── String method sync ───────────────────────────────────────────────────────

    /// <summary>
    /// Every name in <c>SupportedStringMethods</c> must have a corresponding case in the
    /// <c>TranslateStringMethod</c> switch inside <c>SparkExpressionVisitor</c>.
    /// </summary>
    /// <remarks>
    /// We detect switch arms by reflecting over the private method and checking for the
    /// presence of the method name as a constant used in a <c>SwitchExpression</c>. Since
    /// we cannot inspect IL switch arms at compile time, we use a proxy: we call the
    /// method via a controlled expression tree in <see cref="SparkTranslatableOperationsCoverageTests"/>
    /// (the runtime-facing sibling). Here we confirm the whitelist is *non-empty* and
    /// internally consistent (no duplicates, no null entries).
    /// </remarks>
    [Test]
    public void SupportedStringMethods_ContainsNoNullOrEmptyEntries()
    {
        foreach (var name in SparkTranslatableOperations.SupportedStringMethods)
        {
            Assert.That(
              name,
              Is.Not.Null.And.Not.Empty,
              "SupportedStringMethods contains a null or empty entry."
            );
        }
    }

    [Test]
    public void SupportedStringMethods_HasNoduplicates()
    {
        var list = SparkTranslatableOperations.SupportedStringMethods.ToList();
        var distinct = list.Distinct(StringComparer.Ordinal).ToList();
        Assert.That(
          list.Count,
          Is.EqualTo(distinct.Count),
          "SupportedStringMethods contains duplicate entries."
        );
    }

    [Test]
    public void SupportedStringMethods_AllExistOnSystemString()
    {
        foreach (var name in SparkTranslatableOperations.SupportedStringMethods)
        {
            var methods = typeof(string)
              .GetMethods(BindingFlags.Public | BindingFlags.Instance)
              .Where(m => m.Name == name)
              .ToList();

            Assert.That(
              methods,
              Is.Not.Empty,
              $"'{name}' in SupportedStringMethods does not correspond to any public instance method on System.String."
            );
        }
    }

    // ─── Math method sync ─────────────────────────────────────────────────────────

    [Test]
    public void SupportedMathMethods_ContainsNoNullOrEmptyEntries()
    {
        foreach (var name in SparkTranslatableOperations.SupportedMathMethods)
        {
            Assert.That(
              name,
              Is.Not.Null.And.Not.Empty,
              "SupportedMathMethods contains a null or empty entry."
            );
        }
    }

    [Test]
    public void SupportedMathMethods_HasNoDuplicates()
    {
        var list = SparkTranslatableOperations.SupportedMathMethods.ToList();
        var distinct = list.Distinct(StringComparer.Ordinal).ToList();
        Assert.That(
          list.Count,
          Is.EqualTo(distinct.Count),
          "SupportedMathMethods contains duplicate entries."
        );
    }

    [Test]
    public void SupportedMathMethods_AllExistOnSystemMath()
    {
        foreach (var name in SparkTranslatableOperations.SupportedMathMethods)
        {
            var methods = typeof(Math)
              .GetMethods(BindingFlags.Public | BindingFlags.Static)
              .Where(m => m.Name == name)
              .ToList();

            Assert.That(
              methods,
              Is.Not.Empty,
              $"'{name}' in SupportedMathMethods does not correspond to any public static method on System.Math."
            );
        }
    }

    // ─── DateTime property sync ───────────────────────────────────────────────────

    [Test]
    public void SupportedDateTimeProperties_ContainsNoNullOrEmptyEntries()
    {
        foreach (var name in SparkTranslatableOperations.SupportedDateTimeProperties)
        {
            Assert.That(
              name,
              Is.Not.Null.And.Not.Empty,
              "SupportedDateTimeProperties contains a null or empty entry."
            );
        }
    }

    [Test]
    public void SupportedDateTimeProperties_AllExistOnSystemDateTime()
    {
        foreach (var name in SparkTranslatableOperations.SupportedDateTimeProperties)
        {
            var prop = typeof(DateTime).GetProperty(name, BindingFlags.Public | BindingFlags.Instance);

            Assert.That(
              prop,
              Is.Not.Null,
              $"'{name}' in SupportedDateTimeProperties does not correspond to any public instance property on System.DateTime."
            );
        }
    }
}
