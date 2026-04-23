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
    // Walk up from the assembly location looking for nx.json (workspace root marker).
    // A fixed-depth traversal breaks when the output path depth changes (e.g. when
    // a Configuration segment is added to BaseOutputPath in Directory.Build.props).
    var assemblyPath = typeof(TemplateIntegrationTests).Assembly.Location;
    var dir = Path.GetDirectoryName(assemblyPath);
    while (dir != null && !File.Exists(Path.Combine(dir, "nx.json")))
    {
      dir = Path.GetDirectoryName(dir);
    }

    if (dir == null)
    {
      throw new DirectoryNotFoundException("Could not locate workspace root (nx.json not found).");
    }

    _templateInstallPath = Path.Combine(dir, "examples", "starter");

    if (!Directory.Exists(_templateInstallPath))
    {
      throw new DirectoryNotFoundException(
        $"Template directory not found: {_templateInstallPath}. "
          + "Ensure examples/starter/ exists."
      );
    }

    TestContext.Out.WriteLine($"Installing template from: {_templateInstallPath}");
    TemplateInstaller.Install(_templateInstallPath);
    TestContext.Out.WriteLine("Template installed successfully");
  }

  /// <summary>
  /// Uninstalls the Flowthru template after all tests complete.
  /// </summary>
  [OneTimeTearDown]
  public static void UninstallTemplate()
  {
    if (_templateInstallPath != null)
    {
      TestContext.Out.WriteLine("Uninstalling template...");
      TemplateInstaller.Uninstall(_templateInstallPath);
      TestContext.Out.WriteLine("Template uninstalled");
    }

    // Clean up test output directory
    var testOutputPath = Path.Combine(Path.GetTempPath(), "flowthru-template-tests");
    if (Directory.Exists(testOutputPath))
    {
      try
      {
        Directory.Delete(testOutputPath, recursive: true);
        TestContext.Out.WriteLine($"Cleaned up test output directory: {testOutputPath}");
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
    TestContext.Out.WriteLine($"Testing: {template}");
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
    TestContext.Out.WriteLine($"Duration: {result.Duration.TotalSeconds:F2}s");
    TestContext.Out.WriteLine($"Exit Code: {result.ExitCode}");

    if (!string.IsNullOrEmpty(result.DiagnosticMessage))
    {
      TestContext.Out.WriteLine($"Diagnostic: {result.DiagnosticMessage}");
    }

    if (result.Exception != null)
    {
      TestContext.Out.WriteLine("--- Exception Details ---");
      TestContext.Out.WriteLine($"Type: {result.Exception.GetType().FullName}");
      TestContext.Out.WriteLine($"Message: {result.Exception.Message}");
      TestContext.Out.WriteLine($"StackTrace:\n{result.Exception.StackTrace}");
      TestContext.Out.WriteLine("--- End Exception Details ---");
    }

    if (!string.IsNullOrEmpty(result.StandardOutput))
    {
      TestContext.Out.WriteLine("--- Standard Output ---");
      TestContext.Out.WriteLine(result.StandardOutput);
      TestContext.Out.WriteLine("--- End Standard Output ---");
    }

    if (!string.IsNullOrEmpty(result.StandardError))
    {
      TestContext.Out.WriteLine("--- Standard Error ---");
      TestContext.Out.WriteLine(result.StandardError);
      TestContext.Out.WriteLine("--- End Standard Error ---");
    }
  }
}
