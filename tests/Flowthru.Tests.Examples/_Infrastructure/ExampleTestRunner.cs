using System.Diagnostics;
using System.Reflection;
using Flowthru.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Tests.Examples.Infrastructure;

/// <summary>
/// Executes an example project by invoking its <c>ConfigureServices(basePath)</c> method,
/// then running all pipelines via <see cref="IFlowthruService"/>.
/// </summary>
/// <remarks>
/// <para>Design invariants:</para>
/// <list type="bullet">
///   <item><b>No <c>Directory.SetCurrentDirectory</c></b> — the project path is passed as
///         <c>basePath</c> to <c>ConfigureServices</c>, making execution safe for parallel tests.</item>
///   <item><b>Hard timeout via <c>Task.WhenAny</c></b> — prevents infinite hangs even when
///         the pipeline does not honour <see cref="CancellationToken"/>.</item>
///   <item><b>Cooperative cancellation</b> — a <see cref="CancellationToken"/> is passed to
///         <c>ExecuteAllFlowsAsync</c> so pipelines that do check it can exit early on timeout.</item>
/// </list>
/// </remarks>
public sealed class ExampleTestRunner
{
  /// <summary>Default per-example timeout. Override via constructor parameter.</summary>
  public static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5);

  private readonly ExampleProject _example;
  private readonly TimeSpan _timeout;

  public ExampleTestRunner(ExampleProject example, TimeSpan? timeout = null)
  {
    _example = example ?? throw new ArgumentNullException(nameof(example));
    _timeout = timeout ?? DefaultTimeout;
  }

  /// <summary>
  /// Runs all pipelines in the example. Throws on failure or timeout.
  /// </summary>
  public async Task RunAsync()
  {
    var stopwatch = Stopwatch.StartNew();

    IServiceProvider? services = null;
    try
    {
      services = BuildServiceProvider();
      var flowthruService = services.GetRequiredService<IFlowthruService>();

      using var cts = new CancellationTokenSource();
      var executionTask = flowthruService.ExecuteFlowAsync(cancellationToken: cts.Token);
      var timeoutTask = Task.Delay(_timeout, CancellationToken.None);

      var completedTask = await Task.WhenAny(executionTask, timeoutTask);

      if (completedTask == timeoutTask)
      {
        await cts.CancelAsync();
        throw new TimeoutException(
          $"Example '{_example.Name}' exceeded the {_timeout.TotalMinutes:F0}-minute timeout. "
            + "This may indicate an infinite loop or a pipeline waiting on unavailable input."
        );
      }

      var result = await executionTask;
      stopwatch.Stop();

      TestContext.Out.WriteLine($"  Completed in {stopwatch.Elapsed.TotalSeconds:F2}s");

      if (!result.Success)
      {
        var message = $"Example '{_example.Name}' pipeline execution reported failure.";

        if (result.Exception != null)
        {
          TestContext.Out.WriteLine(
            $"  Exception: {result.Exception.GetType().Name}: {result.Exception.Message}"
          );
          throw new InvalidOperationException(message, result.Exception);
        }

        throw new InvalidOperationException(message);
      }
    }
    catch (TargetInvocationException tie)
    {
      // Unwrap reflection wrappers so the test sees the real exception.
      throw tie.InnerException ?? tie;
    }
    finally
    {
      if (services is IAsyncDisposable asyncDisposable)
      {
        await asyncDisposable.DisposeAsync();
      }
      else if (services is IDisposable disposable)
      {
        disposable.Dispose();
      }
    }
  }

  /// <summary>
  /// Invokes the example's <c>ConfigureServices</c> method, passing the project path
  /// as <c>basePath</c> when the method accepts a parameter.
  /// </summary>
  private IServiceProvider BuildServiceProvider()
  {
    var method =
      FindConfigureServicesMethod(_example.EntryPointType)
      ?? throw new InvalidOperationException(
        $"No public static ConfigureServices method found on {_example.EntryPointType.FullName}. "
          + "Example projects must expose: public static IServiceProvider ConfigureServices(string? basePath = null)"
      );

    var parameters = method.GetParameters();

    object?[]? args = parameters.Length switch
    {
      0 => null,
      1 when parameters[0].ParameterType == typeof(string) => [_example.ProjectPath],
      2
        when parameters[0].ParameterType == typeof(string)
          && parameters[1].ParameterType == typeof(string) => (object?[])
        [_example.ProjectPath, _example.OutputPath],
      _ => throw new InvalidOperationException(
        $"ConfigureServices on {_example.EntryPointType.FullName} has an unexpected signature. "
          + "Expected: IServiceProvider ConfigureServices() or "
          + "IServiceProvider ConfigureServices(string? basePath) or "
          + "IServiceProvider ConfigureServices(string? basePath, string? outputPath)"
      ),
    };

    return method.Invoke(null, args) as IServiceProvider
      ?? throw new InvalidOperationException(
        $"ConfigureServices on {_example.EntryPointType.FullName} returned null."
      );
  }

  /// <summary>
  /// Finds the <c>ConfigureServices</c> method, preferring the overload that accepts
  /// a <c>basePath</c> parameter (so we can inject the project directory).
  /// </summary>
  private static MethodInfo? FindConfigureServicesMethod(Type type)
  {
    return type.GetMethods(BindingFlags.Public | BindingFlags.Static)
      .Where(m => m.Name == "ConfigureServices" && m.ReturnType == typeof(IServiceProvider))
      .OrderByDescending(m => m.GetParameters().Length)
      .FirstOrDefault();
  }
}
