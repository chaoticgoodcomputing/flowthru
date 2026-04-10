# <a id="Flowthru_Core_Flows_ExecutionOptions"></a> Class ExecutionOptions

Namespace: [Flowthru.Core.Flows](Flowthru.Core.Flows.md)  
Assembly: Flowthru.Core.dll  

Configuration options for pipeline execution.

```csharp
public class ExecutionOptions
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ExecutionOptions](Flowthru.Core.Flows.ExecutionOptions.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

Controls how pipelines are executed and how results are presented.

## Properties

### <a id="Flowthru_Core_Flows_ExecutionOptions_DryRun"></a> DryRun

Whether to perform a dry run, and at what validation depth.

```csharp
public DryRunOption DryRun { get; set; }
```

#### Property Value

 [DryRunOption](Flowthru.Core.Flows.DryRunOption.md)

#### Remarks

Assign <code>true</code> to perform all pre-flight operations (structure validation,
validation hooks, and external data source inspection) without executing nodes.
Assign a <xref href="Flowthru.Core.Flows.ValidationDepth" data-throw-if-not-resolved="false"></xref> value to control how deeply the pre-flight
checks run — for example, <xref href="Flowthru.Core.Flows.ValidationDepth.StructureOnly" data-throw-if-not-resolved="false"></xref> validates
the pipeline graph and runs extension hooks without probing any data sources.
Assign <code>false</code> (default) to run normally without a dry-run stop.

### <a id="Flowthru_Core_Flows_ExecutionOptions_EnableParallelExecution"></a> EnableParallelExecution

Whether to enable parallel execution of nodes within the same layer.

```csharp
public bool EnableParallelExecution { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Remarks

Phase 2 feature - currently not implemented.
When true, nodes in the same execution layer run concurrently.

### <a id="Flowthru_Core_Flows_ExecutionOptions_ResultFormatter"></a> ResultFormatter

The result formatter to use for displaying execution results.

```csharp
public IFlowResultFormatter? ResultFormatter { get; set; }
```

#### Property Value

 [IFlowResultFormatter](Flowthru.Core.Results.IFlowResultFormatter.md)?

#### Remarks

Defaults to ConsoleResultFormatter if not specified.

### <a id="Flowthru_Core_Flows_ExecutionOptions_SliceStrategy"></a> SliceStrategy

Optional slicing strategy to apply when executing pipelines.

```csharp
public FlowSliceStrategy? SliceStrategy { get; set; }
```

#### Property Value

 [FlowSliceStrategy](Flowthru.Core.Graph.FlowSliceStrategy.md)?

#### Remarks

When provided, only nodes matching the slice strategy will be executed.
Used when slicing flags are provided without a specific pipeline name.

### <a id="Flowthru_Core_Flows_ExecutionOptions_StopOnFirstError"></a> StopOnFirstError

Whether to stop execution on the first node failure.

```csharp
public bool StopOnFirstError { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Remarks

When true (default), pipeline execution stops immediately when a node fails.
When false, execution continues to independent nodes (Phase 2 feature).

