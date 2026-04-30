namespace ProjectName.Flows.FlowName.Steps;

/// <summary>
/// Dummy node for FlowName pipeline - replace with actual transformation logic.
/// </summary>
public static class FlowNameDummyStep
{
  /// <summary>
  /// Creates a dummy 0-input/0-output transformation. Replace with your actual
  /// processing logic — typically a Func&lt;TIn, TOut&gt; with real inputs and outputs
  /// wired through the catalog.
  /// </summary>
  /// <returns>
  /// An <see cref="Action"/> placeholder that does nothing.
  /// </returns>
  public static Action Create()
  {
    return () =>
    {
      // TODO: Replace this dummy node with actual transformation logic.
      // Typical shape:
      //   public static Func<IEnumerable<InputSchema>, IEnumerable<OutputSchema>> Create()
      //   {
      //     return input => input.Select(item => new OutputSchema { ... });
      //   }
    };
  }
}
