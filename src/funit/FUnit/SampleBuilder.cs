namespace Flowthru.Step.Testing;

/// <summary>
/// Helpers for constructing typed sample data inside step tests.
/// Resolved off <see cref="FUnitContext.Samples"/>, e.g.
/// <c>Samples.Of(row1, row2)</c> or
/// <c>Samples.Generate(10, i =&gt; new Foo { Id = i })</c>.
/// </summary>
public class SampleBuilder
{
  /// <summary>Wrap explicit instances into an <see cref="IEnumerable{T}"/>.</summary>
  public IEnumerable<T> Of<T>(params T[] items) => items;

  /// <summary>
  /// Generate <paramref name="count"/> rows by invoking
  /// <paramref name="factory"/> with the zero-based row index.
  /// </summary>
  public IEnumerable<T> Generate<T>(int count, Func<int, T> factory)
  {
    for (var i = 0; i < count; i++)
    {
      yield return factory(i);
    }
  }
}
