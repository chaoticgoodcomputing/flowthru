using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Flowthru.Core.SourceGenerators.Step;

/// <summary>
/// Analyzer that emits <c>FT1301</c> when a class implementing
/// <c>Flowthru.Step.IStepExtension</c> declares
/// <c>[StepExtensionCapabilities]</c> whose <c>Inputs</c> or
/// <c>Outputs</c> bitmask fails to include the minimum floor of
/// <c>Singleton | Enumerable</c>. Severity is downgraded from error
/// to warning when the attribute's <c>Status</c> property is
/// <c>ExtensionStatus.InDevelopment</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ExtensionMinimumContainerSupportAnalyzer : DiagnosticAnalyzer
{
  internal const string StepExtensionInterfaceFullName = "Flowthru.Step.IStepExtension";
  internal const string CapabilitiesAttributeFullName = "Flowthru.Step.StepExtensionCapabilitiesAttribute";

  // Mirrors the StepContainerKind enum's bit layout.
  private const int Singleton = 1;
  private const int Enumerable = 1 << 1;
  private const int MinimumFloor = Singleton | Enumerable;

  // Mirrors the ExtensionStatus enum's value layout.
  private const int StatusProduction = 0;
  private const int StatusInDevelopment = 1;

  /// <inheritdoc/>
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    ImmutableArray.Create(StepDiagnostics.ExtensionMissesMinimumContainerSupport);

  /// <inheritdoc/>
  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();
    context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
  }

  private static void AnalyzeNamedType(SymbolAnalysisContext context)
  {
    var type = (INamedTypeSymbol)context.Symbol;
    if (type.TypeKind != TypeKind.Class) return;

    // Must implement IStepExtension — the marker is what makes this a
    // step extension descriptor as opposed to some other annotated
    // class.
    if (!ImplementsInterface(type, StepExtensionInterfaceFullName)) return;

    var capabilitiesAttr = type.GetAttributes().FirstOrDefault(a =>
      a.AttributeClass?.ToDisplayString() == CapabilitiesAttributeFullName);
    if (capabilitiesAttr is null) return;

    if (!TryReadCapabilities(capabilitiesAttr, out var inputs, out var outputs, out var status))
    {
      return;
    }

    var effectiveSeverity = status == StatusInDevelopment
      ? DiagnosticSeverity.Warning
      : DiagnosticSeverity.Error;

    if ((inputs & MinimumFloor) != MinimumFloor)
    {
      Report(context, capabilitiesAttr, type.Name, "Inputs", DescribeKinds(inputs), effectiveSeverity);
    }

    if ((outputs & MinimumFloor) != MinimumFloor)
    {
      Report(context, capabilitiesAttr, type.Name, "Outputs", DescribeKinds(outputs), effectiveSeverity);
    }
  }

  private static void Report(
    SymbolAnalysisContext context,
    AttributeData attr,
    string typeName,
    string slot,
    string declared,
    DiagnosticSeverity severity
  )
  {
    var location = attr.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation()
      ?? Location.None;

    var descriptor = StepDiagnostics.ExtensionMissesMinimumContainerSupport;
    context.ReportDiagnostic(
      Diagnostic.Create(
        descriptor: descriptor,
        location: location,
        effectiveSeverity: severity,
        additionalLocations: null,
        properties: null,
        messageArgs: new object[] { typeName, slot, declared }
      )
    );
  }

  private static bool TryReadCapabilities(
    AttributeData attr,
    out int inputs,
    out int outputs,
    out int status
  )
  {
    inputs = 0;
    outputs = 0;
    status = StatusProduction;

    if (attr.ConstructorArguments.Length < 2) return false;

    var inputArg = attr.ConstructorArguments[0];
    var outputArg = attr.ConstructorArguments[1];
    if (inputArg.Value is not int inputInt) return false;
    if (outputArg.Value is not int outputInt) return false;

    inputs = inputInt;
    outputs = outputInt;

    foreach (var named in attr.NamedArguments)
    {
      if (named.Key == "Status" && named.Value.Value is int statusInt)
      {
        status = statusInt;
      }
    }

    return true;
  }

  private static bool ImplementsInterface(INamedTypeSymbol type, string fullName) =>
    type.AllInterfaces.Any(i => i.ToDisplayString() == fullName);

  private static string DescribeKinds(int bits)
  {
    if (bits == 0) return "None";

    var parts = new System.Collections.Generic.List<string>();
    if ((bits & Singleton) != 0) parts.Add("Singleton");
    if ((bits & Enumerable) != 0) parts.Add("Enumerable");
    if ((bits & (1 << 2)) != 0) parts.Add("Queryable");
    if ((bits & (1 << 3)) != 0) parts.Add("AsyncStream");

    return parts.Count == 0 ? $"0x{bits:X}" : string.Join(" | ", parts);
  }
}
