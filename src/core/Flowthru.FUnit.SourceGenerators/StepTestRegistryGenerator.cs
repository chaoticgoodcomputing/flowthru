using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Flowthru.FUnit.SourceGenerators;

/// <summary>
/// Incremental source generator that:
/// <list type="number">
/// <item>Collects all classes annotated with <c>[FlowthruStep]</c> in the compilation.</item>
/// <item>Collects all methods annotated with <c>[StepTest(typeof(X))]</c> and maps them
///   to step types.</item>
/// <item>Emits a <c>StepTestRegistry</c> class mapping step types to test counts.</item>
/// <item>Detects which test framework (NUnit, xUnit, MSTest) is referenced and emits
///   framework-annotated runner classes so <c>dotnet test</c> can discover
///   <c>[StepTest]</c> methods without any framework attributes in user code.</item>
/// </list>
/// <para>
/// Diagnostics (FU001, FU002) are emitted by <see cref="FunitDiagnosticAnalyzer"/>,
/// not by this generator — generators should only generate code.
/// </para>
/// </summary>
[Generator]
public class StepTestRegistryGenerator : IIncrementalGenerator
{
  private const string FlowthruStepAttributeFullName = "Flowthru.Core.Steps.FlowthruStepAttribute";
  private const string StepTestAttributeFullName = "Flowthru.FUnit.StepTestAttribute";

  private enum TestFramework
  {
    None,
    NUnit,
    XUnit,
    MSTest,
  }

  // All information needed to emit a test runner for one [StepTest] method.
  private sealed record StepTestEntry(
    string MethodName,
    string StepTypeFqn,
    string TestClassFqn,
    string TestClassNamespace,
    string BaseClassRef // type path relative to its containing namespace, e.g. "MySplitStep.Tests"
  );

  /// <inheritdoc/>
  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    // Collect [FlowthruStep]-annotated class declarations
    var stepClasses = context
      .SyntaxProvider.CreateSyntaxProvider(
        predicate: static (node, _) =>
          node is ClassDeclarationSyntax c && c.AttributeLists.Count > 0,
        transform: static (ctx, _) =>
        {
          var classDecl = (ClassDeclarationSyntax)ctx.Node;
          if (ctx.SemanticModel.GetDeclaredSymbol(classDecl) is not INamedTypeSymbol symbol)
          {
            return null;
          }

          var hasAttr = symbol
            .GetAttributes()
            .Any(a => a.AttributeClass?.ToDisplayString() == FlowthruStepAttributeFullName);

          return hasAttr ? symbol : null;
        }
      )
      .Where(s => s is not null)
      .Collect();

    // Collect [StepTest]-annotated methods with full class context for runner emission
    var stepTestEntries = context
      .SyntaxProvider.CreateSyntaxProvider(
        predicate: static (node, _) =>
          node is MethodDeclarationSyntax m && m.AttributeLists.Count > 0,
        transform: static (ctx, _) =>
        {
          var method = (MethodDeclarationSyntax)ctx.Node;
          if (ctx.SemanticModel.GetDeclaredSymbol(method) is not IMethodSymbol symbol)
          {
            return null;
          }

          foreach (var attr in symbol.GetAttributes())
          {
            if (attr.AttributeClass?.ToDisplayString() != StepTestAttributeFullName)
            {
              continue;
            }

            if (
              attr.ConstructorArguments.Length > 0
              && attr.ConstructorArguments[0].Value is INamedTypeSymbol stepType
            )
            {
              var testClass = symbol.ContainingType;
              var ns = testClass.ContainingNamespace is { IsGlobalNamespace: false }
                ? testClass.ContainingNamespace.ToDisplayString()
                : "";
              var fqn = testClass.ToDisplayString();
              var baseRef =
                ns.Length > 0 && fqn.StartsWith(ns + ".") ? fqn.Substring(ns.Length + 1) : fqn;

              return new StepTestEntry(
                MethodName: symbol.Name,
                StepTypeFqn: stepType.ToDisplayString(),
                TestClassFqn: fqn,
                TestClassNamespace: ns,
                BaseClassRef: baseRef
              );
            }
          }

          return null;
        }
      )
      .Where(e => e is not null)
      .Collect();

    // Detect which test framework (if any) is referenced in this compilation
    var framework = context.CompilationProvider.Select(
      static (compilation, _) =>
      {
        var assemblyNames = compilation.ReferencedAssemblyNames;
        if (
          assemblyNames.Any(static a =>
            string.Equals(a.Name, "nunit.framework", System.StringComparison.OrdinalIgnoreCase)
          )
        )
        {
          return TestFramework.NUnit;
        }

        if (
          assemblyNames.Any(static a =>
            string.Equals(a.Name, "xunit.core", System.StringComparison.OrdinalIgnoreCase)
          )
          || assemblyNames.Any(static a =>
            string.Equals(a.Name, "xunit.v3.core", System.StringComparison.OrdinalIgnoreCase)
          )
        )
        {
          return TestFramework.XUnit;
        }

        if (
          assemblyNames.Any(static a =>
            string.Equals(
              a.Name,
              "Microsoft.VisualStudio.TestPlatform.TestFramework",
              System.StringComparison.OrdinalIgnoreCase
            )
          )
        )
        {
          return TestFramework.MSTest;
        }

        return TestFramework.None;
      }
    );

    // StepTestRegistry source
    context.RegisterSourceOutput(
      stepClasses.Combine(stepTestEntries),
      static (ctx, pair) => Execute(ctx, pair.Left!, pair.Right!)
    );

    // Per-framework test runner classes enabling dotnet test discovery
    context.RegisterSourceOutput(
      framework.Combine(stepTestEntries),
      static (ctx, pair) => EmitRunners(ctx, pair.Left, pair.Right!)
    );
  }

  private static void Execute(
    SourceProductionContext context,
    IReadOnlyList<INamedTypeSymbol?> stepSymbols,
    IReadOnlyList<StepTestEntry?> entries
  )
  {
    // Build map: fully-qualified step name → test count
    var testCounts = entries
      .Where(e => e is not null)
      .GroupBy(e => e!.StepTypeFqn)
      .ToDictionary(g => g.Key, g => g.Count());

    // Deduplicate step symbols (incremental pipelines may yield duplicates)
    var steps = stepSymbols
      .Where(s => s is not null)
      .Select(s => s!)
      .GroupBy(s => s.ToDisplayString())
      .Select(g => g.First())
      .ToList();

    if (steps.Count == 0)
    {
      return;
    }

    // Emit StepTestRegistry
    var sb = new StringBuilder();
    sb.AppendLine(
      """
      // <auto-generated/>
      #nullable enable
      using System;
      using System.Collections.Generic;

      internal static class StepTestRegistry
      {
          public static IReadOnlyDictionary<Type, int> TestCounts { get; } =
              new Dictionary<Type, int>
              {
      """
    );

    foreach (var step in steps)
    {
      var fqn = step.ToDisplayString();
      var count = testCounts.TryGetValue(fqn, out var c) ? c : 0;
      sb.AppendLine($"          [typeof({fqn})] = {count},");
    }

    sb.AppendLine(
      """
              };
      }
      """
    );

    context.AddSource("StepTestRegistry.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
  }

  private static void EmitRunners(
    SourceProductionContext context,
    TestFramework framework,
    IReadOnlyList<StepTestEntry?> entries
  )
  {
    if (framework == TestFramework.None)
    {
      return;
    }

    // Group methods by their containing test class, one runner file per class
    var groups = entries.Where(e => e is not null).GroupBy(e => e!.TestClassFqn).ToList();

    foreach (var group in groups)
    {
      var first = group.First()!;
      var methods = group.Select(e => e!.MethodName).Distinct().ToList();
      var runnerName =
        first.BaseClassRef.Replace(".", "_") + "_" + FrameworkRunnerSuffix(framework);
      var hasNamespace = first.TestClassNamespace.Length > 0;
      var indent = hasNamespace ? "    " : "";

      var sb = new StringBuilder();
      sb.AppendLine("// <auto-generated/>");
      sb.AppendLine(
        "// FUnit test runner — generated by Flowthru.FUnit.SourceGenerators. Do not edit manually."
      );
      sb.AppendLine("#nullable enable");
      sb.AppendLine();
      sb.AppendLine(FrameworkUsing(framework));

      if (hasNamespace)
      {
        sb.AppendLine($"namespace {first.TestClassNamespace}");
        sb.AppendLine("{");
      }

      if (framework is TestFramework.NUnit)
      {
        sb.AppendLine($"{indent}[NUnit.Framework.TestFixture]");
        sb.AppendLine($"{indent}[NUnit.Framework.Category(\"FUnit\")]");
      }
      else if (framework is TestFramework.MSTest)
      {
        sb.AppendLine($"{indent}[Microsoft.VisualStudio.TestTools.UnitTesting.TestClass]");
      }

      sb.AppendLine($"{indent}public sealed class {runnerName} : {first.BaseClassRef}");
      sb.AppendLine($"{indent}{{");

      foreach (var method in methods)
      {
        sb.AppendLine($"{indent}    [{MethodAttribute(framework)}]");
        sb.AppendLine($"{indent}    public new void {method}() => base.{method}();");
        sb.AppendLine();
      }

      sb.AppendLine($"{indent}}}");

      if (hasNamespace)
      {
        sb.AppendLine("}");
      }

      context.AddSource($"{runnerName}.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }
  }

  private static string FrameworkRunnerSuffix(TestFramework framework) =>
    framework switch
    {
      TestFramework.NUnit => "NUnitRunner",
      TestFramework.XUnit => "XUnitRunner",
      TestFramework.MSTest => "MSTestRunner",
      _ => "Runner",
    };

  private static string FrameworkUsing(TestFramework framework) =>
    framework switch
    {
      TestFramework.NUnit => "using NUnit.Framework;",
      TestFramework.XUnit => "using Xunit;",
      TestFramework.MSTest => "using Microsoft.VisualStudio.TestTools.UnitTesting;",
      _ => "",
    };

  private static string MethodAttribute(TestFramework framework) =>
    framework switch
    {
      TestFramework.NUnit => "NUnit.Framework.Test",
      TestFramework.XUnit => "Xunit.Fact",
      TestFramework.MSTest => "Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod",
      _ => "",
    };
}
