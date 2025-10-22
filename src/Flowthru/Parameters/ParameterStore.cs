namespace Flowthru.Parameters;

/// <summary>
/// Stores pipeline-specific parameters in a type-safe manner.
/// </summary>
/// <remarks>
/// <para>
/// The parameter store holds parameters for each pipeline, indexed by pipeline name.
/// Each parameter set can be a different type, providing flexibility while maintaining
/// type safety at the point of registration.
/// </para>
/// <para>
/// This class is used internally by FlowthruApplication and is not typically
/// accessed directly by user code.
/// </para>
/// </remarks>
internal class ParameterStore
{
  private readonly Dictionary<string, object> _parameters = new();

  /// <summary>
  /// Stores parameters for a specific pipeline.
  /// </summary>
  /// <param name="pipelineName">The pipeline name</param>
  /// <param name="parameters">The parameter object</param>
  public void Set(string pipelineName, object parameters)
  {
    if (string.IsNullOrWhiteSpace(pipelineName))
      throw new ArgumentException("Pipeline name cannot be null or empty", nameof(pipelineName));

    _parameters[pipelineName] = parameters ?? throw new ArgumentNullException(nameof(parameters));
  }

  /// <summary>
  /// Retrieves parameters for a specific pipeline.
  /// </summary>
  /// <typeparam name="T">The expected parameter type</typeparam>
  /// <param name="pipelineName">The pipeline name</param>
  /// <returns>The parameters, or null if not found</returns>
  public T? Get<T>(string pipelineName) where T : class
  {
    if (_parameters.TryGetValue(pipelineName, out var parameters))
    {
      return parameters as T;
    }

    return null;
  }

  /// <summary>
  /// Checks if parameters are stored for a specific pipeline.
  /// </summary>
  /// <param name="pipelineName">The pipeline name</param>
  /// <returns>True if parameters exist for the pipeline</returns>
  public bool Contains(string pipelineName)
  {
    return _parameters.ContainsKey(pipelineName);
  }
}
