using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flowthru.Core.SourceGenerators.StepAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Flowthru.Core.CodeFixes;

/// <summary>
/// Code fix for FT4001: adds <c>[FlowthruStep]</c> to the step factory class referenced
/// from a <c>FlowBuilder.AddStep(transform: …)</c> call. The fix may modify a different
/// document than the diagnostic site, since the step class is typically authored in its
/// own file.
/// </summary>
[
  ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Ft4001AddFlowthruStepAttributeFix)),
  Shared
]
public sealed class Ft4001AddFlowthruStepAttributeFix : CodeFixProvider
{
  private const string FlowthruStepsNamespace = "Flowthru.Core.Steps";
  private const string FlowthruStepAttributeShortName = "FlowthruStep";

  /// <inheritdoc/>
  public override ImmutableArray<string> FixableDiagnosticIds =>
    ImmutableArray.Create(StepDiagnostics.MissingFlowthruStepAttribute.Id);

  /// <inheritdoc/>
  public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

  /// <inheritdoc/>
  public override async Task RegisterCodeFixesAsync(CodeFixContext context)
  {
    var diagnostic = context.Diagnostics.First();
    var document = context.Document;

    var root = await document
      .GetSyntaxRootAsync(context.CancellationToken)
      .ConfigureAwait(false);
    if (root is null)
    {
      return;
    }

    // The diagnostic location is the `transform:` argument expression. Resolve the
    // receiver type via the same logic the analyzer uses.
    var node = root.FindNode(diagnostic.Location.SourceSpan);
    var semanticModel = await document
      .GetSemanticModelAsync(context.CancellationToken)
      .ConfigureAwait(false);
    if (semanticModel is null)
    {
      return;
    }

    var receiverType = ResolveReceiverType(node, semanticModel, context.CancellationToken);
    if (receiverType is null)
    {
      return;
    }

    var declRef = receiverType.DeclaringSyntaxReferences.FirstOrDefault();
    if (declRef is null)
    {
      return;
    }

    var className = receiverType.Name;
    context.RegisterCodeFix(
      CodeAction.Create(
        title: $"Add [FlowthruStep] attribute to '{className}'",
        createChangedSolution: ct =>
          AddAttributeAsync(document.Project.Solution, declRef, ct),
        equivalenceKey: nameof(Ft4001AddFlowthruStepAttributeFix)
      ),
      diagnostic
    );
  }

  private static async Task<Solution> AddAttributeAsync(
    Solution solution,
    SyntaxReference declRef,
    CancellationToken cancellationToken
  )
  {
    var classSyntaxTree = declRef.SyntaxTree;
    var classDoc = solution.GetDocument(classSyntaxTree);
    if (classDoc is null)
    {
      return solution;
    }

    var classRoot = await classDoc
      .GetSyntaxRootAsync(cancellationToken)
      .ConfigureAwait(false) as CompilationUnitSyntax;
    if (classRoot is null)
    {
      return solution;
    }

    var classNode = (await declRef.GetSyntaxAsync(cancellationToken).ConfigureAwait(false))
      as TypeDeclarationSyntax;
    if (classNode is null)
    {
      return solution;
    }

    // Build `[FlowthruStep]` attribute syntax.
    var attributeList = SyntaxFactory.AttributeList(
      SyntaxFactory.SingletonSeparatedList(
        SyntaxFactory.Attribute(SyntaxFactory.IdentifierName(FlowthruStepAttributeShortName))
      )
    );

    // Preserve existing leading trivia (e.g., XML doc comments) on the class declaration
    // by transferring it to the attribute list, then replacing the class's leading trivia
    // with a single line break so the attribute renders on its own line.
    var leadingTrivia = classNode.GetLeadingTrivia();
    var attributeListWithTrivia = attributeList.WithLeadingTrivia(leadingTrivia);
    var classWithoutLeadingTrivia = classNode.WithLeadingTrivia(SyntaxFactory.LineFeed);

    var newClassNode = classWithoutLeadingTrivia.WithAttributeLists(
      classWithoutLeadingTrivia.AttributeLists.Insert(0, attributeListWithTrivia)
    );

    var newRoot = classRoot.ReplaceNode(classNode, newClassNode);

    // Ensure `using Flowthru.Core.Steps;` is present so the unqualified attribute resolves.
    newRoot = EnsureUsing(newRoot, FlowthruStepsNamespace);

    return solution.WithDocumentSyntaxRoot(classDoc.Id, newRoot);
  }

  private static CompilationUnitSyntax EnsureUsing(
    CompilationUnitSyntax root,
    string namespaceName
  )
  {
    bool alreadyPresent = root
      .Usings.Any(u => u.Name?.ToString() == namespaceName);
    if (alreadyPresent)
    {
      return root;
    }

    var usingDirective = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(namespaceName));
    return root.AddUsings(usingDirective);
  }

  // Mirrors the analyzer's resolution logic so the fix targets the same type the
  // diagnostic flagged.
  private static INamedTypeSymbol? ResolveReceiverType(
    SyntaxNode node,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    // Walk up from the located token until we hit a recognized expression shape.
    var expr = node.AncestorsAndSelf().OfType<ExpressionSyntax>().FirstOrDefault();
    if (expr is null)
    {
      return null;
    }

    if (expr is InvocationExpressionSyntax inner)
    {
      expr = inner.Expression;
    }

    return expr switch
    {
      MemberAccessExpressionSyntax ma =>
        semanticModel.GetTypeInfo(ma.Expression, cancellationToken).Type as INamedTypeSymbol
          ?? semanticModel.GetSymbolInfo(ma.Expression, cancellationToken).Symbol
            as INamedTypeSymbol,
      IdentifierNameSyntax id =>
        semanticModel.GetSymbolInfo(id, cancellationToken).Symbol is IMethodSymbol method
          ? method.ContainingType
          : null,
      _ => null,
    };
  }
}
