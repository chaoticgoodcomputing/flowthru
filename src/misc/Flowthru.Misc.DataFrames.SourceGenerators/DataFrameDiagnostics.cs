using Microsoft.CodeAnalysis;

namespace Flowthru.Misc.DataFrames.Analyzers;

/// <summary>
/// Diagnostic descriptors for the DataFrame expression analyzer.
/// </summary>
public static class DataFrameDiagnostics
{
    private const string Category = "Flowthru.Misc.DataFrames";

    /// <summary>
    /// FDFRAMES1001: The lambda body passed to <c>TypedFrame.Select()</c> must be an
    /// object-creation expression with an initializer, a record/anonymous-type positional
    /// constructor call, or a single member access. Arbitrary expression bodies cannot be
    /// decomposed into named column operations by any DataFrame provider.
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidProjectionBody =
      new(
        id: "FDFRAMES1001",
        title: "TypedFrame Select projection must be an object initializer or record constructor",
        messageFormat: "The Select lambda body '{0}' cannot be translated to named column "
          + "operations. Use an object initializer (new OutputSchema {{ Prop = x.Prop }}), "
          + "a record constructor, or an anonymous type.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "TypedFrame.Select() requires the lambda body to be decomposable into "
          + "named column operations. Object initializers, record constructors, and anonymous "
          + "type constructors are supported. Arbitrary method calls, tuple constructors, or "
          + "other expression forms are not translatable by any DataFrame provider."
      );

    /// <summary>
    /// FDFRAMES1002: An object-initializer binding inside <c>TypedFrame.Select()</c> uses a
    /// collection or nested-object form that cannot be decomposed into a single named column
    /// operation. Only plain property-assignment bindings (<c>Prop = expr</c>) are translatable.
    /// </summary>
    public static readonly DiagnosticDescriptor NonAssignmentBinding =
      new(
        id: "FDFRAMES1002",
        title: "TypedFrame Select initializer must use property-assignment bindings",
        messageFormat: "The binding '{0}' in the Select initializer uses a collection or "
          + "nested-object form. Only property-assignment bindings (Prop = expr) can be "
          + "translated to named column operations.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "TypedFrame.Select() initializer bindings must be directly assignable "
          + "member expressions. Collection initializers (Items = {{ ... }}) and nested-object "
          + "initializers (Nested = {{ Prop = val }}) produce non-assignment expression tree "
          + "bindings that no DataFrame provider can translate."
      );

    /// <summary>
    /// FDFRAMES1003: A positional constructor call inside <c>TypedFrame.Select()</c> targets a
    /// type that is not a record or anonymous type. Column names cannot be derived from
    /// constructor parameter position for plain classes.
    /// </summary>
    public static readonly DiagnosticDescriptor PositionalConstructorNonRecord =
      new(
        id: "FDFRAMES1003",
        title: "TypedFrame Select positional constructor requires a record or anonymous type",
        messageFormat: "'{0}' is not a record or anonymous type. Positional constructors cannot "
          + "be decomposed into named column operations unless the type exposes member metadata "
          + "(records and anonymous types do; plain classes do not).",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "TypedFrame.Select() with a positional constructor requires the target type "
          + "to be a record or anonymous type so that column names can be inferred from the "
          + "constructor parameters. Convert the type to a record or use an object initializer."
      );

    /// <summary>
    /// FDFRAMES1004: The result selector body passed to <c>GroupedFrame.Aggregate()</c> is not
    /// an object-creation expression. Aggregate projections must name each output column
    /// explicitly via an object initializer, record constructor, or anonymous type.
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidAggregateResultBody =
      new(
        id: "FDFRAMES1004",
        title: "TypedFrame Aggregate result selector must be an object initializer",
        messageFormat: "The Aggregate result selector body '{0}' cannot be translated. Use an "
          + "object initializer (new TResult {{ Prop = ctx.Key }}), a positional record "
          + "constructor, or an anonymous type.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "GroupedFrame.Aggregate() requires the result selector to produce an object "
          + "whose properties map explicitly to aggregation outputs. Member-access passthrough "
          + "and arbitrary expressions are not supported — each output column must name a key "
          + "or aggregation function from the AggregationContext."
      );

    /// <summary>
    /// FDFRAMES1005: A binding inside a <c>GroupedFrame.Aggregate()</c> result selector is
    /// neither a key access (<c>ctx.Key</c>) nor an aggregation method call
    /// (<c>ctx.Avg(...)</c>, <c>ctx.Sum(...)</c>, etc.).
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidAggregateBinding =
      new(
        id: "FDFRAMES1005",
        title: "TypedFrame Aggregate binding must be ctx.Key or an aggregation method call",
        messageFormat: "The expression '{0}' cannot be translated as an aggregate output. Each "
          + "property in an Aggregate result selector must be ctx.Key or a call to an "
          + "aggregation method (ctx.Avg(...), ctx.Sum(...), ctx.Count(), etc.).",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "GroupedFrame.Aggregate() result selector bindings must either read the "
          + "group key (ctx.Key) or invoke an aggregation function on the context. Arbitrary "
          + "expressions cannot be translated by any DataFrame provider."
      );
}
