using Flowthru.Data.Validation;

namespace Flowthru.Flows.Validation;

/// <summary>
/// Validation hook that runs during flow pre-flight checks.
/// </summary>
/// <remarks>
/// <para>
/// Validation hooks provide an extensibility point for extensions to contribute
/// their own validation logic during the pre-flight phase. Hooks are invoked after
/// DAG analysis but before external input inspection, allowing extensions to
/// validate their own step types.
/// </para>
/// <para>
/// <strong>Example use cases:</strong>
/// <list type="bullet">
/// <item>Python extension validates @step decorators match C# types</item>
/// <item>Custom extensions validate step-specific configuration</item>
/// <item>Third-party plugins validate external dependencies</item>
/// </list>
/// </para>
/// <para>
/// <strong>Hook execution order:</strong>
/// </para>
/// <list type="number">
/// <item>Flow.Build() - DAG construction and layer assignment</item>
/// <item>ValidationHooks.ValidateAsync() - Extension-specific validation</item>
/// <item>Flow.ValidateExternalInputsAsync() - External input inspection</item>
/// </list>
/// <para>
/// <strong>Error handling:</strong>
/// Hooks should return ValidationResult with errors, never throw exceptions.
/// Multiple hooks may run, and all errors are aggregated into a single result.
/// </para>
/// </remarks>
public interface IFlowValidationHook
{
  /// <summary>
  /// Validates flow steps during pre-flight checks.
  /// </summary>
  /// <param name="flow">The flow being validated</param>
  /// <param name="cancellationToken">Cancellation token for async operations</param>
  /// <returns>Validation result containing any errors found</returns>
  /// <remarks>
  /// <para>
  /// Implementations should:
  /// <list type="bullet">
  /// <item>Never throw exceptions (return errors in ValidationResult)</item>
  /// <item>Be idempotent (safe to call multiple times)</item>
  /// <item>Be reasonably fast (executed during pre-flight, blocks flow start)</item>
  /// <item>Only validate steps they understand (ignore other step types)</item>
  /// </list>
  /// </para>
  /// <para>
  /// <strong>Example implementation (Python extension):</strong>
  /// </para>
  /// <code>
  /// public async Task&lt;ValidationResult&gt; ValidateAsync(
  ///   Flow flow,
  ///   CancellationToken cancellationToken)
  /// {
  ///   var result = ValidationResult.Success();
  ///
  ///   foreach (var step in flow.Steps)
  ///   {
  ///     if (IsPythonStep(step))
  ///     {
  ///       var stepResult = await ValidatePythonStep(step, cancellationToken);
  ///       result.Merge(stepResult);
  ///     }
  ///   }
  ///
  ///   return result;
  /// }
  /// </code>
  /// </remarks>
  Task<ValidationResult> ValidateAsync(Flow flow, CancellationToken cancellationToken);
}
