using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flowthru.FUnit.SourceGenerators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Flowthru.FUnit.CodeFixes;

/// <summary>
/// Code fix for FU002: wraps a <c>FunitContext</c> subclass with
/// <c>#if FUNIT_ENABLED</c> / <c>#endif</c> preprocessor guards.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Fu002WrapWithFunitEnabledFix)), Shared]
public sealed class Fu002WrapWithFunitEnabledFix : CodeFixProvider
{
    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds =>
      ImmutableArray.Create(FunitDiagnosticAnalyzer.Fu002.Id);

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
        var classDecl = root.FindToken(span.Start)
          .Parent?.AncestorsAndSelf()
          .OfType<ClassDeclarationSyntax>()
          .FirstOrDefault();

        if (classDecl is null)
        {
            return;
        }

        context.RegisterCodeFix(
          CodeAction.Create(
            title: "Wrap with #if FUNIT_ENABLED",
            createChangedDocument: ct => WrapWithGuardAsync(context.Document, classDecl, ct),
            equivalenceKey: nameof(Fu002WrapWithFunitEnabledFix)
          ),
          diagnostic
        );
    }

    private static async Task<Document> WrapWithGuardAsync(
      Document document,
      ClassDeclarationSyntax classDecl,
      CancellationToken cancellationToken
    )
    {
        var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

        // Capture full span of the class including any leading XML doc trivia and attributes
        var fullSpan = classDecl.FullSpan;

        var classText = sourceText.GetSubText(fullSpan).ToString();

        var wrapped = $"#if FUNIT_ENABLED\n{classText}#endif\n";

        var newSourceText = sourceText.Replace(fullSpan, wrapped);
        return document.WithText(newSourceText);
    }
}
