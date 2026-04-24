# <a id="Flowthru_Core_Graph_Scheduling"></a> Namespace Flowthru.Core.Graph.Scheduling

### Classes

 [CriticalPathSchedulingStrategy](Flowthru.Core.Graph.Scheduling.CriticalPathSchedulingStrategy.md)

Scheduling strategy that prioritises steps with the longest remaining critical path
(Highest Level First / HLF).

 [FifoSchedulingStrategy](Flowthru.Core.Graph.Scheduling.FifoSchedulingStrategy.md)

Scheduling strategy that preserves arrival order (first-in, first-out).

 [SchedulingContext](Flowthru.Core.Graph.Scheduling.SchedulingContext.md)

Read-only graph context passed to <xref href="Flowthru.Core.Graph.Scheduling.ISchedulingStrategy.Prioritize(System.Collections.Generic.IReadOnlyList%7bFlowthru.Core.Graph.FlowStep%7d%2cFlowthru.Core.Graph.Scheduling.SchedulingContext)" data-throw-if-not-resolved="false"></xref> on
each dispatch cycle.

### Interfaces

 [ISchedulingStrategy](Flowthru.Core.Graph.Scheduling.ISchedulingStrategy.md)

Defines the priority ordering for ready steps in the task-graph scheduler.

