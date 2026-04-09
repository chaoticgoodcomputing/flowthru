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
  /// DI service collection for the test. Register services here before the first
  /// call to <see cref="ServiceProvider"/>. Frozen after first access.
  /// </summary>
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
      return;

    if (disposing && _serviceProvider is IDisposable disposable)
      disposable.Dispose();

    _disposed = true;
  }
}
