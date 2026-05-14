namespace Flowthru.Step;

/// <summary>
/// Production-readiness state of a step extension. Drives whether
/// <c>FT1301</c> (minimum container support) fires as an error or as
/// a warning.
/// </summary>
/// <remarks>
/// Production is the default — opt-in to in-development mode is
/// explicit and PR-visible. The downgrade is strictly compile-time;
/// there is no runtime surfacing. The mechanism is meant as a guard
/// against incomplete algebras in shipped extensions, not a runtime
/// feature flag.
/// </remarks>
public enum ExtensionStatus
{
  /// <summary>
  /// Extension is intended for production consumption. <c>FT1301</c>
  /// fires as an error when the declared container support fails to
  /// meet the minimum floor.
  /// </summary>
  Production = 0,

  /// <summary>
  /// Extension is under development. <c>FT1301</c> fires as a warning
  /// instead of an error, allowing the author to iterate on the
  /// extension's algebra before declaring it production-ready.
  /// </summary>
  InDevelopment = 1,
}
