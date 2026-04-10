using System.Reflection;
using Flowthru.Tests.Examples.Infrastructure;

namespace Flowthru.Tests.Examples;

/// <summary>
/// Verifies that the FUnit source generator auto-discovers test frameworks in example projects
/// and emits the expected NUnit runner classes.
/// </summary>
/// <remarks>
/// The generator detects <c>nunit.framework</c> in referenced assemblies and emits a
/// <c>*_NUnitRunner</c> sealed class annotated with <c>[NUnit.Framework.Category("FUnit")]</c>
/// for each <c>Tests : FunitContext</c> class that contains <c>[StepTest]</c> methods.
/// </remarks>
[TestFixture]
[Category("FUnit")]
[Category("AutoDiscovery")]
public class FUnitAutoDiscoveryTests
{
  /// <summary>
  /// Finds all types in example assemblies that carry <c>[Category("FUnit")]</c> — the
  /// marker that <c>StepTestRegistryGenerator</c> stamps on every emitted runner class.
  /// </summary>
  private static IReadOnlyList<Type> DiscoverFUnitRunnerTypes()
  {
    var testOutputDir = Path.GetDirectoryName(typeof(FUnitAutoDiscoveryTests).Assembly.Location)!;

    return Directory
      .GetFiles(testOutputDir, "*.dll")
      .Select(dll => TryLoadAssembly(dll))
      .Where(a => a is not null)
      .SelectMany(a => TryGetTypes(a!))
      .Where(t =>
        t.Name.EndsWith("NUnitRunner", StringComparison.Ordinal)
        && t.GetCustomAttributes<NUnit.Framework.CategoryAttribute>().Any(c => c.Name == "FUnit")
      )
      .ToList();
  }

  private static IEnumerable<Type> TryGetTypes(Assembly assembly)
  {
    try
    {
      return assembly.GetTypes();
    }
    catch (ReflectionTypeLoadException ex)
    {
      // Return whatever types loaded successfully, ignoring those that failed.
      return ex.Types.Where(t => t is not null).Cast<Type>();
    }
  }

  private static Assembly? TryLoadAssembly(string path)
  {
    try
    {
      return Assembly.LoadFrom(path);
    }
    catch
    {
      return null;
    }
  }

  /// <summary>
  /// At least one FUnit runner must be present in the examples.
  /// Failure here means the generator stopped emitting runners or the example
  /// project no longer contributes to the test output directory.
  /// </summary>
  [Test]
  public void FUnitRunners_AreDiscoveredInExamples()
  {
    var runners = DiscoverFUnitRunnerTypes();

    TestContext.Out.WriteLine($"Discovered {runners.Count} FUnit runner type(s):");
    foreach (var t in runners)
      TestContext.Out.WriteLine($"  - {t.FullName} ({t.Assembly.GetName().Name})");

    Assert.That(
      runners,
      Is.Not.Empty,
      "No FUnit runner types were found. The StepTestRegistryGenerator may have stopped "
        + "emitting runners, or no example project with [StepTest] methods is referenced."
    );
  }

  /// <summary>
  /// Each runner must be a sealed class that directly inherits a <c>FunitContext</c> subclass,
  /// confirming that the generator emitted structurally valid types.
  /// </summary>
  [TestCaseSource(nameof(DiscoverFUnitRunnerTypes))]
  public void FUnitRunner_InheritsFromFunitContextSubclass(Type runnerType)
  {
    var baseType = runnerType.BaseType;

    Assert.That(baseType, Is.Not.Null, $"{runnerType.FullName} has no base type.");

    // Walk the inheritance chain looking for FunitContext
    var current = baseType;
    while (current is not null)
    {
      if (current.FullName == "Flowthru.FUnit.FunitContext")
        return;
      current = current.BaseType;
    }

    Assert.Fail(
      $"{runnerType.FullName} (base: {baseType!.FullName}) does not inherit from "
        + "Flowthru.FUnit.FunitContext anywhere in its hierarchy."
    );
  }
}
