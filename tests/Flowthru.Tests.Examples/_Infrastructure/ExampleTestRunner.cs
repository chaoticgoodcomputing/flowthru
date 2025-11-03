using System.Diagnostics;
using System.Reflection;

namespace Flowthru.Tests.Examples.Infrastructure;

/// <summary>
/// Executes example projects programmatically by invoking their Main methods.
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
  /// Runs the example project by invoking its Main method.
  /// </summary>
  /// <param name="args">Command-line arguments to pass to the example.</param>
  /// <returns>The result of running the example.</returns>
  public async Task<ExampleTestResult> RunAsync(params string[] args)
  {
    var stopwatch = Stopwatch.StartNew();
    int exitCode = 0;
    Exception? exception = null;
    string? capturedOutput = null;

    // Save the current directory and switch to the example's project directory
    var originalDirectory = Directory.GetCurrentDirectory();

    // Capture console output
    var originalOut = Console.Out;
    var originalError = Console.Error;
    var outputCapture = new StringWriter();

    try
    {
      // Redirect console output to capture it
      Console.SetOut(outputCapture);
      Console.SetError(outputCapture);

      // Change to the example's project directory so relative paths work
      Directory.SetCurrentDirectory(_example.ProjectPath);

      // Find the Main method
      var mainMethod = FindMainMethod(_example.EntryPointType);
      if (mainMethod == null)
      {
        throw new InvalidOperationException(
          $"Could not find Main method in {_example.EntryPointType.FullName}"
        );
      }

      // Determine method signature and invoke accordingly
      var parameters = mainMethod.GetParameters();
      var returnType = mainMethod.ReturnType;

      object? result;

      if (parameters.Length == 0)
      {
        // Main() or Main() returning Task
        result = mainMethod.Invoke(null, null);
      }
      else if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string[]))
      {
        // Main(string[] args) or Main(string[] args) returning Task
        result = mainMethod.Invoke(null, new object[] { args });
      }
      else
      {
        throw new InvalidOperationException(
          $"Unsupported Main method signature in {_example.EntryPointType.FullName}"
        );
      }

      // Handle async Main methods
      if (result is Task<int> taskInt)
      {
        exitCode = await taskInt;
      }
      else if (result is Task task)
      {
        await task;
        exitCode = 0;
      }
      else if (result is int intResult)
      {
        exitCode = intResult;
      }
      else if (result == null)
      {
        exitCode = 0;
      }
      else
      {
        throw new InvalidOperationException(
          $"Unexpected return type from Main: {result.GetType().FullName}"
        );
      }
    }
    catch (TargetInvocationException tie)
    {
      // Unwrap the inner exception
      exception = tie.InnerException ?? tie;
      exitCode = 1;
    }
    catch (Exception ex)
    {
      exception = ex;
      exitCode = 1;
    }
    finally
    {
      stopwatch.Stop();

      // Capture the output before restoring console
      capturedOutput = outputCapture.ToString();

      // Restore console output
      Console.SetOut(originalOut);
      Console.SetError(originalError);
      outputCapture.Dispose();

      // Restore the original working directory
      Directory.SetCurrentDirectory(originalDirectory);
    }

    var (category, diagnosticMessage) = CategorizeFailure(exitCode, exception);

    return new ExampleTestResult
    {
      Example = _example,
      CapturedOutput = capturedOutput,
      ExitCode = exitCode,
      Exception = exception,
      Duration = stopwatch.Elapsed,
      Category = category,
      DiagnosticMessage = diagnosticMessage,
    };
  }

  /// <summary>
  /// Categorizes the type of failure based on the exception and exit code.
  /// </summary>
  private static (FailureCategory Category, string? DiagnosticMessage) CategorizeFailure(
    int exitCode,
    Exception? exception
  )
  {
    // Success case
    if (exitCode == 0 && exception == null)
    {
      return (FailureCategory.None, null);
    }

    // Infrastructure failures - test framework issues
    if (
      exception is InvalidOperationException
      || exception is TargetException
      || exception is MethodAccessException
      || exception is ArgumentException
    )
    {
      return (
        FailureCategory.Infrastructure,
        $"Test infrastructure error: {exception.GetType().Name}"
      );
    }

    // Application failures - expected runtime issues
    var applicationFailureIndicators = new[]
    {
      "FileNotFoundException",
      "DirectoryNotFoundException",
      "Configuration section",
      "not found",
      "InvalidCastException", // State pollution issue
      "connection", // Database/network issues
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
      return (
        FailureCategory.Application,
        $"Example needs setup: {exception?.GetType().Name ?? "Non-zero exit code"}"
      );
    }

    // Default to application failure for unknown cases
    return (
      FailureCategory.Application,
      exception != null ? $"Runtime error: {exception.GetType().Name}" : "Non-zero exit code"
    );
  }

  /// <summary>
  /// Finds the Main method in the given type.
  /// </summary>
  private static MethodInfo? FindMainMethod(Type type)
  {
    // Look for public static method named "Main"
    var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);

    return methods.FirstOrDefault(m =>
      m.Name == "Main"
      && (
        m.ReturnType == typeof(void)
        || m.ReturnType == typeof(int)
        || m.ReturnType == typeof(Task)
        || m.ReturnType == typeof(Task<int>)
      )
    );
  }
}
