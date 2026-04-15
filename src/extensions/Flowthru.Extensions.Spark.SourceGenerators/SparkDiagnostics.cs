using Microsoft.CodeAnalysis;

namespace Flowthru.Extensions.Spark.Analyzers;

/// <summary>
/// Diagnostic descriptors for the Spark expression analyzer.
/// </summary>
public static class SparkDiagnostics
{
  private const string Category = "Flowthru.Spark";

  /// <summary>
  /// FSPARK1002: A method call inside a <c>TypedFrame</c> lambda has no translation in the
  /// Spark provider. The supported sets are defined in
  /// <see cref="Flowthru.Extensions.Spark.Shared.SparkTranslatableOperations"/>.
  /// </summary>
  public static readonly DiagnosticDescriptor UnsupportedMethodCall =
    new(
      id: "FSPARK1002",
      title: "Method call in TypedFrame lambda has no Spark translation",
      messageFormat: "'{0}.{1}' cannot be translated to a Spark Column expression. "
        + "Supported string methods: {2}. Supported Math methods: {3}.",
      category: Category,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true,
      description: "The Spark provider can only translate a specific subset of method calls "
        + "inside TypedFrame lambdas. Calls outside this set will fail at runtime. "
        + "To add support for a new method, add it to SparkTranslatableOperations and "
        + "implement the corresponding switch arm in SparkExpressionVisitor."
    );
}
