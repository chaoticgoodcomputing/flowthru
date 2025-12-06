namespace MagicAST.Tests.Infrastructure;

using System.Runtime.CompilerServices;

/// <summary>
/// Module initializer to configure Verify settings for all tests.
/// </summary>
public static class VerifyConfiguration
{
  [ModuleInitializer]
  public static void Initialize()
  {
    // Sort properties alphabetically for consistent snapshots
    VerifierSettings.SortPropertiesAlphabetically();

    // Scrub runtime-specific info that shouldn't affect comparisons
    VerifierSettings.ScrubInlineGuids();

    // Place all snapshots in a centralized Snapshots/ directory
    Verifier.DerivePathInfo(
      (sourceFile, projectDirectory, type, method) =>
        new(
          directory: Path.Combine(projectDirectory, "Snapshots"),
          typeName: type.Name,
          methodName: method.Name
        )
    );
  }
}
