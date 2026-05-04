namespace Flowthru.Core.Effects;

/// <summary>
/// A single error reported by a <see cref="FlowValidation"/>.
/// </summary>
/// <param name="Source">
/// Identifies what produced the failure — typically a catalog label, item label, or
/// connection string. Used for diagnostic grouping in aggregated reports.
/// </param>
/// <param name="Message">Human-readable description of the failure.</param>
/// <param name="Exception">Optional underlying exception that triggered the failure.</param>
public sealed record FlowValidationFailure(
  string Source,
  string Message,
  Exception? Exception = null
);
