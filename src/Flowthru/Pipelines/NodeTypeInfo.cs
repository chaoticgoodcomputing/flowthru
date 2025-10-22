using Flowthru.Nodes;

namespace Flowthru.Pipelines;

/// <summary>
/// Extracts type parameter information from NodeBase-derived types.
/// Used by PipelineBuilder to infer TInput, TOutput, and TParameters from TNode.
/// </summary>
internal static class NodeTypeInfo {
  /// <summary>
  /// Extracts the three type parameters (TInput, TOutput, TParameters) from a NodeBase-derived type.
  /// </summary>
  /// <param name="nodeType">The node type (must derive from NodeBase&lt;,,&gt;)</param>
  /// <returns>Tuple of (TInput, TOutput, TParameters)</returns>
  /// <exception cref="InvalidOperationException">Thrown if nodeType doesn't derive from NodeBase</exception>
  public static (Type TInput, Type TOutput, Type TParameters) ExtractTypeArguments(Type nodeType) {
    // Walk up the inheritance chain to find NodeBase<TInput, TOutput, TParameters>
    var currentType = nodeType;

    while (currentType != null && currentType != typeof(object)) {
      if (currentType.IsGenericType) {
        var genericDef = currentType.GetGenericTypeDefinition();

        // Check if this is NodeBase<TInput, TOutput, TParameters>
        if (genericDef == typeof(NodeBase<,,>)) {
          var typeArgs = currentType.GetGenericArguments();
          return (typeArgs[0], typeArgs[1], typeArgs[2]);
        }
      }

      currentType = currentType.BaseType;
    }

    throw new InvalidOperationException(
      $"Type {nodeType.Name} does not derive from NodeBase<TInput, TOutput, TParameters>. " +
      "All nodes must inherit from NodeBase.");
  }

  /// <summary>
  /// Validates that a CatalogMap's schema type matches the expected type.
  /// </summary>
  public static void ValidateCatalogMapType<TSchema>(Type expectedType, string parameterName)
    where TSchema : new() {
    if (typeof(TSchema) != expectedType) {
      throw new InvalidOperationException(
        $"Type mismatch: {parameterName} uses CatalogMap<{typeof(TSchema).Name}>, " +
        $"but node expects {expectedType.Name}. " +
        "Ensure the CatalogMap schema type matches the node's input/output type.");
    }
  }
}
