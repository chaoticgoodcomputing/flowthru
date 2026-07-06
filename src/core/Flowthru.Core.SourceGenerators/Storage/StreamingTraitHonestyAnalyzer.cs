using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Flowthru.Core.SourceGenerators.Storage;

/// <summary>
/// Analyzer that emits <c>FT1201</c> when a format serializer declares
/// <c>StorageTraits.CanStream = true</c> but does not actually stream.
/// </summary>
/// <remarks>
/// <para>Two sub-cases, one diagnostic:</para>
/// <list type="number">
///   <item><description><b>Marker honesty</b> — <c>CanStream = true</c> without
///     implementing <c>Flowthru.Data.Storage.IFormatStreamReader&lt;TRow&gt;</c>.
///     This is the compile-time complement of the runtime drift law in
///     <c>IFormatSerializerLaws</c> (which only fires once a laws test is
///     written).</description></item>
///   <item><description><b>Body honesty</b> — <c>CanStream = true</c> with a
///     <c>DeserializeRows</c> body that materialises the whole input: a
///     whole-document <c>JsonSerializer.Deserialize</c>/<c>DeserializeAsync</c>
///     call, or a <c>ToList</c>/<c>ToListAsync</c>/<c>ToArray</c>/<c>ToArrayAsync</c>
///     applied to the input <c>stream</c>. This is the signal the runtime drift
///     law cannot give — it inspects only the flag/marker agreement, never the
///     body.</description></item>
/// </list>
/// <para>
/// Deliberately conservative to keep false positives near zero: the body check
/// flags a materialisation only when its receiver names the <c>stream</c>
/// parameter (so a bounded per-row-group <c>ToList</c> over decoded metadata —
/// as in the Parquet reader — is not flagged), and matches only the
/// whole-document JSON entry points (never <c>DeserializeAsyncEnumerable</c>).
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StreamingTraitHonestyAnalyzer : DiagnosticAnalyzer
{
  internal const string FormatRowReaderOpenName = "Flowthru.Data.Storage.IFormatRowReader<TRow>";
  internal const string FormatStreamReaderOpenName = "Flowthru.Data.Storage.IFormatStreamReader<TRow>";

  private static readonly ImmutableHashSet<string> MaterializingCalls =
    ImmutableHashSet.Create("ToList", "ToListAsync", "ToArray", "ToArrayAsync");

  /// <inheritdoc/>
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    ImmutableArray.Create(StorageDiagnostics.StreamingTraitDishonest);

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

    // Gate: only format serializers (IFormatRowReader<TRow> implementers).
    // This excludes storage adapters / media that carry CanStream on their own
    // traits (EFCore, S3, HTTP) — those are not format-serializer read paths.
    if (!ImplementsOpenGenericInterface(type, FormatRowReaderOpenName)) return;

    // Only act when the type declares CanStream = true on its own Traits member.
    if (!TryFindCanStreamTrue(type, context.CancellationToken, out var canStreamLocation)) return;

    // ── Branch 1: marker honesty (compile-time complement of the drift law). ──
    if (!ImplementsOpenGenericInterface(type, FormatStreamReaderOpenName))
    {
      Report(context, canStreamLocation, type.Name,
        "declares StorageTraits.CanStream = true but does not implement "
        + "Flowthru.Data.Storage.IFormatStreamReader<TRow>. A genuinely streaming format carries the "
        + "IFormatStreamReader<TRow> marker; add it, or set CanStream = false.");
    }

    // ── Branch 2: body honesty (the signal the drift law cannot give). ──
    foreach (var (location, detail) in FindWholeInputMaterializations(type, context.CancellationToken))
    {
      Report(context, location, type.Name, detail);
    }
  }

  /// <summary>
  /// Finds an object-initializer assignment <c>CanStream = true</c> on the
  /// type's own <c>Traits</c> member (property or field). Returns its location
  /// so branch-1 diagnostics point at the offending trait declaration.
  /// </summary>
  private static bool TryFindCanStreamTrue(
    INamedTypeSymbol type,
    CancellationToken ct,
    out Location location)
  {
    location = type.Locations.FirstOrDefault() ?? Location.None;

    foreach (var member in type.GetMembers("Traits"))
    {
      if (member is not (IPropertySymbol or IFieldSymbol)) continue;

      foreach (var syntaxRef in member.DeclaringSyntaxReferences)
      {
        var assignment = syntaxRef.GetSyntax(ct)
          .DescendantNodes()
          .OfType<AssignmentExpressionSyntax>()
          .FirstOrDefault(IsCanStreamTrue);
        if (assignment is not null)
        {
          location = assignment.GetLocation();
          return true;
        }
      }
    }

    return false;
  }

  private static bool IsCanStreamTrue(AssignmentExpressionSyntax assignment) =>
    assignment.Left is IdentifierNameSyntax { Identifier.ValueText: "CanStream" }
    && assignment.Right is LiteralExpressionSyntax literal
    && literal.IsKind(SyntaxKind.TrueLiteralExpression);

  /// <summary>
  /// Scans the type's <c>DeserializeRows</c> method bodies for calls that
  /// materialise the whole input, yielding one (location, detail) per offence.
  /// </summary>
  private static IEnumerable<(Location Location, string Detail)> FindWholeInputMaterializations(
    INamedTypeSymbol type,
    CancellationToken ct)
  {
    foreach (var member in type.GetMembers("DeserializeRows").OfType<IMethodSymbol>())
    {
      foreach (var syntaxRef in member.DeclaringSyntaxReferences)
      {
        if (syntaxRef.GetSyntax(ct) is not MethodDeclarationSyntax method) continue;

        var streamParam = method.ParameterList.Parameters.FirstOrDefault()?.Identifier.ValueText;

        foreach (var invocation in method.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
          var detail = ClassifyInvocation(invocation, streamParam);
          if (detail is not null)
          {
            yield return (invocation.GetLocation(), detail);
          }
        }
      }
    }
  }

  private static string? ClassifyInvocation(InvocationExpressionSyntax invocation, string? streamParam)
  {
    if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess) return null;
    var calledName = memberAccess.Name.Identifier.ValueText;

    // Whole-document JSON deserialize (deliberately not DeserializeAsyncEnumerable).
    if ((calledName == "Deserialize" || calledName == "DeserializeAsync")
        && ReceiverTrailingNameEquals(memberAccess.Expression, "JsonSerializer"))
    {
      return "its DeserializeRows body calls the whole-document JsonSerializer." + calledName
        + "(...) rather than the streaming JsonSerializer.DeserializeAsyncEnumerable(...) API, which "
        + "buffers the entire input before yielding. Use the streaming API, or set CanStream = false.";
    }

    // Materialising the input stream (ToList/ToArray over something derived from `stream`).
    if (MaterializingCalls.Contains(calledName)
        && streamParam is not null
        && ReceiverReferencesIdentifier(memberAccess.Expression, streamParam))
    {
      return "its DeserializeRows body calls ." + calledName + "() over the input stream, materialising "
        + "the whole dataset before yielding — O(dataset) memory, not the O(batch) that CanStream = true "
        + "promises. Yield incrementally, or set CanStream = false.";
    }

    return null;
  }

  private static bool ReceiverReferencesIdentifier(ExpressionSyntax receiver, string name) =>
    receiver.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>()
      .Any(id => id.Identifier.ValueText == name);

  private static bool ReceiverTrailingNameEquals(ExpressionSyntax receiver, string name) =>
    receiver switch
    {
      IdentifierNameSyntax id => id.Identifier.ValueText == name,
      MemberAccessExpressionSyntax ma => ma.Name.Identifier.ValueText == name,
      _ => false,
    };

  private static void Report(SymbolAnalysisContext context, Location location, string typeName, string detail) =>
    context.ReportDiagnostic(Diagnostic.Create(
      StorageDiagnostics.StreamingTraitDishonest, location, typeName, detail));

  private static bool ImplementsOpenGenericInterface(INamedTypeSymbol type, string openGenericFullName) =>
    type.AllInterfaces.Any(i =>
      i.IsGenericType && i.OriginalDefinition.ToDisplayString() == openGenericFullName);
}
