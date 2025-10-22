namespace Flowthru.Nodes;

/// <summary>
/// Marker type for nodes that don't require parameters.
/// Used as the default TParameters type in NodeBase&lt;TInput, TOutput, TParameters&gt;.
/// </summary>
/// <remarks>
/// <para>
/// This is a simple empty class that serves as a shorthand for users and the library
/// when no parameters are needed for a node. Nodes that don't need configuration can
/// omit the third type parameter by using the two-parameter NodeBase&lt;TInput, TOutput&gt;
/// convenience base class.
/// </para>
/// <para>
/// <strong>Usage Examples:</strong>
/// </para>
/// <code>
/// // Explicit NoParams (rarely needed)
/// public class MyNode : NodeBase&lt;Input, Output, NoParams&gt; { }
/// 
/// // Recommended: Use two-parameter base class
/// public class MyNode : NodeBase&lt;Input, Output&gt; { }
/// 
/// // With parameters
/// public class ConfigurableNode : NodeBase&lt;Input, Output, MyParameters&gt;
/// {
///     // Parameters property is automatically available
/// }
/// </code>
/// </remarks>
public sealed class NoParams {
  // Empty marker class - no properties or methods needed
}
