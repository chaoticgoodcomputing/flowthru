using Flowthru.Tests.Fixtures.TestCatalogs;

namespace Flowthru.Tests.Fixtures.TestSteps;

/// <summary>
/// Simple passthrough step that copies input to output unchanged.
/// </summary>
public static class PassthroughStep
{
  public static Func<IEnumerable<TestData>, Task<IEnumerable<TestData>>> Create()
  {
    return async (input) => await Task.FromResult(input);
  }
}

/// <summary>
/// Step that always throws an exception during execution.
/// </summary>
public static class FailingStep
{
  public static Func<IEnumerable<TestData>, Task<IEnumerable<TestData>>> Create(
    string errorMessage = "Test step failure"
  )
  {
    return async (input) =>
    {
      await Task.CompletedTask;
      throw new InvalidOperationException(errorMessage);
    };
  }
}

/// <summary>
/// Step that introduces a delay during execution.
/// </summary>
public static class DelayedStep
{
  public static Func<IEnumerable<TestData>, CancellationToken, Task<IEnumerable<TestData>>> Create(
    TimeSpan? delay = null
  )
  {
    var actualDelay = delay ?? TimeSpan.FromMilliseconds(100);
    return async (input, cancellationToken) =>
    {
      await Task.Delay(actualDelay, cancellationToken);
      return input;
    };
  }
}

/// <summary>
/// Step that transforms data by incrementing the Id field.
/// </summary>
public static class IncrementStep
{
  public static Func<IEnumerable<TestData>, Task<IEnumerable<TestData>>> Create()
  {
    return async (input) =>
    {
      var result = input.Select(item => item with { Id = item.Id + 1 });
      return await Task.FromResult(result);
    };
  }
}

/// <summary>
/// Step that transforms data by doubling the Value field.
/// </summary>
public static class DoubleValueStep
{
  public static Func<IEnumerable<TestData>, Task<IEnumerable<TestData>>> Create()
  {
    return async (input) =>
    {
      var result = input.Select(item => item with { Value = item.Value * 2 });
      return await Task.FromResult(result);
    };
  }
}

/// <summary>
/// Step that merges two datasets into one.
/// </summary>
public static class MergeStep
{
  public static Func<
    (IEnumerable<TestData>, IEnumerable<TestData>),
    Task<IEnumerable<TestData>>
  > Create()
  {
    return async (input) =>
    {
      var (first, second) = input;
      return await Task.FromResult(first.Concat(second));
    };
  }
}

/// <summary>
/// Step that splits a dataset into two halves.
/// </summary>
public static class SplitStep
{
  public static Func<
    IEnumerable<TestData>,
    Task<(IEnumerable<TestData>, IEnumerable<TestData>)>
  > Create()
  {
    return async (input) =>
    {
      var list = input.ToList();
      var midpoint = list.Count / 2;
      var result = (list.Take(midpoint).AsEnumerable(), list.Skip(midpoint).AsEnumerable());
      return await Task.FromResult(result);
    };
  }
}
