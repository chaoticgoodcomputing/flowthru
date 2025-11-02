namespace Flowthru.Tests.KedroSpaceflights.Pipelines.DataDiagnostics.Nodes;

/// <summary>
/// Generic pass-through node that exports data to CSV format for debugging.
/// </summary>
/// <typeparam name="T">The type of data to export</typeparam>
/// <remarks>
/// This is a diagnostic node that simply passes data through while writing
/// it to a CSV catalog entry. Useful for debugging pipeline data issues.
/// </remarks>
public static class PassthroughInputToOutputNode<T>
{
  public static Func<IEnumerable<T>, Task<IEnumerable<T>>> Create()
  {
    return async (input) =>
    {
      // Pass-through: return input unchanged
      return input;
    };
  }
}
