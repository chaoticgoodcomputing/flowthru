using System.Runtime.CompilerServices;

namespace Flowthru.Core.Effects;

/// <summary>
/// Represents a cancellable asynchronous effect that produces a value of type <typeparamref name="A"/>.
/// </summary>
/// <typeparam name="A">The type of value produced by this effect.</typeparam>
/// <remarks>
/// <para>
/// <see cref="FlowIO{A}"/> is a lightweight effect monad for representing I/O operations.
/// It provides:
/// </para>
/// <list type="bullet">
/// <item>Lazy evaluation - effects don't run until <see cref="Run"/> is called</item>
/// <item>Cancellation support - all effects accept a <see cref="CancellationToken"/></item>
/// <item>Functor/Monad operations - <see cref="Map{B}"/>, <see cref="Bind{B}"/></item>
/// <item>LINQ comprehension syntax - via <see cref="Select{B}"/> and <see cref="SelectMany{B,C}"/></item>
/// </list>
/// <para>
/// <strong>Example - Basic usage:</strong>
/// </para>
/// <code>
/// FlowIO&lt;string&gt; ReadFile(string path) =>
///     FlowIO.LiftAsync(ct => File.ReadAllTextAsync(path, ct));
///
/// FlowIO&lt;int&gt; GetWordCount(string path) =>
///     from content in ReadFile(path)
///     select content.Split(' ').Length;
///
/// int count = await GetWordCount("data.txt").Run();
/// </code>
/// <para>
/// <strong>Example - Error handling:</strong>
/// </para>
/// <code>
/// FlowIO&lt;Data&gt; LoadData() =>
///     FlowIO.LiftAsync(async ct => {
///         if (!File.Exists("data.json"))
///             throw new FileNotFoundException("Data file missing");
///         return await JsonSerializer.DeserializeAsync&lt;Data&gt;(...);
///     });
///
/// try {
///     var data = await LoadData().Run();
/// }
/// catch (FileNotFoundException ex) {
///     // Handle error
/// }
/// </code>
/// </remarks>
public readonly struct FlowIO<A>
{
  private readonly Func<CancellationToken, ValueTask<A>>? _thunk;

  /// <summary>
  /// Initializes a new instance of <see cref="FlowIO{A}"/> with the given effect function.
  /// </summary>
  /// <param name="thunk">The function that produces the effect when executed.</param>
  internal FlowIO(Func<CancellationToken, ValueTask<A>> thunk)
  {
    _thunk = thunk;
  }

  /// <summary>
  /// Executes this effect and returns the result.
  /// </summary>
  /// <param name="token">Optional cancellation token to cancel the effect.</param>
  /// <returns>A <see cref="ValueTask{A}"/> representing the asynchronous operation.</returns>
  /// <exception cref="InvalidOperationException">Thrown if the effect is uninitialized.</exception>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public ValueTask<A> Run(CancellationToken token = default)
  {
    if (_thunk is null)
    {
      return ValueTask.FromException<A>(
        new InvalidOperationException("Cannot run an uninitialized FlowIO effect.")
      );
    }

    return _thunk(token);
  }

  /// <summary>
  /// Maps the result of this effect using the specified function.
  /// </summary>
  /// <typeparam name="B">The result type after mapping.</typeparam>
  /// <param name="f">The function to apply to the effect's result.</param>
  /// <returns>A new effect that applies <paramref name="f"/> to this effect's result.</returns>
  /// <remarks>
  /// This is the functor's <c>fmap</c> operation. It transforms the value inside the effect
  /// without changing the effect structure.
  /// </remarks>
  public FlowIO<B> Map<B>(Func<A, B> f)
  {
    var thunk = _thunk; // Copy to avoid capturing 'this' in lambda
    if (thunk is null)
    {
      return new FlowIO<B>(_ =>
        ValueTask.FromException<B>(
          new InvalidOperationException("Cannot map over an uninitialized FlowIO effect.")
        )
      );
    }

    return new FlowIO<B>(async ct =>
    {
      var a = await thunk(ct).ConfigureAwait(false);
      return f(a);
    });
  }

  /// <summary>
  /// Transforms this effect using the specified function (alias for <see cref="Map{B}"/>).
  /// Enables LINQ <c>select</c> syntax.
  /// </summary>
  /// <typeparam name="B">The result type after transformation.</typeparam>
  /// <param name="selector">The function to apply to the effect's result.</param>
  /// <returns>A new effect with the transformed result.</returns>
  /// <remarks>
  /// <strong>Example:</strong>
  /// <code>
  /// var result = from x in GetValue()
  ///              select x * 2;
  /// </code>
  /// </remarks>
  public FlowIO<B> Select<B>(Func<A, B> selector) => Map(selector);

  /// <summary>
  /// Projects this effect through a function that produces another effect, then combines
  /// both results using a projection function. Enables LINQ <c>from...from...select</c> syntax.
  /// </summary>
  /// <typeparam name="B">The intermediate result type.</typeparam>
  /// <typeparam name="C">The final result type.</typeparam>
  /// <param name="bind">The function that produces the next effect based on this effect's result.</param>
  /// <param name="project">The function that combines both results into the final result.</param>
  /// <returns>A new effect representing the composition.</returns>
  /// <remarks>
  /// <strong>Example:</strong>
  /// <code>
  /// var result = from x in GetFirstValue()
  ///              from y in GetSecondValue(x)
  ///              select x + y;
  /// </code>
  /// </remarks>
  public FlowIO<C> SelectMany<B, C>(Func<A, FlowIO<B>> bind, Func<A, B, C> project)
  {
    var thunk = _thunk; // Copy to avoid capturing 'this' in lambda
    if (thunk is null)
    {
      return new FlowIO<C>(_ =>
        ValueTask.FromException<C>(
          new InvalidOperationException("Cannot bind over an uninitialized FlowIO effect.")
        )
      );
    }

    return new FlowIO<C>(async ct =>
    {
      var a = await thunk(ct).ConfigureAwait(false);
      var flowB = bind(a);
      var b = await flowB.Run(ct).ConfigureAwait(false);
      return project(a, b);
    });
  }

}

/// <summary>
/// Provides factory methods and combinators for creating <see cref="FlowIO{A}"/> effects.
/// </summary>
public static class FlowIO
{
  /// <summary>
  /// Creates an effect that immediately returns the given value.
  /// </summary>
  /// <typeparam name="A">The type of value.</typeparam>
  /// <param name="value">The value to return.</param>
  /// <returns>An effect that produces <paramref name="value"/>.</returns>
  /// <remarks>
  /// This is the monad's <c>return</c> or <c>pure</c> operation.
  /// </remarks>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static FlowIO<A> Pure<A>(A value) => new(_ => ValueTask.FromResult(value));

  /// <summary>
  /// Creates an effect that immediately fails with the given exception.
  /// </summary>
  /// <typeparam name="A">The expected result type.</typeparam>
  /// <param name="error">The exception to throw.</param>
  /// <returns>An effect that fails with <paramref name="error"/>.</returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static FlowIO<A> Fail<A>(Exception error) => new(_ => ValueTask.FromException<A>(error));

  /// <summary>
  /// Creates an effect that immediately fails with an exception containing the given message.
  /// </summary>
  /// <typeparam name="A">The expected result type.</typeparam>
  /// <param name="message">The error message.</param>
  /// <returns>An effect that fails with an exception containing <paramref name="message"/>.</returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static FlowIO<A> Fail<A>(string message) =>
    new(_ => ValueTask.FromException<A>(new Exception(message)));

  /// <summary>
  /// Lifts a synchronous function into an effect.
  /// </summary>
  /// <typeparam name="A">The return type.</typeparam>
  /// <param name="f">The function to lift.</param>
  /// <returns>An effect that executes <paramref name="f"/>.</returns>
  /// <remarks>
  /// The function is still executed lazily - only when <see cref="FlowIO{A}.Run"/> is called.
  /// </remarks>
  public static FlowIO<A> Lift<A>(Func<A> f) =>
    new(_ =>
    {
      try
      {
        return ValueTask.FromResult(f());
      }
      catch (Exception ex)
      {
        return ValueTask.FromException<A>(ex);
      }
    });

  /// <summary>
  /// Lifts a cancellation-aware <see cref="ValueTask{A}"/>-returning function into an effect.
  /// </summary>
  /// <typeparam name="A">The return type.</typeparam>
  /// <param name="f">The function that accepts a cancellation token and returns a <see cref="ValueTask{A}"/>.</param>
  /// <returns>An effect that executes <paramref name="f"/>.</returns>
  /// <remarks>
  /// <para>
  /// All async I/O operations should observe the cancellation token to support graceful shutdown.
  /// If your operation is truly synchronous, use <see cref="Lift{A}"/> instead.
  /// </para>
  /// <para>
  /// For Task-based APIs, convert using <c>.AsTask()</c>: <c>LiftAsync(async ct => await SomeTaskAsync(ct).AsTask())</c>
  /// or rely on implicit conversion from Task to ValueTask.
  /// </para>
  /// </remarks>
  public static FlowIO<A> LiftAsync<A>(Func<CancellationToken, ValueTask<A>> f) => new(f);
}
