namespace Flowthru.Tests.Examples.Infrastructure;

/// <summary>
/// Represents a discovered Flowthru example project that can be executed in tests.
/// </summary>
public sealed class ExampleProject
{
  /// <summary>
  /// Display name for the example (e.g., "Spaceflights").
  /// </summary>
  public required string Name { get; init; }

  /// <summary>
  /// Absolute path to the example's source project directory.
  /// Passed as <c>basePath</c> to <c>ConfigureServices</c> so that catalog entries,
  /// Python source files, and configuration files resolve correctly.
  /// </summary>
  public required string ProjectPath { get; init; }

  /// <summary>
  /// Absolute path to the example's build output directory.
  /// Contains <c>pyproject.toml</c>, <c>uv.lock</c>, and materialized <c>.venv/</c>.
  /// </summary>
  public required string OutputPath { get; init; }

  /// <summary>
  /// The <see cref="Type"/> that exposes the
  /// <c>public static IServiceProvider ConfigureServices(string? basePath = null)</c> method.
  /// </summary>
  public required Type EntryPointType { get; init; }

  /// <inheritdoc />
  public override string ToString() => Name;
}
