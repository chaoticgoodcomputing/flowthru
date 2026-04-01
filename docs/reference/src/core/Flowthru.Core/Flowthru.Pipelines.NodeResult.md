# <a id="Flowthru_Pipelines_NodeResult"></a> Class NodeResult

Namespace: [Flowthru.Pipelines](Flowthru.Pipelines.md)  
Assembly: Flowthru.Core.dll  

Represents the execution result of a single pipeline node.

```csharp
public class NodeResult
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[NodeResult](Flowthru.Pipelines.NodeResult.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Properties

### <a id="Flowthru_Pipelines_NodeResult_Exception"></a> Exception

Exception that occurred during node execution, if any.

```csharp
public Exception? Exception { get; init; }
```

#### Property Value

 [Exception](https://learn.microsoft.com/dotnet/api/system.exception)?

#### Remarks

Null if Success is true. Contains the exception that caused
the node to fail if Success is false.

### <a id="Flowthru_Pipelines_NodeResult_ExecutionTime"></a> ExecutionTime

Execution time for this specific node.

```csharp
public TimeSpan ExecutionTime { get; init; }
```

#### Property Value

 [TimeSpan](https://learn.microsoft.com/dotnet/api/system.timespan)

### <a id="Flowthru_Pipelines_NodeResult_InputCount"></a> InputCount

Number of input items processed by this node.

```csharp
public int InputCount { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Remarks

For multi-input nodes, this represents the total count across
all input catalog entries.

### <a id="Flowthru_Pipelines_NodeResult_NodeName"></a> NodeName

The name of the node that was executed.

```csharp
public required string NodeName { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="Flowthru_Pipelines_NodeResult_OutputCount"></a> OutputCount

Number of output items produced by this node.

```csharp
public int OutputCount { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Remarks

For multi-output nodes, this represents the total count across
all output catalog entries.

### <a id="Flowthru_Pipelines_NodeResult_Success"></a> Success

Indicates whether the node executed successfully.

```csharp
public bool Success { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="Flowthru_Pipelines_NodeResult_CreateFailure_System_String_System_TimeSpan_System_Exception_System_Int32_"></a> CreateFailure\(string, TimeSpan, Exception, int\)

Creates a failed node result.

```csharp
public static NodeResult CreateFailure(string nodeName, TimeSpan executionTime, Exception exception, int inputCount = 0)
```

#### Parameters

`nodeName` [string](https://learn.microsoft.com/dotnet/api/system.string)

`executionTime` [TimeSpan](https://learn.microsoft.com/dotnet/api/system.timespan)

`exception` [Exception](https://learn.microsoft.com/dotnet/api/system.exception)

`inputCount` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [NodeResult](Flowthru.Pipelines.NodeResult.md)

### <a id="Flowthru_Pipelines_NodeResult_CreateSuccess_System_String_System_TimeSpan_System_Int32_System_Int32_"></a> CreateSuccess\(string, TimeSpan, int, int\)

Creates a successful node result.

```csharp
public static NodeResult CreateSuccess(string nodeName, TimeSpan executionTime, int inputCount, int outputCount)
```

#### Parameters

`nodeName` [string](https://learn.microsoft.com/dotnet/api/system.string)

`executionTime` [TimeSpan](https://learn.microsoft.com/dotnet/api/system.timespan)

`inputCount` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`outputCount` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [NodeResult](Flowthru.Pipelines.NodeResult.md)

