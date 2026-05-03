# <a id="Flowthru_Core_Effects"></a> Namespace Flowthru.Core.Effects

### Classes

 [FlowIO](Flowthru.Core.Effects.FlowIO.md)

Provides factory methods and combinators for creating <xref href="Flowthru.Core.Effects.FlowIO%601" data-throw-if-not-resolved="false"></xref> effects.

### Structs

 [FlowIO<A\>](Flowthru.Core.Effects.FlowIO\-1.md)

Represents a cancellable asynchronous effect that produces a value of type <code class="typeparamref">A</code>.

 [FlowUnit](Flowthru.Core.Effects.FlowUnit.md)

Represents a void-like value for effect operations with no meaningful return value.
Similar to <code>Unit</code> in functional programming or <code>void</code> in imperative programming,
but usable as a type parameter.

### Interfaces

 [IFlowthruInspector<TService\>](Flowthru.Core.Effects.IFlowthruInspector\-1.md)

Sidecar capability contract for preflight-inspecting a service that a step depends on.

