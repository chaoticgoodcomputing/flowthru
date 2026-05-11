using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Flowthru.FUnit.SourceGenerators;

/// <summary>
/// Shared syntax utilities for FUnit source generators and analyzers.
/// </summary>
internal static class FUnitSyntaxHelpers
{
  /// <summary>
  /// Checks whether a class declaration is enclosed inside a <c>#if</c> preprocessor
  /// directive whose condition text contains <paramref name="guardName"/>.
  /// </summary>
  internal static bool IsInsidePreprocessorGuard(ClassDeclarationSyntax classDecl, string guardName)
  {
    var root = classDecl.SyntaxTree.GetCompilationUnitRoot();
    var classStart = classDecl.SpanStart;

    var directivesBefore = root.DescendantTrivia()
      .Where(t => t.IsDirective && t.SpanStart < classStart)
      .OrderBy(t => t.SpanStart)
      .Select(t => t.GetStructure())
      .OfType<DirectiveTriviaSyntax>();

    var stack = new Stack<bool>();

    foreach (var directive in directivesBefore)
    {
      if (directive is IfDirectiveTriviaSyntax ifDir)
      {
        stack.Push(ifDir.Condition.ToString().Contains(guardName));
      }
      else if (directive is ElifDirectiveTriviaSyntax || directive is ElseDirectiveTriviaSyntax)
      {
        if (stack.Count > 0)
        {
          stack.Pop();
        }

        stack.Push(false);
      }
      else if (directive is EndIfDirectiveTriviaSyntax)
      {
        if (stack.Count > 0)
        {
          stack.Pop();
        }
      }
    }

    return stack.Any(v => v);
  }
}
