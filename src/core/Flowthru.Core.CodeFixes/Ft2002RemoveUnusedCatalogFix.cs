using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flowthru.Core.SourceGenerators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Flowthru.Core.CodeFixes;

/// <summary>
/// Code fix for FT2002: removes the <c>RegisterCatalog</c> call that is registered
/// but never referenced by any flow.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Ft2002RemoveUnusedCatalogFix)), Shared]
public sealed class Ft2002RemoveUnusedCatalogFix : CodeFixProvider
{
    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds =>
      ImmutableArray.Create(FlowthruRegistrationAnalyzer.UnusedCatalogId);

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

        // The diagnostic points at the invocation expression; walk up to the statement
        var token = root.FindToken(span.Start);
        var statement = token
          .Parent?.AncestorsAndSelf()
          .OfType<ExpressionStatementSyntax>()
          .FirstOrDefault();
        if (statement is null)
        {
            return;
        }

        context.RegisterCodeFix(
          CodeAction.Create(
            title: "Remove unused RegisterCatalog call",
            createChangedDocument: ct => RemoveStatementAsync(context.Document, statement, ct),
            equivalenceKey: nameof(Ft2002RemoveUnusedCatalogFix)
          ),
          diagnostic
        );
    }

    private static async Task<Document> RemoveStatementAsync(
      Document document,
      ExpressionStatementSyntax statement,
      CancellationToken cancellationToken
    )
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        var newRoot = root.RemoveNode(statement, SyntaxRemoveOptions.KeepLeadingTrivia);
        if (newRoot is null)
        {
            return document;
        }

        return document.WithSyntaxRoot(newRoot);
    }
}
