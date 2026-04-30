using System.Reflection;
using Flowthru.Core.Data.Validation;
using Flowthru.Core.Graph;
using Flowthru.FUnit.Samples;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.FUnit;

/// <summary>
/// Framework-agnostic base class for Flowthru step and effect tests.
/// Subclass this in any test framework (NUnit, xUnit, MSTest) to gain
/// typed step invocation, pre-flight validation, sample data helpers,
/// and a DI service collection scoped to the test.
/// </summary>
/// <remarks>
/// <para>
/// Analogous to <c>BunitContext</c> in bUnit — provides a controlled environment
/// in which a single unit (a step function or an effect node) can be exercised
/// in isolation.
/// </para>
/// <para>
/// <strong>Usage:</strong>
/// <code>
/// public class EvaluateModelStepTests : FunitContext
/// {
///     [StepTest(typeof(EvaluateModelStep))]
///     public void PerfectPredictions_ShouldReturn100PercentAccuracy()
///     {
///         var input = (
///             Samples.Of(new PredictionRow { Class = 0 }),
///             Samples.Of(new LabelRow { Setosa = 1.0 })
///         );
///         var result = Invoke(EvaluateModelStep.Create(), input);
///         Assert.That(result.Accuracy, Is.EqualTo(1.0));
///     }
/// }
/// </code>
/// </para>
/// <para>
/// <strong>DI services:</strong> Register services in <see cref="Services"/> before
/// the first call to <see cref="ServiceProvider"/> or any <c>Invoke</c> method.
/// The service collection is frozen on first access.
/// </para>
/// </remarks>
public class FunitContext : IDisposable
{
  private bool _disposed;
  private IServiceProvider? _serviceProvider;
  private readonly ServiceCollection _services = new();

  /// <summary>
  /// Constructs the test context and auto-registers any
  /// <see cref="FUnitStubContainerAttribute"/>-attributed static classes from the
  /// derived class's assembly. Per-test or per-fixture <see cref="Services"/>
  /// registrations made after the constructor runs override stub-container defaults.
  /// </summary>
  public FunitContext()
  {
    RegisterStubContainers();
  }

  /// <summary>
  /// DI service collection for the test. Register services here before the first
  /// call to <see cref="ServiceProvider"/>. Frozen after first access.
  /// </summary>
  /// <remarks>
  /// Stub registrations from <see cref="FUnitStubContainerAttribute"/>-marked classes
  /// are added during construction, so per-test code added here runs second and wins
  /// on conflicts (mirrors ASP.NET <c>WebApplicationFactory.ConfigureTestServices</c>
  /// semantics).
  /// </remarks>
  public IServiceCollection Services => _services;

  /// <summary>
  /// Sample data construction helpers.
  /// </summary>
  public SampleBuilder Samples { get; } = new SampleBuilder();

  /// <summary>
  /// Lazily-built DI service provider. Freezes <see cref="Services"/> on first access.
  /// </summary>
  protected IServiceProvider ServiceProvider =>
    _serviceProvider ??= _services.BuildServiceProvider();

  /// <summary>
  /// Resolves a required service from the test's DI container. Sugar for
  /// <c>ServiceProvider.GetRequiredService&lt;T&gt;()</c> that keeps test code terse.
  /// </summary>
  /// <typeparam name="T">The service type to resolve.</typeparam>
  /// <exception cref="InvalidOperationException">
  /// Thrown when no service of type <typeparamref name="T"/> is registered. Add a
  /// registration to a <see cref="FUnitStubContainerAttribute"/>-marked class or
  /// directly to <see cref="Services"/> to fix.
  /// </exception>
  protected T GetRequiredService<T>() where T : notnull =>
    ServiceProvider.GetRequiredService<T>();

  /// <summary>
  /// Invokes a synchronous step function with the given input and returns the output.
  /// </summary>
  protected TOutput Invoke<TInput, TOutput>(Func<TInput, TOutput> step, TInput input) =>
    step(input);

  /// <summary>
  /// Invokes an asynchronous step function with the given input and returns the output.
  /// </summary>
  protected Task<TOutput> InvokeAsync<TInput, TOutput>(
    Func<TInput, Task<TOutput>> step,
    TInput input
  ) => step(input);

  /// <summary>
  /// Invokes an asynchronous cancellable step function with the given input.
  /// </summary>
  protected Task<TOutput> InvokeAsync<TInput, TOutput>(
    Func<TInput, CancellationToken, Task<TOutput>> step,
    TInput input,
    CancellationToken cancellationToken = default
  ) => step(input, cancellationToken);

  /// <summary>
  /// Runs pre-flight validation on any <see cref="INode"/> — items, effects, or steps.
  /// </summary>
  protected async Task<ValidationResult> Validate(
    INode node,
    CancellationToken cancellationToken = default
  ) => await node.Validate().Run(cancellationToken);

  /// <summary>
  /// Reflects on the derived test class's assembly to find every
  /// <see cref="FUnitStubContainerAttribute"/>-attributed type, then invokes each one's
  /// <c>public static void Configure(IServiceCollection)</c> method against
  /// <see cref="_services"/>. Containers without the expected method signature are
  /// silently ignored — the analyzer can warn about misconfigurations separately.
  /// </summary>
  private void RegisterStubContainers()
  {
    var assembly = GetType().Assembly;
    Type[] types;
    try
    {
      types = assembly.GetTypes();
    }
    catch (ReflectionTypeLoadException ex)
    {
      // Some types failed to load (e.g., missing optional refs); proceed with what loaded.
      types = ex.Types.Where(t => t is not null).Cast<Type>().ToArray();
    }

    foreach (var type in types)
    {
      if (type.GetCustomAttribute<FUnitStubContainerAttribute>() is null)
      {
        continue;
      }

      var configureMethod = type.GetMethod(
        name: "Configure",
        bindingAttr: BindingFlags.Public | BindingFlags.Static,
        binder: null,
        types: new[] { typeof(IServiceCollection) },
        modifiers: null
      );
      if (configureMethod is null)
      {
        // Convention violation — no Configure(IServiceCollection) found. Skip silently;
        // the analyzer (Phase 5 follow-up) reports these.
        continue;
      }

      configureMethod.Invoke(null, new object[] { _services });
    }
  }

  /// <inheritdoc/>
  public void Dispose()
  {
    Dispose(disposing: true);
    GC.SuppressFinalize(this);
  }

  /// <summary>Dispose pattern implementation.</summary>
  protected virtual void Dispose(bool disposing)
  {
    if (_disposed)
    {
      return;
    }

    if (disposing && _serviceProvider is IDisposable disposable)
    {
      disposable.Dispose();
    }

    _disposed = true;
  }
}
