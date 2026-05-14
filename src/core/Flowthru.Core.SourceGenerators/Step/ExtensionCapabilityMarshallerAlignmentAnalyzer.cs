using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Flowthru.Core.SourceGenerators.Step;

/// <summary>
/// Analyzer that emits <c>FT1303</c> when a step extension's
/// <c>[StepExtensionCapabilities]</c> declarations don't line up with
/// the marshaller marker interfaces the class implements (in either
/// direction).
/// </summary>
/// <remarks>
/// The attribute and the marker interfaces are co-authoritative —
/// declaring <c>Queryable</c> without implementing
/// <c>IQueryableMarshaller&lt;Self&gt;</c>, or implementing it
/// without declaring the kind, is silent drift that the analyzer
/// catches at build time.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ExtensionCapabilityMarshallerAlignmentAnalyzer : DiagnosticAnalyzer
{
  internal const string StepExtensionInterfaceFullName = "Flowthru.Step.IStepExtension";
  internal const string CapabilitiesAttributeFullName = "Flowthru.Step.StepExtensionCapabilitiesAttribute";
  internal const string ContainerMarshallerOpenName = "Flowthru.Step.Marshalling.IContainerMarshaller<TExtension>";
  internal const string QueryableMarshallerOpenName = "Flowthru.Step.Marshalling.IQueryableMarshaller<TExtension>";
  internal const string AsyncStreamMarshallerOpenName = "Flowthru.Step.Marshalling.IAsyncStreamMarshaller<TExtension>";

  // Mirrors StepContainerKind bit layout.
  private const int Singleton = 1;
  private const int Enumerable = 1 << 1;
  private const int Queryable = 1 << 2;
  private const int AsyncStream = 1 << 3;
  private const int FloorBits = Singleton | Enumerable;

  /// <inheritdoc/>
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    ImmutableArray.Create(StepDiagnostics.ExtensionCapabilityImplementationMismatch);

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
    if (!ImplementsInterface(type, StepExtensionInterfaceFullName)) return;

    var capabilitiesAttr = type.GetAttributes().FirstOrDefault(a =>
      a.AttributeClass?.ToDisplayString() == CapabilitiesAttributeFullName);
    if (capabilitiesAttr is null) return;

    if (!TryReadCapabilityBits(capabilitiesAttr, out var inputs, out var outputs)) return;

    var declared = inputs | outputs;
    var hasContainer = ImplementsOpenGenericInterface(type, ContainerMarshallerOpenName);
    var hasQueryable = ImplementsOpenGenericInterface(type, QueryableMarshallerOpenName);
    var hasAsyncStream = ImplementsOpenGenericInterface(type, AsyncStreamMarshallerOpenName);

    var location = capabilitiesAttr.ApplicationSyntaxReference?
      .GetSyntax(context.CancellationToken).GetLocation() ?? type.Locations.FirstOrDefault();

    // ── Floor: any Singleton/Enumerable declaration requires IContainerMarshaller<Self>. ──
    var declaresFloor = (declared & FloorBits) != 0;
    if (declaresFloor && !hasContainer)
    {
      Report(context, location, type.Name,
        "declares Singleton/Enumerable container support but does not implement "
        + "Flowthru.Step.Marshalling.IContainerMarshaller<" + type.Name + ">. "
        + "Add the marker interface, or remove the capability from the attribute.");
    }
    else if (!declaresFloor && hasContainer)
    {
      Report(context, location, type.Name,
        "implements Flowthru.Step.Marshalling.IContainerMarshaller<" + type.Name + "> but the "
        + "[StepExtensionCapabilities] attribute declares neither Singleton nor Enumerable. "
        + "Declare the kinds in the attribute, or remove the marker interface.");
    }

    // ── Opt-in: Queryable. ──
    var declaresQueryable = (declared & Queryable) != 0;
    if (declaresQueryable && !hasQueryable)
    {
      Report(context, location, type.Name,
        "declares Queryable container support but does not implement "
        + "Flowthru.Step.Marshalling.IQueryableMarshaller<" + type.Name + ">. "
        + "Add the marker interface, or remove Queryable from the attribute.");
    }
    else if (!declaresQueryable && hasQueryable)
    {
      Report(context, location, type.Name,
        "implements Flowthru.Step.Marshalling.IQueryableMarshaller<" + type.Name + "> but the "
        + "[StepExtensionCapabilities] attribute does not declare Queryable. "
        + "Add Queryable to the attribute's Inputs or Outputs, or remove the marker interface.");
    }

    // ── Opt-in: AsyncStream. ──
    var declaresAsyncStream = (declared & AsyncStream) != 0;
    if (declaresAsyncStream && !hasAsyncStream)
    {
      Report(context, location, type.Name,
        "declares AsyncStream container support but does not implement "
        + "Flowthru.Step.Marshalling.IAsyncStreamMarshaller<" + type.Name + ">. "
        + "Add the marker interface, or remove AsyncStream from the attribute.");
    }
    else if (!declaresAsyncStream && hasAsyncStream)
    {
      Report(context, location, type.Name,
        "implements Flowthru.Step.Marshalling.IAsyncStreamMarshaller<" + type.Name + "> but the "
        + "[StepExtensionCapabilities] attribute does not declare AsyncStream. "
        + "Add AsyncStream to the attribute's Inputs or Outputs, or remove the marker interface.");
    }
  }

  private static void Report(
    SymbolAnalysisContext context,
    Location? location,
    string typeName,
    string detail
  )
  {
    context.ReportDiagnostic(
      Diagnostic.Create(
        StepDiagnostics.ExtensionCapabilityImplementationMismatch,
        location ?? Location.None,
        typeName,
        detail
      )
    );
  }

  private static bool TryReadCapabilityBits(AttributeData attr, out int inputs, out int outputs)
  {
    inputs = 0;
    outputs = 0;
    if (attr.ConstructorArguments.Length < 2) return false;
    if (attr.ConstructorArguments[0].Value is not int i) return false;
    if (attr.ConstructorArguments[1].Value is not int o) return false;
    inputs = i;
    outputs = o;
    return true;
  }

  private static bool ImplementsInterface(INamedTypeSymbol type, string fullName) =>
    type.AllInterfaces.Any(i => i.ToDisplayString() == fullName);

  private static bool ImplementsOpenGenericInterface(INamedTypeSymbol type, string openGenericFullName) =>
    type.AllInterfaces.Any(i =>
      i.IsGenericType && i.OriginalDefinition.ToDisplayString() == openGenericFullName);
}
