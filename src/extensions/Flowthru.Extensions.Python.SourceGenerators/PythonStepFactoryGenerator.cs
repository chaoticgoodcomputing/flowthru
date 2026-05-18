using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Flowthru.Step.Python.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Flowthru.Extensions.Python.SourceGenerators;

/// <summary>
/// Discovers Python files marked as <c>AdditionalFiles</c> in the
/// consuming project, parses every <c>@step(...)</c> decorator, and
/// emits a strongly-typed factory method per discovered step that
/// returns a <c>PythonStep&lt;TIn,TOut&gt;</c>. The schema names in
/// the decorator are resolved against the consuming compilation's
/// <c>[FlowthruSchema]</c>-decorated record types — unresolved names
/// surface as build errors (FT2xxx range), giving the user the
/// strongest possible compile-time agreement between the Python and
/// C# sides.
/// </summary>
/// <remarks>
/// <para>
/// Decorator authoring grammar — both forms accepted:
/// </para>
/// <code>
/// @step(inputs=[FeatureVectorSchema], outputs=[ModelWeightsSchema])
/// @step(inputs=["FeatureVectorSchema"], outputs=["ModelWeightsSchema"])
/// </code>
/// <para>
/// <strong>Generated factory shape:</strong>
/// </para>
/// <code>
/// public static IItem&lt;...&gt; TrainModel(IPythonExecutor executor, IItem&lt;TIn&gt; input, IItem&lt;TOut&gt; output)
///   =&gt; ...constructs PythonStep&lt;TIn,TOut&gt;...
/// </code>
/// </remarks>
[Generator]
public class PythonStepFactoryGenerator : IIncrementalGenerator
{
  // Diagnostic identifiers — FT2xxx range (composition / wiring).
  //
  // Severity is Warning rather than Error because the
  // <c>@step(outputs=...)</c> decorator legitimately accepts catalog-
  // item label strings (e.g. <c>outputs="CoverageHeatmap"</c> where
  // <c>CoverageHeatmap</c> is an <c>IItem&lt;byte[]&gt;</c> with no
  // backing [FlowthruSchema] type — Plotly outputs, binary blobs,
  // <c>DirectoryOf&lt;byte[]&gt;</c>, etc.). Consumers using the
  // string-based <c>pipeline.AddPythonStep&lt;TIn, TOut&gt;</c> path
  // never reference the generated factory method for those steps and
  // so never trip a compile error; consumers using the named factory
  // (<c>PythonSteps.{X}(...)</c>) hit a downstream compile error
  // because the factory's signature uses <c>object</c> for unresolved
  // type parameters — that downstream error is the actionable one,
  // so FT2007 here serves as advisory rather than build-breaking.
  private static readonly DiagnosticDescriptor SchemaNotFound = new(
    id: "FT2007",
    title: "Python decorator references unknown schema",
    // `{{X}}` escapes a literal `{X}` for string.Format — without the escape the formatter trips on `{X}` (X isn't a valid argument index), the substitution throws, and Roslyn surfaces the raw unformatted template so consumers see a literal `{0}` instead of the schema name. Reported by MagicAtlas in 0.18.2.
    messageFormat: "Schema '{0}' referenced in @step decorator is not a [FlowthruSchema]-decorated type in the consuming compilation. Named-factory consumers (PythonSteps.{{X}}) will see a downstream compile error.",
    category: "Flowthru.Python",
    DiagnosticSeverity.Warning,
    isEnabledByDefault: true
  );

  /// <summary>
  /// Flags <c>[FlowthruSchema]</c> properties whose CLR type the
  /// <c>ArrowMarshaller</c> cannot encode/decode when the schema is
  /// referenced by a Python <c>@step(...)</c> decorator. This turns a
  /// runtime <c>NotSupportedException</c> from <c>BuildArrayFromValues</c>
  /// into a compile-time diagnostic — the gold-standard placement under
  /// CONTRIBUTING.md's three-error-phase model.
  /// </summary>
  private static readonly DiagnosticDescriptor SchemaUnmarshallable = new(
    id: "FT2008",
    title: "Python step schema contains a property type Arrow cannot marshal",
    messageFormat: "Schema '{0}' (used by Python step '{1}') has property '{2}' of type '{3}', which is not supported by the Python extension's Arrow marshaller. Supported types: int, long, float, double, bool, string, DateTime, DateTimeOffset, TimeSpan, Guid, byte[], enum, list/array of any supported type, and their nullable variants.",
    category: "Flowthru.Python",
    DiagnosticSeverity.Error,
    isEnabledByDefault: true
  );

  /// <summary>
  /// Closes the same loop as <see cref="SchemaUnmarshallable"/>, but
  /// for the C# escape hatch: <c>builder.AddPythonStep&lt;TIn, TOut&gt;</c>
  /// invocations bypass the <c>@step</c>-decorator code path entirely,
  /// so an unmarshallable property reachable from <c>TIn</c> or
  /// <c>TOut</c> would otherwise only surface at runtime as a wrapped
  /// <c>NotSupportedException</c>. Flagging it at the call site keeps
  /// CONTRIBUTING.md's "if it compiles, Arrow can encode it" promise.
  /// </summary>
  private static readonly DiagnosticDescriptor AddPythonStepUnmarshallable = new(
    id: "FT2009",
    title: "Python step type argument contains a property type Arrow cannot marshal",
    messageFormat: "Type argument '{0}' on AddPythonStep contains property '{1}' of type '{2}', which the Python extension's Arrow marshaller cannot encode. Supported types: int, long, float, double, bool, string, DateTime, DateTimeOffset, TimeSpan, Guid, byte[], enum, list/array of any supported type, and their nullable variants.",
    category: "Flowthru.Python",
    DiagnosticSeverity.Error,
    isEnabledByDefault: true
  );

  /// <inheritdoc/>
  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    // Discover all .py AdditionalFiles in the consuming project. Each
    // file can host multiple @step decorators, so the select stage
    // flattens by returning every step found in the file.
    var pythonSteps = context.AdditionalTextsProvider
      .Where(file => file.Path.EndsWith(".py"))
      .SelectMany((file, ct) =>
      {
        var content = file.GetText(ct)?.ToString();
        if (string.IsNullOrEmpty(content)) return ImmutableArray<PythonStepInfo>.Empty;
        return ImmutableArray.CreateRange(ParsePythonSteps(file.Path, content!));
      })
      .Collect();

    // Combine with the compilation's [FlowthruSchema] type registry.
    var combined = context.CompilationProvider.Combine(pythonSteps);

    context.RegisterSourceOutput(combined, (ctx, pair) =>
    {
      var (compilation, steps) = pair;
      if (steps.Length == 0) return;

      var schemaIndex = BuildSchemaIndex(compilation);
      var sb = new StringBuilder();
      sb.AppendLine(
        """
        // <auto-generated/>
        #nullable enable

        using global::Flowthru.Data.Catalog;
        using global::Flowthru.Step.Python;
        using global::Flowthru.Validation.Runtime;

        namespace Flowthru.Extensions.Python.Generated;

        /// <summary>
        /// Compile-time-discovered factory methods for Python steps.
        /// Each method returns a <c>PythonStep&lt;TIn,TOut&gt;</c> ready
        /// to add to a <c>FlowBuilder</c> via <c>builder.Add(step)</c>.
        /// Schemas referenced in the @step decorator are resolved
        /// against the consuming project's [FlowthruSchema] types; a
        /// missing schema is a build error.
        /// </summary>
        public static class PythonSteps
        {
        """
      );

      foreach (var step in steps)
      {
        EmitFactory(ctx, sb, step!, schemaIndex);
      }

      sb.AppendLine("}");

      // Emit a `[ModuleInitializer]` companion class that populates
      // PythonStepCacheRegistry for every @step(cacheable=True) discovered
      // in the project. The matrix-generated AddPythonStep overloads
      // consult the registry to decide whether to auto-derive a
      // CodeVersion. Registration is idempotent, so re-running the
      // initializer (e.g. across multiple test fixtures) is safe.
      EmitCacheRegistration(sb, steps);

      ctx.AddSource("PythonSteps.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    });

    // FT2009: walk every call site of AddPythonStep<...>(...) in the
    // consuming compilation and report unmarshallable property types
    // reachable from TIn or TOut. This is the C#-escape-hatch twin of
    // FT2008 (which flags the @step-decorator path).
    var addPythonStepInvocations = context.SyntaxProvider
      .CreateSyntaxProvider(
        predicate: static (node, _) =>
          node is InvocationExpressionSyntax inv && InvocationNameMatchesAddPythonStep(inv),
        transform: static (gsc, _) => (InvocationExpressionSyntax)gsc.Node
      )
      .Collect();

    var addPythonStepCombined = context.CompilationProvider.Combine(addPythonStepInvocations);

    context.RegisterSourceOutput(addPythonStepCombined, (ctx, pair) =>
    {
      var (compilation, invocations) = pair;
      if (invocations.IsDefaultOrEmpty) return;
      AnalyzeAddPythonStepCallSites(ctx, compilation, invocations);
    });
  }

  /// <summary>
  /// Cheap syntactic predicate that matches any invocation whose called
  /// name is <c>AddPythonStep</c> — qualified, unqualified, or via a
  /// fluent <c>.AddPythonStep&lt;...&gt;(...)</c> chain. Symbol-level
  /// verification (the right factory, the right arity) happens in the
  /// transform pass.
  /// </summary>
  private static bool InvocationNameMatchesAddPythonStep(InvocationExpressionSyntax invocation)
  {
    var expr = invocation.Expression;
    return expr switch
    {
      MemberAccessExpressionSyntax m => IsAddPythonStepIdentifier(m.Name),
      MemberBindingExpressionSyntax mb => IsAddPythonStepIdentifier(mb.Name),
      IdentifierNameSyntax id => id.Identifier.ValueText == "AddPythonStep",
      GenericNameSyntax gn => gn.Identifier.ValueText == "AddPythonStep",
      _ => false,
    };
  }

  private static bool IsAddPythonStepIdentifier(SimpleNameSyntax name) =>
    name.Identifier.ValueText == "AddPythonStep";

  // ── Decorator parsing ─────────────────────────────────────────────────

  /// <summary>
  /// Parse every <c>@step(...)</c>-decorated function in <paramref name="content"/>.
  /// A single <c>.py</c> file can host multiple decorators (MagicAtlas's
  /// embed_oracle_text.py defines an <c>embed_default</c> and an
  /// <c>embed_finetuned</c> in one module, for example) — the previous
  /// implementation returned only the first match via <c>Regex.Match</c>
  /// and silently dropped every subsequent decorator, which left those
  /// steps uncacheable at runtime with no diagnostic.
  /// </summary>
  private static IEnumerable<PythonStepInfo> ParsePythonSteps(string filePath, string content)
  {
    // Match @step(inputs=..., outputs=...) with optional services=[...]
    // and optional cacheable=True/False.
    //
    // Inputs/outputs accept three shapes, mirroring the Python
    // decorator's runtime tolerance:
    //   - bracketed list: outputs=["X", "Y"]
    //   - bare string:    outputs="X"          (sugar for ["X"])
    //   - None:           outputs=None         (no outputs)
    //
    // Whichever capture is non-empty is fed to ParseSchemaList, which
    // accepts string literals, bare identifiers, and dotted forms.
    // The bare-string sugar was supported by the Python @step decorator
    // from day one but was previously absent from the C# regex —
    // decorators using it (e.g. FlowthruCoverage's
    // `outputs="CoverageHeatmap"`) silently never matched, so the
    // generator emitted no factory/registration for those steps.
    var decoratorPattern =
      @"@step\s*\(\s*inputs\s*=\s*(?:(\[[^\]]*\])|(""[^""]+"")|None)"
      + @"\s*,\s*outputs\s*=\s*(?:(\[[^\]]*\])|(""[^""]+"")|None)"
      + @"(?:\s*,\s*services\s*=\s*\[[^\]]*\])?"
      + @"(?:\s*,\s*cacheable\s*=\s*(True|False))?"
      // Trailing comma before the closing paren is valid Python and
      // common when decorators span multiple lines.
      + @"\s*,?\s*\)";
    var functionPattern = @"def\s+(\w+)\s*\(";
    var modulePath = DeriveModulePath(filePath);

    foreach (Match decoratorMatch in Regex.Matches(content, decoratorPattern))
    {
      // Each decorator's `def` is the FIRST `def` that follows the
      // decorator's closing paren (not just its start), so we search
      // from after the match's end. Searching from `decoratorMatch.Index`
      // would re-attach a downstream decorator's def to an upstream
      // decorator when both live in the same file.
      var searchStart = decoratorMatch.Index + decoratorMatch.Length;
      var functionMatch = Regex.Match(content.Substring(searchStart), functionPattern);
      if (!functionMatch.Success) continue;

      var functionName = functionMatch.Groups[1].Value;
      // Groups 1 + 2 → inputs (bracketed list or bare string). Group 3 + 4
      // → outputs (same). Group 5 → cacheable. An empty capture means the
      // other alternative matched, OR the value was `None` (no captures).
      var inputsBracketed = decoratorMatch.Groups[1].Value;
      var inputsBareString = decoratorMatch.Groups[2].Value;
      var outputsBracketed = decoratorMatch.Groups[3].Value;
      var outputsBareString = decoratorMatch.Groups[4].Value;
      var cacheableRaw = decoratorMatch.Groups[5].Value;

      var inputsRaw = !string.IsNullOrEmpty(inputsBracketed) ? inputsBracketed : inputsBareString;
      var outputsRaw = !string.IsNullOrEmpty(outputsBracketed) ? outputsBracketed : outputsBareString;

      // Both inputs and outputs collapse to an empty schema list when the
      // captured text is empty — the only way that happens is the
      // `inputs=None` / `outputs=None` branch of the regex's alternation
      // (which captures nothing in either bracketed or bare-string group).
      var inputs = ParseSchemaList(inputsRaw);
      var outputs = ParseSchemaList(outputsRaw);
      var cacheable = string.Equals(cacheableRaw, "True", StringComparison.Ordinal);

      // Per-decorator Location so FT2007 (and any future per-decorator
      // diagnostic) points the IDE at the exact @step(...) span, not the
      // project root. Without this, a consumer with N broken decorators
      // sees N project-level warnings with no file/line to navigate to.
      var decoratorLocation = ComputeLocation(
        filePath, content, decoratorMatch.Index, decoratorMatch.Length);

      yield return new PythonStepInfo(
        functionName, modulePath, inputs, outputs, cacheable, filePath, decoratorLocation);
    }
  }

  /// <summary>
  /// Build a Roslyn <see cref="Location"/> from a byte offset + length
  /// into a non-C# source file (here, a <c>.py</c> AdditionalFile).
  /// Walks the content once to count newlines so the
  /// <see cref="LinePositionSpan"/> is correctly populated for IDE
  /// navigation. Returns <see cref="Location.None"/> when offsets are
  /// out of range (defensive — should be impossible if the caller
  /// passes a regex Match against the same content).
  /// </summary>
  private static Location ComputeLocation(string filePath, string content, int start, int length)
  {
    if (start < 0 || length <= 0 || start + length > content.Length)
      return Location.None;

    var startLine = 0;
    var startColumn = 0;
    for (var i = 0; i < start; i++)
    {
      if (content[i] == '\n') { startLine++; startColumn = 0; }
      else startColumn++;
    }
    var endLine = startLine;
    var endColumn = startColumn;
    for (var i = start; i < start + length; i++)
    {
      if (content[i] == '\n') { endLine++; endColumn = 0; }
      else endColumn++;
    }

    return Location.Create(
      filePath,
      new TextSpan(start, length),
      new LinePositionSpan(
        new LinePosition(startLine, startColumn),
        new LinePosition(endLine, endColumn)
      )
    );
  }

  /// <summary>
  /// Extract identifier or string-literal entries from the
  /// bracket-delimited decorator argument. Accepts both
  /// <c>["FeatureVectorSchema"]</c> and <c>[FeatureVectorSchema]</c>
  /// forms; the resulting list contains schema names regardless of
  /// authoring style.
  /// </summary>
  private static List<string> ParseSchemaList(string raw)
  {
    // Match either a quoted string or a bare identifier (incl. dotted).
    var pattern = @"""([^""]+)""|([A-Za-z_][A-Za-z0-9_\.]*)";
    var matches = Regex.Matches(raw, pattern);
    var result = new List<string>();
    foreach (Match m in matches)
    {
      if (m.Groups[1].Success)
      {
        var s = m.Groups[1].Value;
        if (!string.IsNullOrWhiteSpace(s)) result.Add(s);
      }
      else if (m.Groups[2].Success)
      {
        var s = m.Groups[2].Value;
        // Skip None and reserved-ish words.
        if (s == "None" || s == "True" || s == "False") continue;
        // Take the rightmost segment so `Module.Schema` resolves to `Schema`.
        var dotIdx = s.LastIndexOf('.');
        result.Add(dotIdx < 0 ? s : s.Substring(dotIdx + 1));
      }
    }
    return result;
  }

  private static string DeriveModulePath(string filePath)
  {
    // Walk from the first "Flows" or "Steps" anchor downward and
    // produce a dotted module path with the .py suffix stripped.
    var parts = filePath.Replace('\\', '/').Split('/');
    var relevant = new List<string>();
    var capturing = false;
    foreach (var part in parts)
    {
      if (part == "Flows" || capturing)
      {
        capturing = true;
        if (part.EndsWith(".py")) relevant.Add(part.Substring(0, part.Length - 3));
        else if (!string.IsNullOrEmpty(part)) relevant.Add(part);
      }
    }
    return string.Join(".", relevant);
  }

  // ── Schema lookup ─────────────────────────────────────────────────────

  /// <summary>
  /// Index every <c>[FlowthruSchema]</c>-decorated type in the
  /// compilation by its short name (the same name that the Python
  /// <c>@step(inputs=[Foo])</c> decorator captures via
  /// <c>__qualname__</c>). Used by <see cref="EmitFactory"/> to
  /// resolve decorator references and report unknowns as
  /// <see cref="SchemaNotFound"/> diagnostics.
  /// </summary>
  private static Dictionary<string, INamedTypeSymbol> BuildSchemaIndex(Compilation compilation)
  {
    var index = new Dictionary<string, INamedTypeSymbol>(StringComparer.Ordinal);
    var attrType = compilation.GetTypeByMetadataName("Flowthru.Data.Schema.FlowthruSchemaAttribute");
    if (attrType is null) return index;

    foreach (var type in EnumerateAllTypes(compilation.GlobalNamespace))
    {
      foreach (var attr in type.GetAttributes())
      {
        if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attrType))
        {
          index[type.Name] = type;
          break;
        }
      }
    }
    return index;
  }

  private static IEnumerable<INamedTypeSymbol> EnumerateAllTypes(INamespaceSymbol root)
  {
    foreach (var member in root.GetMembers())
    {
      if (member is INamespaceSymbol ns)
      {
        foreach (var inner in EnumerateAllTypes(ns)) yield return inner;
      }
      else if (member is INamedTypeSymbol nt)
      {
        yield return nt;
        foreach (var nested in nt.GetTypeMembers()) yield return nested;
      }
    }
  }

  // ── Cache registration emission ───────────────────────────────────────

  /// <summary>
  /// Emit a <c>[ModuleInitializer]</c>-decorated method that registers
  /// every <c>cacheable=True</c> Python step with the global
  /// <c>PythonStepCacheRegistry</c>. The matrix-generated
  /// <c>AddPythonStep</c> overloads consult this registry at step
  /// construction time to decide whether to auto-derive a CodeVersion.
  /// Skipped entirely when no step opted in.
  /// </summary>
  private static void EmitCacheRegistration(StringBuilder sb, System.Collections.Immutable.ImmutableArray<PythonStepInfo> steps)
  {
    var cacheable = steps.Where(s => s.Cacheable).ToList();
    if (cacheable.Count == 0) return;

    sb.AppendLine();
    sb.AppendLine(
      """
      /// <summary>
      /// Module initializer that registers every @step(cacheable=True)
      /// function in this project with the global Python step cache
      /// registry. Runs once at assembly load — registration is
      /// idempotent so test harnesses re-loading assemblies are safe.
      /// </summary>
      internal static class PythonStepCacheRegistration
      {
        [global::System.Runtime.CompilerServices.ModuleInitializer]
        internal static void Init()
        {
      """
    );

    foreach (var step in cacheable)
    {
      var pyPathLiteral = ToCSharpStringLiteral(step.PyFilePath);
      var candidatePaths = ComposeLockfileCandidates(step.PyFilePath);
      var candidateLiterals = string.Join(", ", candidatePaths.Select(ToCSharpStringLiteral));
      sb.AppendLine(
        $"      global::Flowthru.Step.Python.PythonStepCacheRegistry.Register("
          + $"\"{step.ModulePath}\", \"{step.FunctionName}\", {pyPathLiteral}"
          + (candidateLiterals.Length == 0 ? "" : ", " + candidateLiterals)
          + ");"
      );
    }

    sb.AppendLine("    }");
    sb.AppendLine("  }");
  }

  /// <summary>
  /// Compose the list of candidate lockfile paths for a given .py file,
  /// walking up the directory hierarchy and emitting each plausible
  /// name at each level in priority order. Source generators are barred
  /// from filesystem IO (RS1035), so existence checking is deferred to
  /// runtime — the runtime <c>Entry.ResolveLockfile()</c> picks the
  /// first candidate that actually exists.
  /// </summary>
  /// <remarks>
  /// Priority order matches what reproducible-build tooling actually
  /// pins: <c>uv.lock</c> → <c>poetry.lock</c> →
  /// <c>requirements.txt</c> → <c>Pipfile.lock</c> →
  /// <c>pyproject.toml</c>. The pyproject fallback is over-broad
  /// (constraints, not resolutions) but still produces a meaningful
  /// identity dimension when no lockfile exists.
  /// </remarks>
  private static List<string> ComposeLockfileCandidates(string pyFilePath)
  {
    var names = new[] { "uv.lock", "poetry.lock", "requirements.txt", "Pipfile.lock", "pyproject.toml" };
    var paths = new List<string>();
    var dir = Path.GetDirectoryName(pyFilePath);
    // Cap traversal at 10 levels — generous for any realistic repo and
    // guards against malformed input (unrooted paths, junctions) that
    // could otherwise loop the walk.
    for (var depth = 0; depth < 10 && !string.IsNullOrEmpty(dir); depth++)
    {
      foreach (var name in names)
      {
        paths.Add(Path.Combine(dir!, name));
      }
      var parent = Path.GetDirectoryName(dir);
      if (parent == dir || string.IsNullOrEmpty(parent)) break;
      dir = parent;
    }
    return paths;
  }

  /// <summary>
  /// Render a string as a valid C# string literal: escape backslashes
  /// and quotes. Path separators on Windows need this; *nix paths
  /// generally don't, but the round-trip cost is nothing.
  /// </summary>
  private static string ToCSharpStringLiteral(string value) =>
    "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

  // ── Factory emission ──────────────────────────────────────────────────

  private static void EmitFactory(
    SourceProductionContext ctx,
    StringBuilder sb,
    PythonStepInfo step,
    Dictionary<string, INamedTypeSymbol> schemaIndex
  )
  {
    var inputTypes = new List<string>();
    var anyMissing = false;
    foreach (var schemaName in step.Inputs)
    {
      var typeRef = ResolveSchemaReference(
        ctx, schemaName, schemaIndex, step.DecoratorLocation, ref anyMissing);
      inputTypes.Add(typeRef);
      ReportUnmarshallableProperties(ctx, schemaName, step.FunctionName, schemaIndex);
    }
    var outputTypes = new List<string>();
    foreach (var schemaName in step.Outputs)
    {
      var typeRef = ResolveSchemaReference(
        ctx, schemaName, schemaIndex, step.DecoratorLocation, ref anyMissing);
      outputTypes.Add(typeRef);
      ReportUnmarshallableProperties(ctx, schemaName, step.FunctionName, schemaIndex);
    }
    if (anyMissing) return; // Diagnostics already emitted; skip generation.

    var inputTuple = JoinAsTupleOrSingle(inputTypes);
    var outputTuple = JoinAsTupleOrSingle(outputTypes);
    var methodName = ToPascalCase(step.FunctionName);

    sb.AppendLine(
      $$"""
        /// <summary>
        /// Compile-time factory for the Python step <c>{{step.ModulePath}}.{{step.FunctionName}}</c>.
        /// Schemas resolved at build time against [FlowthruSchema] types in this compilation.
        /// </summary>
        public static global::Flowthru.Step.Python.PythonStep<{{inputTuple}}, {{outputTuple}}> {{methodName}}(
          string label,
          global::Flowthru.Step.Python.IPythonExecutor executor,
          {{EmitInputItemParameters(inputTypes)}},
          {{EmitOutputItemParameters(outputTypes)}},
          System.Collections.Generic.IReadOnlyList<global::Flowthru.Validation.Runtime.ServiceRef>? services = null,
          string? codeVersion = null
        )
        {
          return new global::Flowthru.Step.Python.PythonStep<{{inputTuple}}, {{outputTuple}}>(
            label: label,
            moduleName: "{{step.ModulePath}}",
            functionName: "{{step.FunctionName}}",
            transform: tin => executor.Invoke<{{inputTuple}}, {{outputTuple}}>("{{step.ModulePath}}", "{{step.FunctionName}}", tin),
            inputs: new global::Flowthru.Data.Catalog.IItem[] { {{string.Join(", ", Enumerable.Range(1, inputTypes.Count).Select(i => $"input{i}"))}} },
            outputs: new global::Flowthru.Data.Catalog.IItem[] { {{string.Join(", ", Enumerable.Range(1, outputTypes.Count).Select(i => $"output{i}"))}} },
            loadInputs: () => {{LoadInputsExpr(inputTypes.Count)}},
            saveOutputs: {{SaveOutputsExpr(outputTypes.Count)}},
            serviceDependencies: services,
            codeVersion: codeVersion
          );
        }
        """
    );
    sb.AppendLine();
  }

  private static string ResolveSchemaReference(
    SourceProductionContext ctx,
    string schemaName,
    Dictionary<string, INamedTypeSymbol> schemaIndex,
    Location decoratorLocation,
    ref bool anyMissing
  )
  {
    // Wire-format primitives the user can write directly; map to canonical C# types.
    if (schemaName == "bytes") return "byte[]";
    if (schemaName == "str") return "string";
    if (schemaName == "int") return "int";
    if (schemaName == "float") return "double";
    if (schemaName == "bool") return "bool";
    if (schemaName == "object") return "object";

    if (!schemaIndex.TryGetValue(schemaName, out var symbol))
    {
      // FT2007 fires at the @step(...) decorator span (computed during
      // ParsePythonSteps). MagicAtlas reported 0.18.2 emitting these
      // project-level — without a Location, the IDE has no anchor and a
      // consumer with N misses cannot tell which decorator each refers to.
      ctx.ReportDiagnostic(Diagnostic.Create(SchemaNotFound, decoratorLocation, schemaName));
      anyMissing = true;
      return $"/* unresolved: {schemaName} */ object";
    }

    return $"global::System.Collections.Generic.IEnumerable<{symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>";
  }

  private static string JoinAsTupleOrSingle(List<string> types)
  {
    if (types.Count == 0) return "object";
    if (types.Count == 1) return types[0];
    return $"({string.Join(", ", types)})";
  }

  private static string EmitInputItemParameters(List<string> inputTypes) =>
    string.Join(", ", inputTypes.Select((t, i) => $"global::Flowthru.Data.Catalog.IItem<{t}> input{i + 1}"));

  private static string EmitOutputItemParameters(List<string> outputTypes) =>
    string.Join(", ", outputTypes.Select((t, i) => $"global::Flowthru.Data.Catalog.IItem<{t}> output{i + 1}"));

  private static string LoadInputsExpr(int n)
  {
    if (n == 1) return "input1.Load()";
    var letters = Enumerable.Range(1, n).Select(i => $"v{i}").ToArray();
    var sb = new StringBuilder();
    for (int i = 1; i <= n; i++) sb.Append($"from {letters[i - 1]} in input{i}.Load() ");
    sb.Append($"select ({string.Join(", ", letters)})");
    return sb.ToString();
  }

  private static string SaveOutputsExpr(int n)
  {
    if (n == 1) return "out_ => output1.Save(out_)";
    var letters = Enumerable.Range(1, n).Select(i => $"o{i}").ToArray();
    var dummies = Enumerable.Range(1, n).Select(i => $"_x{i}").ToArray();
    var sb = new StringBuilder();
    sb.Append("out_ => { var (");
    sb.Append(string.Join(", ", letters));
    sb.Append(") = out_; return ");
    for (int i = 1; i <= n; i++) sb.Append($"from {dummies[i - 1]} in output{i}.Save({letters[i - 1]}) ");
    sb.Append("select global::Flowthru.Prelude.FlowUnit.Default; }");
    return sb.ToString();
  }

  /// <summary>
  /// Walk a resolved schema symbol's properties and emit
  /// <see cref="SchemaUnmarshallable"/> for any whose type the
  /// Arrow marshaller doesn't accept. The set must stay in sync with
  /// <c>ArrowMarshaller.BuildArrayFromValues</c> / <c>GetValueFromArray</c>
  /// in the runtime extension — extending one without the other turns
  /// this analyzer into a misdirection.
  /// </summary>
  private static void ReportUnmarshallableProperties(
    SourceProductionContext ctx,
    string schemaName,
    string stepFunctionName,
    Dictionary<string, INamedTypeSymbol> schemaIndex
  )
  {
    // Wire-format primitives don't resolve to a [FlowthruSchema] symbol;
    // their property surface isn't ours to validate.
    if (!schemaIndex.TryGetValue(schemaName, out var symbol)) return;

    foreach (var member in symbol.GetMembers())
    {
      if (member is not IPropertySymbol property) continue;
      if (property.IsIndexer) continue;
      if (property.DeclaredAccessibility != Accessibility.Public) continue;
      if (property.IsStatic) continue;

      if (IsMarshallable(property.Type)) continue;

      var location = property.Locations.Length > 0 ? property.Locations[0] : Location.None;
      ctx.ReportDiagnostic(
        Diagnostic.Create(
          SchemaUnmarshallable,
          location,
          schemaName,
          stepFunctionName,
          property.Name,
          property.Type.ToDisplayString()
        )
      );
    }
  }

  private static readonly HashSet<string> _marshallableLeafNames =
    new(PythonMarshallableTypeNames.All, StringComparer.Ordinal);

  /// <summary>
  /// Mirror of <c>ArrowMarshaller.BuildArrayFromValues</c>'s accepted set,
  /// expressed against Roslyn symbols. Nullable value types are unwrapped;
  /// list/array element types are checked recursively. Leaf names are
  /// matched against the shared <see cref="PythonMarshallableTypeNames"/>
  /// list so the analyzer cannot drift from the runtime registry.
  /// </summary>
  private static bool IsMarshallable(ITypeSymbol type)
  {
    if (type is INamedTypeSymbol named
        && named.ConstructedFrom?.SpecialType == SpecialType.System_Nullable_T)
    {
      return IsMarshallable(named.TypeArguments[0]);
    }

    if (type.TypeKind == TypeKind.Enum) return true;

    if (_marshallableLeafNames.Contains(CanonicalLeafName(type))) return true;

    if (type is IArrayTypeSymbol arr && arr.Rank == 1)
    {
      // byte[] is itself a marshallable leaf — only recurse for any other
      // array element so e.g. int[] passes via the recursion.
      return IsMarshallable(arr.ElementType);
    }

    if (type is INamedTypeSymbol generic)
    {
      var enumerable = FindIEnumerableElement(generic);
      if (enumerable is not null)
      {
        return IsMarshallable(enumerable);
      }
    }

    return false;
  }

  /// <summary>
  /// Canonical, alias-free leaf name suitable for matching the shared
  /// <see cref="PythonMarshallableTypeNames.All"/> list. SpecialType
  /// primitives short-circuit to their <c>System.*</c> form so the
  /// C# alias display (<c>int</c>, <c>string</c>) doesn't miss the
  /// match. <c>byte[]</c> is the only special-case array leaf.
  /// </summary>
  private static string CanonicalLeafName(ITypeSymbol type)
  {
    switch (type.SpecialType)
    {
      case SpecialType.System_Int32: return "System.Int32";
      case SpecialType.System_Int64: return "System.Int64";
      case SpecialType.System_Single: return "System.Single";
      case SpecialType.System_Double: return "System.Double";
      case SpecialType.System_Boolean: return "System.Boolean";
      case SpecialType.System_String: return "System.String";
    }

    if (type is IArrayTypeSymbol arr
        && arr.Rank == 1
        && arr.ElementType.SpecialType == SpecialType.System_Byte)
    {
      return "System.Byte[]";
    }

    // Strip nullable annotation; full namespace + name.
    var fmt = new SymbolDisplayFormat(
      typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
      genericsOptions: SymbolDisplayGenericsOptions.None,
      miscellaneousOptions: SymbolDisplayMiscellaneousOptions.None
    );
    return type.WithNullableAnnotation(NullableAnnotation.None).ToDisplayString(fmt);
  }

  private static ITypeSymbol? FindIEnumerableElement(INamedTypeSymbol type)
  {
    // If `type` is itself IEnumerable<T>, use its arg.
    if (type.ConstructedFrom?.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
    {
      return type.TypeArguments[0];
    }

    foreach (var iface in type.AllInterfaces)
    {
      if (iface.ConstructedFrom?.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
      {
        return iface.TypeArguments[0];
      }
    }

    return null;
  }

  private static string ToPascalCase(string snakeCase)
  {
    var parts = snakeCase.Split('_');
    return string.Join("", parts.Select(p => p.Length == 0 ? p : char.ToUpperInvariant(p[0]) + p.Substring(1)));
  }

  // ── FT2009: AddPythonStep<TIn, TOut>(...) call-site analysis ─────────

  /// <summary>
  /// Walk every <c>AddPythonStep&lt;...&gt;(...)</c> invocation seen by
  /// the syntax provider, resolve it to an <see cref="IMethodSymbol"/>,
  /// and surface FT2009 for any <c>[FlowthruSchema]</c> reachable from
  /// a type argument that has an unmarshallable property.
  /// </summary>
  private static void AnalyzeAddPythonStepCallSites(
    SourceProductionContext ctx,
    Compilation compilation,
    System.Collections.Immutable.ImmutableArray<InvocationExpressionSyntax> invocations
  )
  {
    // Cheap up-front gate: if the consuming compilation has no Python
    // factory symbol, nothing to do (e.g. analyzer running in a project
    // that pulled in the source-gen but not the runtime).
    var factoryType = compilation.GetTypeByMetadataName("Flowthru.Flow.PythonStepFactory");
    if (factoryType is null) return;

    foreach (var invocation in invocations)
    {
      var model = compilation.GetSemanticModel(invocation.SyntaxTree);
      var symbolInfo = model.GetSymbolInfo(invocation);
      var method = symbolInfo.Symbol as IMethodSymbol
        ?? symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();
      if (method is null) continue;

      // For extension-method calls Roslyn reports the ReducedFrom symbol;
      // walk back to the original definition so we can match against the
      // factory's container.
      var owner = (method.ReducedFrom ?? method.OriginalDefinition).ContainingType;
      if (!SymbolEqualityComparer.Default.Equals(owner, factoryType)) continue;

      var typeArguments = method.TypeArguments;
      if (typeArguments.IsDefaultOrEmpty) continue;

      var typeArgumentSyntaxNodes = GetTypeArgumentSyntax(invocation);

      for (int i = 0; i < typeArguments.Length; i++)
      {
        var typeArg = typeArguments[i];
        var slotLabel = method.TypeParameters[i].Name;
        var location = i < typeArgumentSyntaxNodes.Count
          ? typeArgumentSyntaxNodes[i].GetLocation()
          : invocation.GetLocation();

        ReportUnmarshallableSchemaProperties(ctx, typeArg, slotLabel, location);
      }
    }
  }

  /// <summary>
  /// Extract the <c>TypeSyntax</c> nodes from an
  /// <c>AddPythonStep&lt;...&gt;</c> invocation so diagnostics can land
  /// on the exact <c>TInN</c> / <c>TOutN</c> token. Returns an empty
  /// list when the type arguments were inferred (no
  /// <c>GenericNameSyntax</c> to read).
  /// </summary>
  private static IReadOnlyList<TypeSyntax> GetTypeArgumentSyntax(InvocationExpressionSyntax invocation)
  {
    GenericNameSyntax? generic = invocation.Expression switch
    {
      GenericNameSyntax gn => gn,
      MemberAccessExpressionSyntax ma when ma.Name is GenericNameSyntax g => g,
      MemberBindingExpressionSyntax mb when mb.Name is GenericNameSyntax g => g,
      _ => null,
    };
    return generic?.TypeArgumentList.Arguments.ToList() ?? new List<TypeSyntax>();
  }

  /// <summary>
  /// Unwrap a single type-argument slot down to the
  /// <c>[FlowthruSchema]</c>-decorated leaf records it transitively
  /// reaches, then emit FT2009 for any unmarshallable property on
  /// each. The unwrap covers tabular shapes
  /// (<c>IEnumerable&lt;T&gt;</c>, <c>T[]</c>), the
  /// <c>Flowthru.Data.Storage.DirectoryOf&lt;T&gt;</c> catalog wrapper,
  /// and value-tuple positional packing.
  /// </summary>
  private static void ReportUnmarshallableSchemaProperties(
    SourceProductionContext ctx,
    ITypeSymbol typeArg,
    string slotLabel,
    Location location
  )
  {
    var visited = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
    foreach (var schema in EnumerateFlowthruSchemaLeaves(typeArg, visited))
    {
      foreach (var member in schema.GetMembers())
      {
        if (member is not IPropertySymbol property) continue;
        if (property.IsIndexer) continue;
        if (property.DeclaredAccessibility != Accessibility.Public) continue;
        if (property.IsStatic) continue;

        if (IsMarshallable(property.Type)) continue;

        ctx.ReportDiagnostic(
          Diagnostic.Create(
            AddPythonStepUnmarshallable,
            location,
            slotLabel,
            property.Name,
            property.Type.ToDisplayString()
          )
        );
      }
    }
  }

  /// <summary>
  /// Enumerate every <c>[FlowthruSchema]</c> leaf reachable from
  /// <paramref name="type"/> by unwrapping common Flowthru wrapper
  /// shapes. Visited symbols are tracked to terminate on accidental
  /// type cycles (e.g. a schema that references itself via a list).
  /// </summary>
  private static IEnumerable<INamedTypeSymbol> EnumerateFlowthruSchemaLeaves(
    ITypeSymbol type,
    HashSet<ISymbol> visited
  )
  {
    if (type is null) yield break;
    if (!visited.Add(type)) yield break;

    if (type is INamedTypeSymbol named)
    {
      if (named.ConstructedFrom?.SpecialType == SpecialType.System_Nullable_T)
      {
        foreach (var s in EnumerateFlowthruSchemaLeaves(named.TypeArguments[0], visited))
          yield return s;
        yield break;
      }

      if (named.IsTupleType)
      {
        foreach (var element in named.TupleElements)
        foreach (var s in EnumerateFlowthruSchemaLeaves(element.Type, visited))
          yield return s;
        yield break;
      }

      if (named.IsGenericType
          && named.ConstructedFrom is { } ctor
          && ctor.ToDisplayString() == "Flowthru.Data.Storage.DirectoryOf<T>")
      {
        foreach (var s in EnumerateFlowthruSchemaLeaves(named.TypeArguments[0], visited))
          yield return s;
        yield break;
      }

      if (HasFlowthruSchemaAttribute(named))
      {
        yield return named;
        yield break;
      }

      var enumerable = FindIEnumerableElement(named);
      if (enumerable is not null)
      {
        foreach (var s in EnumerateFlowthruSchemaLeaves(enumerable, visited))
          yield return s;
        yield break;
      }
    }

    if (type is IArrayTypeSymbol arr)
    {
      foreach (var s in EnumerateFlowthruSchemaLeaves(arr.ElementType, visited))
        yield return s;
    }
  }

  private static bool HasFlowthruSchemaAttribute(INamedTypeSymbol type)
  {
    foreach (var attr in type.GetAttributes())
    {
      var attrClass = attr.AttributeClass;
      if (attrClass is null) continue;
      if (attrClass.ToDisplayString() == "Flowthru.Data.Schema.FlowthruSchemaAttribute")
        return true;
    }
    return false;
  }

  /// <summary>Information about a Python step parsed from source.</summary>
  internal sealed class PythonStepInfo
  {
    public string FunctionName { get; }
    public string ModulePath { get; }
    public List<string> Inputs { get; }
    public List<string> Outputs { get; }
    /// <summary>
    /// True when the <c>@step(...)</c> decorator declares
    /// <c>cacheable=True</c>. Drives whether the generator emits a
    /// <c>PythonStepCacheRegistry.Register</c> entry for this step.
    /// </summary>
    public bool Cacheable { get; }
    /// <summary>
    /// Absolute path to the <c>.py</c> file containing this step.
    /// Captured at generation time and baked into the emitted
    /// registration so the runtime <c>AddPythonStep</c> path can read
    /// fresh content for the cache fingerprint.
    /// </summary>
    public string PyFilePath { get; }

    /// <summary>
    /// Roslyn <see cref="Location"/> pointing at the <c>@step(...)</c>
    /// decorator span in the <c>.py</c> file. Used as the location on
    /// per-decorator diagnostics (FT2007) so the IDE highlights the
    /// offending decorator rather than reporting at the project level.
    /// <see cref="Location.None"/> when the parse path didn't have a
    /// usable offset (e.g., synthetic test input without computed
    /// LinePositionSpan).
    /// </summary>
    public Location DecoratorLocation { get; }

    public PythonStepInfo(
      string functionName,
      string modulePath,
      List<string> inputs,
      List<string> outputs,
      bool cacheable,
      string pyFilePath,
      Location? decoratorLocation = null
    )
    {
      FunctionName = functionName;
      ModulePath = modulePath;
      Inputs = inputs;
      Outputs = outputs;
      Cacheable = cacheable;
      PyFilePath = pyFilePath;
      DecoratorLocation = decoratorLocation ?? Location.None;
    }
  }
}
