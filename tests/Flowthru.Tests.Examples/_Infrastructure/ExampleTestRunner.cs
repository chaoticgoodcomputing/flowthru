using System.Diagnostics;
using System.Reflection;
using Flowthru.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Tests.Examples.Infrastructure;

/// <summary>
/// Executes example projects programmatically by using their configured services.
/// </summary>
public sealed class ExampleTestRunner
{
  private readonly ExampleProject _example;

  /// <summary>
  /// Initializes a new instance of the <see cref="ExampleTestRunner"/> class.
  /// </summary>
  /// <param name="example">The example project to run.</param>
  public ExampleTestRunner(ExampleProject example)
  {
    _example = example ?? throw new ArgumentNullException(nameof(example));
  }

  /// <summary>
  /// Runs the example project by executing all its pipelines via IFlowthruService.
  /// </summary>
  /// <returns>The result of running the example.</returns>
  public async Task<ExampleTestResult> RunAsync()
  {
    var stopwatch = Stopwatch.StartNew();
    Exception? exception = null;
    bool success = false;
    string? diagnosticMessage = null;

    // Save the current directory and switch to the example's project directory
    var originalDirectory = Directory.GetCurrentDirectory();

    try
    {
      // Change to the example's project directory so relative paths work
      Directory.SetCurrentDirectory(_example.ProjectPath);

      // Find and invoke ConfigureServices method
      var configureServicesMethod = FindConfigureServicesMethod(_example.EntryPointType);
      if (configureServicesMethod == null)
      {
        throw new InvalidOperationException(
          $"Could not find ConfigureServices() method in {_example.EntryPointType.FullName}. "
            + "Example projects must expose a public static IServiceProvider ConfigureServices() method."
        );
      }

      // Get the service provider
      var services = (IServiceProvider?)configureServicesMethod.Invoke(null, null);
      if (services == null)
      {
        throw new InvalidOperationException(
          $"ConfigureServices() returned null in {_example.EntryPointType.FullName}"
        );
      }

      // Get the Flowthru service
      var flowthruService = services.GetRequiredService<IFlowthruService>();

      // Execute all pipelines
      var result = await flowthruService.ExecuteAllPipelinesAsync();

      success = result.Success;
      exception = result.Exception;

      if (!success && exception == null)
      {
        diagnosticMessage = "Pipeline execution completed without exception but reported failure";
      }
    }
    catch (TargetInvocationException tie)
    {
      // Unwrap the inner exception
      exception = tie.InnerException ?? tie;
      success = false;
    }
    catch (Exception ex)
    {
      exception = ex;
      success = false;
    }
    finally
    {
      stopwatch.Stop();

      // Restore the original working directory
      Directory.SetCurrentDirectory(originalDirectory);
    }

    var category = CategorizeFailure(success, exception);

    return new ExampleTestResult
    {
      Example = _example,
      CapturedOutput = null, // No longer capturing output - service layer doesn't write to console
      ExitCode = success ? 0 : 1,
      Exception = exception,
      Duration = stopwatch.Elapsed,
      Category = category,
      DiagnosticMessage = diagnosticMessage,
    };
  }

  /// <summary>
  /// Categorizes the type of failure based on the exception and success status.
  /// </summary>
  private static FailureCategory CategorizeFailure(bool success, Exception? exception)
  {
    // Success case
    if (success && exception == null)
    {
      return FailureCategory.None;
    }

    // Infrastructure failures - test framework issues
    if (
      exception is InvalidOperationException
      || exception is TargetException
      || exception is MethodAccessException
      || exception is ArgumentException
    )
    {
      return FailureCategory.Infrastructure;
    }

    // Application failures - expected runtime issues
    var applicationFailureIndicators = new[]
    {
      "FileNotFoundException",
      "DirectoryNotFoundException",
      "Configuration section",
      "not found",
      "InvalidCastException",
      "connection",
      "authentication",
      "authorization",
    };

    var exceptionMessage = exception?.ToString() ?? "";
    if (
      applicationFailureIndicators.Any(indicator =>
        exceptionMessage.Contains(indicator, StringComparison.OrdinalIgnoreCase)
      )
    )
    {
      return FailureCategory.Application;
    }

    // Default to application failure
    return FailureCategory.Application;
  }

  /// <summary>
  /// Finds the ConfigureServices method in the given type.
  /// </summary>
  private static MethodInfo? FindConfigureServicesMethod(Type type)
  {
    // Look for public static method named "ConfigureServices" that returns IServiceProvider
    var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);

    return methods.FirstOrDefault(m =>
      m.Name == "ConfigureServices"
      && m.ReturnType == typeof(IServiceProvider)
      && m.GetParameters().Length == 0
    );
  }
}
