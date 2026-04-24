# <a id="Flowthru_Core_Graph_Scheduling_SchedulingContext"></a> Class SchedulingContext

Namespace: [Flowthru.Core.Graph.Scheduling](Flowthru.Core.Graph.Scheduling.md)  
Assembly: Flowthru.Core.dll  

Read-only graph context passed to <xref href="Flowthru.Core.Graph.Scheduling.ISchedulingStrategy.Prioritize(System.Collections.Generic.IReadOnlyList%7bFlowthru.Core.Graph.FlowStep%7d%2cFlowthru.Core.Graph.Scheduling.SchedulingContext)" data-throw-if-not-resolved="false"></xref> on
each dispatch cycle.

```csharp
public sealed record SchedulingContext : IEquatable<SchedulingContext>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[SchedulingContext](Flowthru.Core.Graph.Scheduling.SchedulingContext.md)

#### Implements

[IEquatable<SchedulingContext\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

Carries structural information about the DAG that strategies may use to make ordering
decisions. Designed to be extended: future fields (e.g., historical step durations)
can be added here without changing the <xref href="Flowthru.Core.Graph.Scheduling.ISchedulingStrategy" data-throw-if-not-resolved="false"></xref> signature.

## Constructors

### <a id="Flowthru_Core_Graph_Scheduling_SchedulingContext__ctor_System_Collections_Generic_IReadOnlyDictionary_Flowthru_Core_Graph_FlowStep_System_Collections_Generic_IReadOnlyList_Flowthru_Core_Graph_FlowStep___"></a> SchedulingContext\(IReadOnlyDictionary<FlowStep, IReadOnlyList<FlowStep\>\>\)

Read-only graph context passed to <xref href="Flowthru.Core.Graph.Scheduling.ISchedulingStrategy.Prioritize(System.Collections.Generic.IReadOnlyList%7bFlowthru.Core.Graph.FlowStep%7d%2cFlowthru.Core.Graph.Scheduling.SchedulingContext)" data-throw-if-not-resolved="false"></xref> on
each dispatch cycle.

```csharp
public SchedulingContext(IReadOnlyDictionary<FlowStep, IReadOnlyList<FlowStep>> Dependents)
```

#### Parameters

`Dependents` [IReadOnlyDictionary](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlydictionary\-2)<[FlowStep](Flowthru.Core.Graph.FlowStep.md), [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[FlowStep](Flowthru.Core.Graph.FlowStep.md)\>\>

Reverse adjacency map: for each step, the list of steps that depend on it.
A step with an empty list is a sink (no descendants).

#### Remarks

Carries structural information about the DAG that strategies may use to make ordering
decisions. Designed to be extended: future fields (e.g., historical step durations)
can be added here without changing the <xref href="Flowthru.Core.Graph.Scheduling.ISchedulingStrategy" data-throw-if-not-resolved="false"></xref> signature.

## Properties

### <a id="Flowthru_Core_Graph_Scheduling_SchedulingContext_Dependents"></a> Dependents

Reverse adjacency map: for each step, the list of steps that depend on it.
A step with an empty list is a sink (no descendants).

```csharp
public IReadOnlyDictionary<FlowStep, IReadOnlyList<FlowStep>> Dependents { get; init; }
```

#### Property Value

 [IReadOnlyDictionary](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlydictionary\-2)<[FlowStep](Flowthru.Core.Graph.FlowStep.md), [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[FlowStep](Flowthru.Core.Graph.FlowStep.md)\>\>

