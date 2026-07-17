using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Flowthru.Core.SourceGenerators.Security;

/// <summary>
/// FT5003 — <c>SecretText.Reveal()</c> in a disclosure-prone position. Flags a
/// call to <c>SecretText.Reveal()</c> that appears directly inside a string
/// interpolation, or as an argument to a logging / console-output /
/// <c>string.Format</c> call — the argument positions where a revealed
/// credential would most likely leak into a log line or an exception message.
/// </summary>
/// <remarks>
/// <para>
/// This is a <strong>syntactic, argument-position</strong> check — the
/// design-time backstop for <c>SecretText</c>'s containment (ADR-0026). It is
/// deliberately <strong>not</strong> a taint tracker: it does not follow a
/// <c>Reveal()</c> result through a local variable or across a method call, so
/// <c>var s = secret.Reveal(); log(s);</c> is not flagged. The guarantee it
/// provides — "a revealed secret is not obviously interpolated or logged in
/// place" — is the design-time control the <c>SECURITY.md</c> attestation cites.
/// </para>
/// <para>
/// Escape hatch: standard Roslyn suppression
/// (<c>#pragma warning disable FT5003</c> or
/// <c>[SuppressMessage("Flowthru.Security", "FT5003")]</c>) at a reviewed reveal
/// site that genuinely must format the value — e.g. building the <c>CREATE
/// SECRET</c> SQL that is itself scrubbed from any error.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RevealInSensitivePositionAnalyzer : DiagnosticAnalyzer
{
  /// <summary>FT5003 diagnostic descriptor.</summary>
  public static readonly DiagnosticDescriptor Ft5003 = new(
    id: "FT5003",
    title: "SecretText.Reveal() in a disclosure-prone position",
    messageFormat:
      "SecretText.Reveal() is used in a {0} position — a revealed credential must not be "
        + "interpolated, logged, or formatted in place. Pass the value only to the boundary "
        + "that consumes it, and scrub that reveal site's own failures.",
    category: "Flowthru.Security",
    defaultSeverity: DiagnosticSeverity.Warning,
    isEnabledByDefault: true,
    description:
      "SecretText holds a credential's plaintext, reachable only through Reveal(). "
        + "Interpolating or logging that result would defeat the containment. This is a "
        + "syntactic position check (ADR-0026), not full taint tracking."
  );

  private static readonly ImmutableHashSet<string> LoggingSinkNames = ImmutableHashSet.Create(
    "Log", "LogInformation", "LogWarning", "LogError", "LogDebug", "LogTrace", "LogCritical",
    "WriteLine", "Write", "Format", "TraceInformation", "TraceError", "TraceWarning", "Print"
  );

  /// <inheritdoc/>
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    ImmutableArray.Create(Ft5003);

  /// <inheritdoc/>
  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();
    context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
  }

  private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
  {
    var invocation = (InvocationExpressionSyntax)context.Node;

    // Must be a `<receiver>.Reveal()` with no arguments.
    if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess) return;
    if (memberAccess.Name.Identifier.ValueText != "Reveal") return;
    if (invocation.ArgumentList.Arguments.Count != 0) return;

    // The receiver must be Flowthru.Data.Storage.SecretText.
    if (context.SemanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method) return;
    var containing = method.ContainingType;
    if (containing is null || containing.Name != "SecretText") return;
    var ns = containing.ContainingNamespace?.ToDisplayString() ?? string.Empty;
    if (ns != "Flowthru.Data.Storage") return;

    var position = ClassifyPosition(invocation);
    if (position is null) return;

    context.ReportDiagnostic(Diagnostic.Create(Ft5003, invocation.GetLocation(), position));
  }

  /// <summary>
  /// Classify the disclosure-prone position the <c>Reveal()</c> call sits in, or
  /// null if it is not in one. Walks outward, stopping at a lambda / method
  /// boundary so only the immediate syntactic context is judged.
  /// </summary>
  private static string? ClassifyPosition(InvocationExpressionSyntax reveal)
  {
    for (var node = reveal.Parent; node is not null; node = node.Parent)
    {
      switch (node)
      {
        // A scope boundary reached without finding a disclosure position.
        case SimpleLambdaExpressionSyntax:
        case ParenthesizedLambdaExpressionSyntax:
        case AnonymousMethodExpressionSyntax:
        case MethodDeclarationSyntax:
        case LocalFunctionStatementSyntax:
          return null;

        // Directly inside a string interpolation: $"...{secret.Reveal()}...".
        case InterpolationSyntax:
          return "string-interpolation";

        // A direct argument to a logging / console / format invocation.
        case ArgumentSyntax arg
            when arg.Parent is ArgumentListSyntax argList
              && argList.Parent is InvocationExpressionSyntax outer
              && NamesLoggingSink(outer):
          return "logging-argument";
      }
    }
    return null;
  }

  private static bool NamesLoggingSink(InvocationExpressionSyntax invocation)
  {
    var name = invocation.Expression switch
    {
      MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
      IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
      _ => null,
    };
    return name is not null && LoggingSinkNames.Contains(name);
  }
}
