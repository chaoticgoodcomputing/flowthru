using Flowthru.Nodes;

namespace Flowthru.Tests.KedroSpaceflights.Pipelines.DataProcessing.Nodes;

/// <summary>
/// Generic pass-through node that exports data to CSV format for debugging.
/// </summary>
/// <typeparam name="T">The type of data to export</typeparam>
/// <remarks>
/// This is a diagnostic node that simply passes data through while writing
/// it to a CSV catalog entry. Useful for debugging pipeline data issues.
/// </remarks>
public class ExportToCsvNode<T> : NodeBase<T, T, NoParams> {
  protected override Task<IEnumerable<T>> Transform(IEnumerable<T> input) {
    // Pass-through: return input unchanged
    return Task.FromResult(input);
  }
}
