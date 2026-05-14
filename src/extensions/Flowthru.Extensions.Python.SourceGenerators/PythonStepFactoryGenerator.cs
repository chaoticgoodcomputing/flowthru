using System;
using System.Collections.Generic;
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
  private static readonly DiagnosticDescriptor SchemaNotFound = new(
    id: "FT2007",
    title: "Python decorator references unknown schema",
    messageFormat: "Schema '{0}' referenced in @step decorator is not a [FlowthruSchema]-decorated type in the consuming compilation",
    category: "Flowthru.Python",
    DiagnosticSeverity.Error,
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
    // Discover all .py AdditionalFiles in the consuming project.
    var pythonSteps = context.AdditionalTextsProvider
      .Where(file => file.Path.EndsWith(".py"))
      .Select((file, ct) =>
      {
        var content = file.GetText(ct)?.ToString();
        if (string.IsNullOrEmpty(content)) return null;
        return ParsePythonStep(file.Path, content!);
      })
      .Where(step => step is not null)
      .Select((step, _) => step!)
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

  private static PythonStepInfo? ParsePythonStep(string filePath, string content)
  {
    // Match @step(inputs=[...], outputs=[...]) with optional services=[...].
    // The bracket contents are handed to ParseSchemaList which accepts
    // both string literals AND bare identifiers.
    var decoratorPattern =
      @"@step\s*\(\s*inputs\s*=\s*(\[[^\]]*\])\s*,\s*outputs\s*=\s*(\[[^\]]*\]|None)"
      + @"(?:\s*,\s*services\s*=\s*\[[^\]]*\])?\s*\)";
    var functionPattern = @"def\s+(\w+)\s*\(";

    var decoratorMatch = Regex.Match(content, decoratorPattern);
    if (!decoratorMatch.Success) return null;

    var functionMatch = Regex.Match(content.Substring(decoratorMatch.Index), functionPattern);
    if (!functionMatch.Success) return null;

    var functionName = functionMatch.Groups[1].Value;
    var inputsRaw = decoratorMatch.Groups[1].Value;
    var outputsRaw = decoratorMatch.Groups[2].Value;

    var inputs = ParseSchemaList(inputsRaw);
    var outputs = outputsRaw == "None" ? new List<string>() : ParseSchemaList(outputsRaw);
    var modulePath = DeriveModulePath(filePath);

    return new PythonStepInfo(functionName, modulePath, inputs, outputs);
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
      var typeRef = ResolveSchemaReference(ctx, schemaName, schemaIndex, ref anyMissing);
      inputTypes.Add(typeRef);
      ReportUnmarshallableProperties(ctx, schemaName, step.FunctionName, schemaIndex);
    }
    var outputTypes = new List<string>();
    foreach (var schemaName in step.Outputs)
    {
      var typeRef = ResolveSchemaReference(ctx, schemaName, schemaIndex, ref anyMissing);
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
      ctx.ReportDiagnostic(Diagnostic.Create(SchemaNotFound, location: Location.None, schemaName));
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

    public PythonStepInfo(string functionName, string modulePath, List<string> inputs, List<string> outputs)
    {
      FunctionName = functionName;
      ModulePath = modulePath;
      Inputs = inputs;
      Outputs = outputs;
    }
  }
}
