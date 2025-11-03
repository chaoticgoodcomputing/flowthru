namespace Flowthru.Tests.Examples.Infrastructure;

/// <summary>
/// Represents a discovered Flowthru example project.
/// </summary>
public sealed class ExampleProject
{
  /// <summary>
  /// Gets the name of the example project (e.g., "KedroSpaceflights.Custom").
  /// </summary>
  public required string Name { get; init; }

  /// <summary>
  /// Gets the absolute path to the example project directory.
  /// </summary>
  public required string ProjectPath { get; init; }

  /// <summary>
  /// Gets the absolute path to the example's .csproj file.
  /// </summary>
  public required string CsprojPath { get; init; }

  /// <summary>
  /// Gets the Type containing the Main entry point for the example.
  /// </summary>
  public required Type EntryPointType { get; init; }

  /// <summary>
  /// Returns the example name for test display purposes.
  /// </summary>
  public override string ToString() => Name;
}
