namespace Flowthru.Tests.Examples.Infrastructure;

/// <summary>
/// Represents a discovered Flowthru example project that can be executed in tests.
/// </summary>
public sealed class ExampleProject
{
  /// <summary>
  /// Display name for the example (e.g., "KedroSpaceflights").
  /// </summary>
  public required string Name { get; init; }

  /// <summary>
  /// Absolute path to the example's source project directory.
  /// Passed as <c>basePath</c> to <c>ConfigureServices</c> so that catalog entries
  /// and configuration files resolve independently of the working directory.
  /// </summary>
  public required string ProjectPath { get; init; }

  /// <summary>
  /// The <see cref="Type"/> that exposes the
  /// <c>public static IServiceProvider ConfigureServices(string? basePath = null)</c> method.
  /// </summary>
  public required Type EntryPointType { get; init; }

  /// <inheritdoc />
  public override string ToString() => Name;
}
