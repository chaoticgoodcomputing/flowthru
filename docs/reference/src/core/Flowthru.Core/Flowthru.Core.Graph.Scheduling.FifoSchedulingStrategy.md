# <a id="Flowthru_Core_Graph_Scheduling_FifoSchedulingStrategy"></a> Class FifoSchedulingStrategy

Namespace: [Flowthru.Core.Graph.Scheduling](Flowthru.Core.Graph.Scheduling.md)  
Assembly: Flowthru.Core.dll  

Scheduling strategy that preserves arrival order (first-in, first-out).

```csharp
public sealed class FifoSchedulingStrategy : ISchedulingStrategy
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[FifoSchedulingStrategy](Flowthru.Core.Graph.Scheduling.FifoSchedulingStrategy.md)

#### Implements

[ISchedulingStrategy](Flowthru.Core.Graph.Scheduling.ISchedulingStrategy.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

Equivalent to the behaviour of the original <code>ConcurrentQueue</code>-based
dispatcher: steps become eligible for dispatch in the order their last
dependency completes, and that order is preserved when claiming worker slots.

## Methods

### <a id="Flowthru_Core_Graph_Scheduling_FifoSchedulingStrategy_Prioritize_System_Collections_Generic_IReadOnlyList_Flowthru_Core_Graph_FlowStep__Flowthru_Core_Graph_Scheduling_SchedulingContext_"></a> Prioritize\(IReadOnlyList<FlowStep\>, SchedulingContext\)

Returns <code class="paramref">readySteps</code> sorted in dispatch-priority order,
highest priority first.

```csharp
public IReadOnlyList<FlowStep> Prioritize(IReadOnlyList<FlowStep> readySteps, SchedulingContext context)
```

#### Parameters

`readySteps` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[FlowStep](Flowthru.Core.Graph.FlowStep.md)\>

Steps whose dependencies have all completed and that
    are eligible for immediate dispatch.

`context` [SchedulingContext](Flowthru.Core.Graph.Scheduling.SchedulingContext.md)

Read-only graph context available to inform ordering decisions.

#### Returns

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[FlowStep](Flowthru.Core.Graph.FlowStep.md)\>

The same steps in priority order. Must contain exactly the same elements
    as <code class="paramref">readySteps</code>.

