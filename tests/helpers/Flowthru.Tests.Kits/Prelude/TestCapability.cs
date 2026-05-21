namespace Flowthru.Tests.Kits.Prelude;

/// <summary>
/// An external dependency a test backend or scenario depends on
/// (Docker, SPARK_HOME, JDK 17+, headless Chromium, …). Backends declare
/// their requirements via <see cref="IResourceBackend{TScope}.RequiredCapabilities"/>;
/// the laws kit runs <see cref="IsAvailable"/> via <c>Assume.That</c> in
/// <c>OneTimeSetUp</c>, so a missing dependency yields an <em>Inconclusive</em>
/// fixture rather than a failure.
/// </summary>
/// <param name="Name">
/// Stable identifier for diagnostics (e.g. <c>"docker"</c>, <c>"SPARK_HOME"</c>).
/// </param>
/// <param name="IsAvailable">
/// Probe that returns <c>true</c> when the dependency is present in the
/// current environment. Should be cheap and side-effect-free; results are
/// cached at the <see cref="TestCapabilities"/> singleton level.
/// </param>
/// <param name="MissingMessage">
/// Human-readable diagnostic surfaced as the <c>Assume.That</c> reason
/// when the dependency is missing. Should include install instructions.
/// </param>
public sealed record TestCapability(
  string Name,
  Func<bool> IsAvailable,
  string MissingMessage
);
