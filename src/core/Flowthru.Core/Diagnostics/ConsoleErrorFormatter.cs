using System.Text;

namespace Flowthru.Diagnostics;

/// <summary>
/// Renders <see cref="RuntimeErrorReport"/> and
/// <see cref="PreFlightErrorReport"/> values into human-readable
/// strings suitable for console output. Pure string-building — no
/// IO, no colour codes — so consumers (CLI, hosted log writers,
/// fixture assertions) can compose the same output deterministically.
/// </summary>
/// <remarks>
/// <para>
/// Per §2.5, <see cref="RuntimeError.InvariantViolated"/> is special:
/// its presence means a pre-flight invariant was missed at runtime,
/// which is a Flowthru bug. The formatter renders these with a
/// "please file an issue" affordance so the user can distinguish a
/// framework defect from a flow defect.
/// </para>
/// </remarks>
public static class ConsoleErrorFormatter
{
  /// <summary>Render a <see cref="RuntimeErrorReport"/>.</summary>
  public static string Format(RuntimeErrorReport report)
  {
    if (report is null) throw new ArgumentNullException(nameof(report));
    var sb = new StringBuilder();
    sb.Append('[').Append(report.DiagnosticCode).Append("] ");
    sb.Append(report.Category).Append(": ");
    sb.Append(report.Message);
    if (report.Error is RuntimeError.InvariantViolated)
    {
      sb.AppendLine();
      sb.Append(
        "  This indicates a bug in Flowthru itself — a pre-flight check "
        + "that should have caught the condition was missing or wrong. "
        + "Please file an issue at https://github.com/anthropics/flowthru/issues."
      );
    }
    return sb.ToString();
  }

  /// <summary>Render a <see cref="PreFlightErrorReport"/>.</summary>
  public static string Format(PreFlightErrorReport report)
  {
    if (report is null) throw new ArgumentNullException(nameof(report));
    var sb = new StringBuilder();
    sb.Append('[').Append(report.DiagnosticCode).Append("] ");
    sb.Append(report.Category).Append(": ");
    sb.Append(report.Message);
    return sb.ToString();
  }

  /// <summary>
  /// Render a list of pre-flight errors as one block — header line
  /// plus one indented line per error.
  /// </summary>
  public static string FormatAll(IReadOnlyList<PreFlightError> errors)
  {
    if (errors is null) throw new ArgumentNullException(nameof(errors));
    if (errors.Count == 0) return string.Empty;

    var sb = new StringBuilder();
    sb.Append("Pre-flight failed (")
      .Append(errors.Count)
      .Append(errors.Count == 1 ? " error" : " errors")
      .AppendLine("):");
    foreach (var error in errors)
    {
      var report = PreFlightErrorClassifier.Classify(error);
      sb.Append("  ").AppendLine(Format(report));
    }
    return sb.ToString();
  }
}
