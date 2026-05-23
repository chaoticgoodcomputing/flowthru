using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace Flowthru.Step.Python.Internal;

/// <summary>
/// Classifies a single line of stderr output from the Python worker
/// into a <see cref="LogLevel"/> and a renderable message. Two code
/// paths feed this classifier:
/// <list type="bullet">
///   <item>
///   Structured records — the worker installs
///   <c>_FlowthruJsonLogHandler</c> on Python's root logger at startup
///   (<c>flowthru_worker.py</c>) so anything routed through stdlib
///   <c>logging</c> (<c>log.info(...)</c>, third-party libraries) is
///   emitted as <c>__flowthru_log__:&lt;json&gt;</c>. The classifier
///   parses the frame and uses the embedded level, dropping the
///   prefix from the rendered message.
///   </item>
///   <item>
///   Raw lines — <c>print()</c>, direct <c>sys.stderr.write(...)</c>,
///   exception tracebacks. These arrive unprefixed and default to
///   <see cref="LogLevel.Information"/> unless a heuristic elevates
///   them (a leading <c>Traceback (most recent call last):</c>
///   becomes <see cref="LogLevel.Error"/>).
///   </item>
/// </list>
/// </summary>
/// <remarks>
/// Per ADR-0005 the engine and every step share one <c>"Flowthru"</c>-
/// category <see cref="ILogger"/>; Python steps participate via the
/// stderr bridge in
/// <see cref="SubprocessPythonExecutor"/>. This classifier is the
/// per-line decision point of that bridge.
/// </remarks>
internal static class StderrLineClassifier
{
  /// <summary>
  /// Prefix the Python worker writes on every structured log line.
  /// Must match <c>_LOG_FRAME_PREFIX</c> in <c>flowthru_worker.py</c>.
  /// </summary>
  internal const string LogFramePrefix = "__flowthru_log__:";

  /// <summary>
  /// Classify a single stderr line. Returns the <see cref="LogLevel"/>
  /// at which the engine's <see cref="ILogger"/> should emit it and
  /// the message to forward (with the structured-frame prefix
  /// stripped when present).
  /// </summary>
  public static (LogLevel Level, string Message) Classify(string line)
  {
    if (line.StartsWith(LogFramePrefix, StringComparison.Ordinal))
    {
      var payload = line.AsSpan(LogFramePrefix.Length);
      try
      {
        // ReadOnlySpan<char> overload isn't on JsonNode.Parse — convert
        // to string once per frame. This is the hot path only when
        // Python steps emit structured logs; raw print() lines skip it.
        var node = JsonNode.Parse(payload.ToString());
        if (node is JsonObject obj)
        {
          var levelName = obj["level"]?.GetValue<string>();
          var loggerName = obj["logger"]?.GetValue<string>();
          var msg = obj["msg"]?.GetValue<string>() ?? string.Empty;
          var exc = obj["exc"]?.GetValue<string>();

          var rendered = string.IsNullOrEmpty(loggerName)
            ? msg
            : $"[{loggerName}] {msg}";
          if (!string.IsNullOrEmpty(exc))
          {
            rendered = rendered + "\n" + exc;
          }
          return (MapLevel(levelName), rendered);
        }
      }
      catch (JsonException)
      {
        // Malformed frame — fall through to the raw-line path so the
        // line still reaches the host instead of being silently
        // dropped. Worth seeing in the log as evidence of the bug.
      }
    }

    // Raw line path: default to Information; heuristically elevate
    // Python-traceback-shaped lines to Error so a crashing step
    // surfaces at the right severity without level coordination
    // between the worker and the host.
    if (line.StartsWith("Traceback (most recent call last):", StringComparison.Ordinal))
    {
      return (LogLevel.Error, line);
    }

    return (LogLevel.Information, line);
  }

  /// <summary>
  /// Map a Python <c>logging</c> level name to the corresponding
  /// .NET <see cref="LogLevel"/>. Unknown / missing names fall back
  /// to <see cref="LogLevel.Information"/> rather than failing — the
  /// stderr bridge is best-effort observation, not a contract that
  /// can refuse work.
  /// </summary>
  internal static LogLevel MapLevel(string? pythonLevelName) => pythonLevelName switch
  {
    "DEBUG" => LogLevel.Debug,
    "INFO" => LogLevel.Information,
    "WARNING" => LogLevel.Warning,
    "ERROR" => LogLevel.Error,
    "CRITICAL" => LogLevel.Critical,
    _ => LogLevel.Information,
  };
}
