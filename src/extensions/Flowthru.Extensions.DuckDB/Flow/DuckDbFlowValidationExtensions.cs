using Flowthru.Data.Storage;
using Flowthru.Step.DuckDb.Internal;

namespace Flowthru.Flow;

/// <summary>
/// Design-time validation surface for DuckDB transforms: run the same
/// hermetic SQL schema check pre-flight runs, from a unit test, so a
/// schema-breaking SQL edit fails the build's test run — design-time by
/// the glossary's definition. The embedded engine is fast enough that
/// this belongs in ordinary unit tests.
/// </summary>
/// <example>
/// In any test framework (or an <c>FUnitContext</c> subclass):
/// <code>
/// [Test]
/// public async Task TransformSqlAgreesWithDeclaredSchemas()
/// {
///   var flow = AnalyticsFlow.Create(new Catalog(), new InProcessDuckDbEngine());
///   var result = await flow.ValidateDuckDbTransforms();
///   Assert.That(result.IsValid, Is.True,
///     string.Join("\n", result.Errors.Select(e => e.Message)));
/// }
/// </code>
/// A single step can be checked through the standard FUnit sugar
/// instead — <c>FUnitContext.Validate(step)</c> runs the same check via
/// <see cref="Step.DuckDb.DuckDbTransformStep{TOut}.Validate"/>.
/// </example>
public static class DuckDbFlowValidationExtensions
{
  /// <summary>
  /// Run the hermetic SQL schema check for every DuckDB transform in
  /// <paramref name="flow"/> and aggregate the findings — every broken
  /// transform reports at once, with the same diagnostics (and FTDDB3xxx
  /// codes, carried in <see cref="ValidationError.Details"/>) that
  /// pre-flight would surface. A flow with no DuckDB transforms is
  /// vacuously valid. Reaches nothing outside the process and reads no
  /// real data.
  /// </summary>
  /// <param name="flow">The built flow whose DuckDB transforms are checked.</param>
  /// <param name="cancellationToken">Cancels the in-engine checks.</param>
  public static async Task<ValidationResult> ValidateDuckDbTransforms(
    this BuiltFlow flow,
    CancellationToken cancellationToken = default
  )
  {
    if (flow is null) throw new ArgumentNullException(nameof(flow));

    var errors = new List<ValidationError>();
    foreach (var step in flow.Steps)
    {
      if (step is not IDuckDbTransformDescriptor transform) continue;

      var failures = await DuckDbSqlSchemaCheck.RunAsync(transform, cancellationToken)
        .ConfigureAwait(false);
      errors.AddRange(failures.Select(f =>
        DuckDbSqlSchemaCheck.ToValidationError(step.Label, f)
      ));
    }

    return errors.Count == 0 ? ValidationResult.Success() : new ValidationResult(errors);
  }
}
