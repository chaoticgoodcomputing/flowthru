# <a id="Flowthru_Core_Graph_Scheduling_CriticalPathSchedulingStrategy"></a> Class CriticalPathSchedulingStrategy

Namespace: [Flowthru.Core.Graph.Scheduling](Flowthru.Core.Graph.Scheduling.md)  
Assembly: Flowthru.Core.dll  

Scheduling strategy that prioritises steps with the longest remaining critical path
(Highest Level First / HLF).

```csharp
public sealed class CriticalPathSchedulingStrategy : ISchedulingStrategy
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[CriticalPathSchedulingStrategy](Flowthru.Core.Graph.Scheduling.CriticalPathSchedulingStrategy.md)

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

<p>
When multiple steps are ready simultaneously, this strategy dispatches the step
with the greatest <xref href="Flowthru.Core.Graph.FlowStep.Height" data-throw-if-not-resolved="false"></xref> first — where height is the length
of the longest path from that step to any leaf in the DAG.
</p>
<p>
<strong>Rationale.</strong> Starting a high-height step unblocks more downstream
parallelism sooner, keeping worker threads saturated. Graham (1966) proved that any
list-scheduling algorithm using this priority order achieves a makespan within a
factor of <code>2 − 1/m</code> of optimal on <code>m</code> identical machines — the best
polynomial-time guarantee known for <code>P|prec|C_max</code>.
</p>
<p>
<strong>Prerequisites.</strong> <xref href="Flowthru.Core.Graph.FlowStep.Height" data-throw-if-not-resolved="false"></xref> values must have been
populated by <xref href="Flowthru.Core.Graph.DependencyAnalyzer.ComputeHeights(System.Collections.Generic.List%7bFlowthru.Core.Graph.FlowStep%7d)" data-throw-if-not-resolved="false"></xref> before this strategy
is used. Steps with <code>Height == -1</code> are treated as height 0.
</p>
<p>
Steps with equal height retain their relative arrival order (stable sort), so the
strategy degrades gracefully to FIFO when all ready steps share the same height.
</p>

## Methods

### <a id="Flowthru_Core_Graph_Scheduling_CriticalPathSchedulingStrategy_Prioritize_System_Collections_Generic_IReadOnlyList_Flowthru_Core_Graph_FlowStep__Flowthru_Core_Graph_Scheduling_SchedulingContext_"></a> Prioritize\(IReadOnlyList<FlowStep\>, SchedulingContext\)

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

