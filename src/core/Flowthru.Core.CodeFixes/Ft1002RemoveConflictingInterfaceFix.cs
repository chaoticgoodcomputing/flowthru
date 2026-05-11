using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flowthru.Core.SourceGenerators.Schema;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Flowthru.Core.CodeFixes;

/// <summary>
/// Code fix for FT1002: removes the conflicting manually-applied marker interface(s)
/// from a <c>[FlowthruSchema]</c> type's base list.
/// The generator will re-apply the correct interfaces automatically.
/// </summary>
[
  ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Ft1002RemoveConflictingInterfaceFix)),
  Shared
]
public sealed class Ft1002RemoveConflictingInterfaceFix : CodeFixProvider
{
  private static readonly string[] _markerInterfaceShortNames =
  {
    "IFlatSchema",
    "INestedSchema",
    "ITextSerializable",
    "IBinarySerializable",
    "IStructuredSerializable",
  };

  /// <inheritdoc/>
  public override ImmutableArray<string> FixableDiagnosticIds =>
    ImmutableArray.Create(SchemaGeneratorDiagnostics.ConflictingManualInterface.Id);

  /// <inheritdoc/>
  public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

  /// <inheritdoc/>
  public override async Task RegisterCodeFixesAsync(CodeFixContext context)
  {
    var root = await context
      .Document.GetSyntaxRootAsync(context.CancellationToken)
      .ConfigureAwait(false);
    if (root is null)
    {
      return;
    }

    var diagnostic = context.Diagnostics.First();
    var span = diagnostic.Location.SourceSpan;
    var typeDecl = root.FindToken(span.Start)
      .Parent?.AncestorsAndSelf()
      .OfType<TypeDeclarationSyntax>()
      .FirstOrDefault();

    if (typeDecl?.BaseList is null)
    {
      return;
    }

    context.RegisterCodeFix(
      CodeAction.Create(
        title: "Remove conflicting marker interface(s)",
        createChangedDocument: ct =>
          RemoveConflictingInterfacesAsync(context.Document, typeDecl, ct),
        equivalenceKey: nameof(Ft1002RemoveConflictingInterfaceFix)
      ),
      diagnostic
    );
  }

  private static async Task<Document> RemoveConflictingInterfacesAsync(
    Document document,
    TypeDeclarationSyntax typeDecl,
    CancellationToken cancellationToken
  )
  {
    var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
    if (root is null)
    {
      return document;
    }

    if (typeDecl.BaseList is null)
    {
      return document;
    }

    // Remove all base list entries whose unqualified name is a known marker interface
    var toRemove = typeDecl
      .BaseList.Types.Where(t =>
      {
        var name =
          (t.Type as IdentifierNameSyntax)?.Identifier.Text
          ?? (t.Type as QualifiedNameSyntax)?.Right.Identifier.Text;
        return name is not null && _markerInterfaceShortNames.Contains(name);
      })
      .ToList();

    if (toRemove.Count == 0)
    {
      return document;
    }

    var newBaseTypes = typeDecl.BaseList.Types;
    foreach (var item in toRemove)
    {
      newBaseTypes = newBaseTypes.Remove(item);
    }

    TypeDeclarationSyntax newDecl;
    if (newBaseTypes.Count == 0)
    {
      newDecl = typeDecl.WithBaseList(null);
    }
    else
    {
      newDecl = typeDecl.WithBaseList(typeDecl.BaseList.WithTypes(newBaseTypes));
    }

    var newRoot = root.ReplaceNode(typeDecl, newDecl);
    return document.WithSyntaxRoot(newRoot);
  }
}
