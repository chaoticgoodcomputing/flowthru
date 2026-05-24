using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Flowthru.Core.SourceGenerators.Step;

/// <summary>
/// Emits a <c>{StepClassName}_Metadata</c> companion record next to
/// every <c>[FlowthruStep]</c>-decorated static class. The companion
/// carries identification (class name, label) plus the
/// <c>StepTraits</c> taken from the attribute, so diagnostics tooling
/// and architecture tests can iterate every step in an assembly
/// without instantiating it.
/// </summary>
/// <remarks>
/// <para>
/// Per §2.4, the companion is the source-of-truth for per-step
/// metadata that doesn't fit inside <c>NodeTraits</c>. <c>NodeTraits</c>
/// stays universal; <c>StepTraits</c> is step-specific and lives on
/// the companion.
/// </para>
/// <para>
/// <strong>CodeVersion.</strong> The companion also exposes a build-time
/// <c>CodeVersion</c> constant — a stable identity for the step's
/// transform logic. It is computed as a SHA-256 prefix over the step
/// class's syntactically normalized source text (trivia stripped via
/// Roslyn's <c>NormalizeWhitespace</c>) so whitespace-only and
/// comment-only edits do not invalidate the identity. <c>[FlowthruStep(CodeVersion = "v2")]</c> replaces the
/// computed digest verbatim — the escape hatch for users that need
/// stable cross-machine identities the trivia stripper can't guarantee.
/// </para>
/// <para>
/// <strong>v1 scope.</strong> The hash covers the step class's own
/// source text only. Cross-assembly type-symbol changes — e.g., a
/// schema record renamed in another project — are not reflected.
/// Downstream cache-plan logic must therefore also incorporate input
/// item digests when deciding cache hits; the per-step
/// <c>CodeVersion</c> is one dimension of that identity, not the whole.
/// </para>
/// </remarks>
[Generator]
public sealed class StepMetadataGenerator : IIncrementalGenerator
{
  private const string AttributeFullName = "Flowthru.Step.FlowthruStepAttribute";

  /// <summary>
  /// Length in hex characters of the SHA-256 prefix emitted as the
  /// computed <c>CodeVersion</c>. 16 hex chars = 64 bits of entropy —
  /// collision probability for the working-set of a single repo
  /// (thousands of steps at most) is vanishingly small while keeping
  /// the constant short enough to be human-glanceable.
  /// </summary>
  private const int CodeVersionHexLength = 16;

  /// <summary>
  /// Fully-qualified type names of framework-recognised observation-
  /// only services. When a <c>Create()</c> parameter's FQN matches an
  /// entry here, the generator emits a <c>ServiceRef.ObservationOnly</c>
  /// instead of the default <c>ServiceRef.CSharp</c> — the cache
  /// planner skips observation-only refs when deciding step
  /// cacheability (ADR-0010). Keep this set tiny; an
  /// <c>[ObservationOnly]</c> per-parameter attribute is the planned
  /// opt-in mechanism for user-defined observability services.
  /// </summary>
  private static readonly System.Collections.Generic.HashSet<string> _observationOnlyFqns =
    new(System.StringComparer.Ordinal)
    {
      "global::Microsoft.Extensions.Logging.ILogger",
    };

  /// <inheritdoc/>
  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    var candidates = context
      .SyntaxProvider.ForAttributeWithMetadataName(
        AttributeFullName,
        predicate: static (node, _) => node is ClassDeclarationSyntax,
        transform: static (ctx, _) => ExtractStepInfo(ctx)
      )
      .Where(static info => info != null)
      .Select(static (info, _) => info!);

    context.RegisterSourceOutput(candidates, static (ctx, info) => Emit(ctx, info));
  }

  private static StepInfo? ExtractStepInfo(GeneratorAttributeSyntaxContext ctx)
  {
    if (ctx.TargetSymbol is not INamedTypeSymbol typeSymbol) return null;
    if (ctx.TargetNode is not ClassDeclarationSyntax classDecl) return null;

    var attribute = typeSymbol
      .GetAttributes()
      .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == AttributeFullName);
    if (attribute is null) return null;

    string? label = null;
    var isIdempotent = false;
    var hasSideEffects = false;
    string? codeVersionOverride = null;
    foreach (var named in attribute.NamedArguments)
    {
      switch (named.Key)
      {
        case "Label":
          label = named.Value.Value as string;
          break;
        case "IsIdempotent":
          isIdempotent = named.Value.Value is true;
          break;
        case "HasSideEffects":
          hasSideEffects = named.Value.Value is true;
          break;
        case "CodeVersion":
          codeVersionOverride = named.Value.Value as string;
          break;
      }
    }

    var ns = typeSymbol.ContainingNamespace.IsGlobalNamespace
      ? ""
      : typeSymbol.ContainingNamespace.ToDisplayString();

    // Compute the source-text digest when no explicit override is
    // supplied. The normalization walks the syntax tree and rewrites
    // every node with trivia stripped, so whitespace and comments do
    // not contribute to the hash.
    var codeVersion = codeVersionOverride ?? ComputeCodeVersion(classDecl);

    // Service-dependency extraction. Heuristic: interface-typed
    // parameters on any public static Create overload are treated as
    // DI service dependencies. Class- and value-typed parameters
    // (TimeZoneInfo, string, schema records, options) are closures
    // bound at the AddStep call site and not services. Multiple
    // Create overloads are unioned and deduped by fully-qualified
    // type name — StepMetadataResolver records identity per-step-class
    // not per-overload, so the registry must reflect the superset.
    var serviceTypes = ExtractServiceDependencies(typeSymbol);

    return new StepInfo(
      Namespace: ns,
      ClassName: typeSymbol.Name,
      Label: label ?? typeSymbol.Name,
      IsIdempotent: isIdempotent,
      HasSideEffects: hasSideEffects,
      CodeVersion: codeVersion,
      TypeArity: typeSymbol.IsGenericType ? typeSymbol.Arity : 0,
      ServiceTypeFqns: serviceTypes
    );
  }

  /// <summary>
  /// Collect fully-qualified type names of interface-typed parameters
  /// across every public static <c>Create</c> overload on the step
  /// class. The union (deduped, ordered) becomes the
  /// <c>ServiceRefs</c> array emitted on the <c>_Metadata</c>
  /// companion. Filters to <see cref="TypeKind.Interface"/> only —
  /// class- and value-typed Create params are treated as
  /// configuration closures, not services.
  /// </summary>
  private static System.Collections.Generic.IReadOnlyList<string> ExtractServiceDependencies(
    INamedTypeSymbol typeSymbol)
  {
    var seen = new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
    var ordered = new System.Collections.Generic.List<string>();
    foreach (var member in typeSymbol.GetMembers("Create"))
    {
      if (member is not IMethodSymbol method) continue;
      if (!method.IsStatic) continue;
      if (method.DeclaredAccessibility != Accessibility.Public) continue;
      foreach (var param in method.Parameters)
      {
        if (param.Type.TypeKind != TypeKind.Interface) continue;
        var fqn = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (seen.Add(fqn)) ordered.Add(fqn);
      }
    }
    return ordered;
  }

  /// <summary>
  /// Compute a stable, trivia-insensitive SHA-256 prefix over the
  /// step class's source text. Two passes:
  /// <list type="number">
  ///   <item><c>NormalizeWhitespace</c> rewrites the tree with canonical
  ///   single-space whitespace trivia;</item>
  ///   <item>The rewriter strips every comment and remaining whitespace
  ///   trivia node, leaving only token text.</item>
  /// </list>
  /// The resulting string is encoded as UTF-8 and hashed; the first
  /// <see cref="CodeVersionHexLength"/> hex chars become the
  /// <c>CodeVersion</c>.
  /// </summary>
  private static string ComputeCodeVersion(ClassDeclarationSyntax classDecl)
  {
    // Normalize whitespace first — collapses every formatting variant
    // (tabs, multi-blank-lines, alignment spaces) into a single canonical
    // shape. Comments survive normalization, so the trivia stripper
    // handles them next.
    var normalized = classDecl.NormalizeWhitespace(indentation: " ", eol: "\n");
    var stripped = TriviaStripper.Instance.Visit(normalized) ?? normalized;
    var canonicalText = stripped.ToFullString();

    using var sha = SHA256.Create();
    var bytes = Encoding.UTF8.GetBytes(canonicalText);
    var hash = sha.ComputeHash(bytes);
    var sb = new StringBuilder(CodeVersionHexLength);
    for (var i = 0; sb.Length < CodeVersionHexLength && i < hash.Length; i++)
    {
      sb.Append(hash[i].ToString("x2"));
    }
    if (sb.Length > CodeVersionHexLength)
    {
      sb.Length = CodeVersionHexLength;
    }
    return sb.ToString();
  }

  private static void Emit(SourceProductionContext ctx, StepInfo info)
  {
    var sb = new StringBuilder();
    sb.AppendLine("// <auto-generated/>");
    sb.AppendLine("#nullable enable");
    sb.AppendLine();
    sb.AppendLine("using global::Flowthru.Step;");
    sb.AppendLine();
    if (!string.IsNullOrEmpty(info.Namespace))
    {
      sb.AppendLine($"namespace {info.Namespace};");
      sb.AppendLine();
    }
    sb.AppendLine($"/// <summary>Generated metadata companion for <see cref=\"{info.ClassName}\"/>.</summary>");
    sb.AppendLine($"public static class {info.ClassName}_Metadata");
    sb.AppendLine("{");
    sb.AppendLine($"  public const string ClassName = \"{Escape(info.ClassName)}\";");
    sb.AppendLine($"  public const string Label = \"{Escape(info.Label)}\";");
    sb.AppendLine($"  public const string CodeVersion = \"{Escape(info.CodeVersion)}\";");
    sb.AppendLine();
    sb.AppendLine("  public static readonly global::Flowthru.Step.StepTraits Traits = new()");
    sb.AppendLine("  {");
    sb.AppendLine($"    IsIdempotent = {(info.IsIdempotent ? "true" : "false")},");
    sb.AppendLine($"    HasSideEffects = {(info.HasSideEffects ? "true" : "false")},");
    sb.AppendLine("  };");
    sb.AppendLine();
    // Service dependencies discovered from interface-typed Create
    // parameters. Empty when the step takes no services (the common
    // case). StepMetadataResolver.ResolveServicesFromDelegate reads
    // this array to populate IStepNode.ServiceDependencies at
    // FlowBuilder.AddStep time.
    sb.AppendLine(
      "  public static readonly global::Flowthru.Validation.Runtime.ServiceRef[] ServiceRefs = new global::Flowthru.Validation.Runtime.ServiceRef[]");
    sb.AppendLine("  {");
    foreach (var fqn in info.ServiceTypeFqns)
    {
      var variant = _observationOnlyFqns.Contains(fqn) ? "ObservationOnly" : "CSharp";
      sb.AppendLine($"    new global::Flowthru.Validation.Runtime.ServiceRef.{variant}(typeof({fqn})),");
    }
    sb.AppendLine("  };");
    sb.AppendLine("}");
    sb.AppendLine();

    // Module-initializer companion (Phase 8). Registers
    // (typeof(StepClass) -> CodeVersion) into StepMetadataRegistry at
    // module load time. The framework's FlowBuilder.AddStep resolves a
    // transform delegate back to its enclosing step class and reads the
    // registry directly — Flow developers never thread codeVersion by
    // hand for source-defined steps.
    // For generic step classes (e.g. PassthroughInputToOutputStep<T>),
    // register the open generic typedef — the StepMetadataResolver
    // canonicalizes to the open generic on lookup so every constructed
    // instantiation resolves to the same recorded CodeVersion.
    var typeofExpr = info.TypeArity == 0
      ? info.ClassName
      : info.ClassName + "<" + new string(',', info.TypeArity - 1) + ">";
    sb.AppendLine($"/// <summary>Auto-registers <see cref=\"{info.ClassName}\"/> with StepMetadataRegistry at module load.</summary>");
    sb.AppendLine($"internal static class {info.ClassName}_Registration");
    sb.AppendLine("{");
    sb.AppendLine("  [global::System.Runtime.CompilerServices.ModuleInitializer]");
    sb.AppendLine("  internal static void Register() =>");
    sb.AppendLine($"    global::Flowthru.Step.StepMetadataRegistry.Register(typeof({typeofExpr}), {info.ClassName}_Metadata.CodeVersion, {info.ClassName}_Metadata.ServiceRefs);");
    sb.AppendLine("}");

    var fileName = string.IsNullOrEmpty(info.Namespace)
      ? $"{info.ClassName}_Metadata.g.cs"
      : $"{info.Namespace}.{info.ClassName}_Metadata.g.cs";
    ctx.AddSource(fileName, SourceText.From(sb.ToString(), Encoding.UTF8));
  }

  private static string Escape(string value) =>
    value.Replace("\\", "\\\\").Replace("\"", "\\\"");

  /// <summary>
  /// <see cref="CSharpSyntaxRewriter"/> that drops every trivia node —
  /// comments, whitespace, end-of-line markers. The
  /// <see cref="ClassDeclarationSyntax"/> normalized upstream still
  /// carries token-level trivia spacing; this rewriter removes it so
  /// the hash sees only token text.
  /// </summary>
  private sealed class TriviaStripper : CSharpSyntaxRewriter
  {
    internal static readonly TriviaStripper Instance = new();

    public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia) => default;
  }
}

internal sealed record StepInfo(
  string Namespace,
  string ClassName,
  string Label,
  bool IsIdempotent,
  bool HasSideEffects,
  string CodeVersion,
  int TypeArity,
  System.Collections.Generic.IReadOnlyList<string> ServiceTypeFqns
);
