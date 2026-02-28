using Flowthru.Tests.Templates.Infrastructure;

namespace Flowthru.Tests.Templates;

/// <summary>
/// Integration tests that verify Flowthru templates can be generated and executed successfully.
/// These tests validate the complete user onboarding flow: dotnet new → restore → build → run.
/// </summary>
[TestFixture]
[Category("Templates")]
[Category("Integration")]
public class TemplateIntegrationTests
{
  private static string _templateInstallPath = null!;

  /// <summary>
  /// Discovers all available starter templates for testing.
  /// </summary>
  private static IEnumerable<TemplateProject> GetAllTemplates()
  {
    return TemplateDiscovery.DiscoverTemplates();
  }

  /// <summary>
  /// Installs the Flowthru template before running any tests.
  /// </summary>
  [OneTimeSetUp]
  public static void InstallTemplate()
  {
    // Find the repository root (navigate up from the test assembly location)
    var assemblyPath = typeof(TemplateIntegrationTests).Assembly.Location;
    var testsDir = Path.GetDirectoryName(assemblyPath);
    var repoRoot = Path.GetFullPath(Path.Combine(testsDir!, "..", "..", "..", ".."));

    _templateInstallPath = Path.Combine(repoRoot, "examples", "starter");

    if (!Directory.Exists(_templateInstallPath))
    {
      throw new DirectoryNotFoundException(
        $"Template directory not found: {_templateInstallPath}. "
          + "Ensure examples/starter/ exists."
      );
    }

    TestContext.WriteLine($"Installing template from: {_templateInstallPath}");
    TemplateInstaller.Install(_templateInstallPath);
    TestContext.WriteLine("Template installed successfully");
  }

  /// <summary>
  /// Uninstalls the Flowthru template after all tests complete.
  /// </summary>
  [OneTimeTearDown]
  public static void UninstallTemplate()
  {
    if (_templateInstallPath != null)
    {
      TestContext.WriteLine("Uninstalling template...");
      TemplateInstaller.Uninstall(_templateInstallPath);
      TestContext.WriteLine("Template uninstalled");
    }

    // Clean up test output directory
    var testOutputPath = Path.Combine(Path.GetTempPath(), "flowthru-template-tests");
    if (Directory.Exists(testOutputPath))
    {
      try
      {
        Directory.Delete(testOutputPath, recursive: true);
        TestContext.WriteLine($"Cleaned up test output directory: {testOutputPath}");
      }
      catch
      {
        // Best effort cleanup
      }
    }
  }

  /// <summary>
  /// Tests that a starter template generates, builds, and validates successfully.
  /// </summary>
  [TestCaseSource(nameof(GetAllTemplates))]
  public async Task StarterTemplate_GeneratesAndValidatesSuccessfully(TemplateProject template)
  {
    // Arrange
    var runner = new TemplateTestRunner(template);

    // Act
    TestContext.WriteLine($"Testing: {template}");
    var result = await runner.GenerateAndRunAsync();

    // Assert
    LogResult(result);
    Assert.That(result.Success, Is.True, $"Template test failed: {result.DiagnosticMessage}");
    Assert.That(result.ExitCode, Is.EqualTo(0), "Process should exit with code 0");
  }

  /// <summary>
  /// Logs test result details to the test context.
  /// </summary>
  private static void LogResult(TemplateTestResult result)
  {
    TestContext.WriteLine($"Duration: {result.Duration.TotalSeconds:F2}s");
    TestContext.WriteLine($"Exit Code: {result.ExitCode}");

    if (!string.IsNullOrEmpty(result.DiagnosticMessage))
    {
      TestContext.WriteLine($"Diagnostic: {result.DiagnosticMessage}");
    }

    if (result.Exception != null)
    {
      TestContext.WriteLine("--- Exception Details ---");
      TestContext.WriteLine($"Type: {result.Exception.GetType().FullName}");
      TestContext.WriteLine($"Message: {result.Exception.Message}");
      TestContext.WriteLine($"StackTrace:\n{result.Exception.StackTrace}");
      TestContext.WriteLine("--- End Exception Details ---");
    }

    if (!string.IsNullOrEmpty(result.StandardOutput))
    {
      TestContext.WriteLine("--- Standard Output ---");
      TestContext.WriteLine(result.StandardOutput);
      TestContext.WriteLine("--- End Standard Output ---");
    }

    if (!string.IsNullOrEmpty(result.StandardError))
    {
      TestContext.WriteLine("--- Standard Error ---");
      TestContext.WriteLine(result.StandardError);
      TestContext.WriteLine("--- End Standard Error ---");
    }
  }
}
