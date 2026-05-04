namespace Flowthru.Core.Effects;

/// <summary>
/// Thrown by the framework when one or more catalog-level
/// <see cref="FlowValidation"/> checks report failures during pre-flight.
/// </summary>
/// <remarks>
/// <para>
/// All failures across all participating catalogs are collected applicatively
/// and surfaced together. The <see cref="Validation"/> property carries the
/// full failure list; <see cref="Exception.Message"/> renders a summary.
/// </para>
/// <para>
/// Distinct from <c>Flowthru.Core.Data.Validation.ValidationResult</c>, which
/// is the existing per-item inspection result returned by
/// <c>Flow.ValidateExternalInputsAsync</c>. The two run sequentially in
/// pre-flight: catalog-level <see cref="FlowValidation"/> first (for fast
/// applicative reports), then per-item inspection (for schema/connection
/// drift on external inputs).
/// </para>
/// </remarks>
public sealed class FlowValidationException : Exception
{
  /// <summary>
  /// The aggregated failure list reported by participating catalogs.
  /// </summary>
  public FlowValidation Validation { get; }

  /// <param name="validation">A non-empty <see cref="FlowValidation"/>.</param>
  public FlowValidationException(FlowValidation validation)
    : base(BuildMessage(validation))
  {
    Validation = validation;
  }

  private static string BuildMessage(FlowValidation validation)
  {
    var failures = validation.Failures;
    if (failures.Count == 0)
    {
      return "Catalog pre-flight validation reported no failures.";
    }

    var lines = new List<string>(capacity: failures.Count + 1)
    {
      $"Catalog pre-flight validation failed with {failures.Count} error(s):",
    };
    foreach (var f in failures)
    {
      lines.Add($"  • [{f.Source}] {f.Message}");
    }
    return string.Join(Environment.NewLine, lines);
  }
}
