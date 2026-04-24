# <a id="Flowthru_Core_Graph_Scheduling_ISchedulingStrategy"></a> Interface ISchedulingStrategy

Namespace: [Flowthru.Core.Graph.Scheduling](Flowthru.Core.Graph.Scheduling.md)  
Assembly: Flowthru.Core.dll  

Defines the priority ordering for ready steps in the task-graph scheduler.

```csharp
public interface ISchedulingStrategy
```

## Remarks

<p>
When multiple steps are ready to dispatch simultaneously (all dependencies satisfied),
the scheduler delegates to an <xref href="Flowthru.Core.Graph.Scheduling.ISchedulingStrategy" data-throw-if-not-resolved="false"></xref> to determine which
step should be dispatched first. This affects which steps claim a worker slot when
the degree of parallelism is limited.
</p>
<p>
Implementations receive the currently ready steps and a <xref href="Flowthru.Core.Graph.Scheduling.SchedulingContext" data-throw-if-not-resolved="false"></xref>
containing graph structure and any available historical data, then return the steps in
dispatch-priority order (highest priority first).
</p>
<p>
The strategy is invoked each time the dispatch loop drains the ready queue, ensuring
newly-unblocked steps are ranked relative to any that were already waiting.
</p>

## Methods

### <a id="Flowthru_Core_Graph_Scheduling_ISchedulingStrategy_Prioritize_System_Collections_Generic_IReadOnlyList_Flowthru_Core_Graph_FlowStep__Flowthru_Core_Graph_Scheduling_SchedulingContext_"></a> Prioritize\(IReadOnlyList<FlowStep\>, SchedulingContext\)

Returns <code class="paramref">readySteps</code> sorted in dispatch-priority order,
highest priority first.

```csharp
IReadOnlyList<FlowStep> Prioritize(IReadOnlyList<FlowStep> readySteps, SchedulingContext context)
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

