namespace Flowthru.Tests.Templates.Infrastructure;

/// <summary>
/// Represents a Flowthru template project configuration for testing.
/// </summary>
public sealed record TemplateProject
{
    /// <summary>
    /// Gets the starter template name (e.g., "iris", "spaceflights").
    /// </summary>
    public required string StarterName { get; init; }

    /// <summary>
    /// Gets the generated project name.
    /// </summary>
    public required string ProjectName { get; init; }

    /// <summary>
    /// Gets the absolute path where the project will be generated.
    /// </summary>
    public required string GeneratedPath { get; init; }

    /// <summary>
    /// Gets the pipeline name to execute for testing.
    /// If null, only validates template generation and compilation.
    /// </summary>
    public string? FlowName { get; init; }

    /// <summary>
    /// Returns a string representation for test display.
    /// </summary>
    public override string ToString() => FlowName != null ? $"{StarterName}/{FlowName}" : StarterName;
}
