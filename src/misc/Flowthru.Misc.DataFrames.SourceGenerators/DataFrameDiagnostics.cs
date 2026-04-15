using Microsoft.CodeAnalysis;

namespace Flowthru.Misc.DataFrames.Analyzers;

/// <summary>
/// Diagnostic descriptors for the DataFrame expression analyzer.
/// </summary>
public static class DataFrameDiagnostics
{
  private const string Category = "Flowthru.Misc.DataFrames";

  /// <summary>
  /// FDFRAME1001: The lambda body passed to <c>TypedFrame.Select()</c> must be an
  /// object-creation expression with an initializer, a record/anonymous-type positional
  /// constructor call, or a single member access. Arbitrary expression bodies cannot be
  /// decomposed into named column operations by any DataFrame provider.
  /// </summary>
  public static readonly DiagnosticDescriptor InvalidProjectionBody =
    new(
      id: "FDFRAME1001",
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
}
