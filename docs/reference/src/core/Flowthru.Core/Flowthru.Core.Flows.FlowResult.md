# <a id="Flowthru_Core_Flows_FlowResult"></a> Class FlowResult

Namespace: [Flowthru.Core.Flows](Flowthru.Core.Flows.md)  
Assembly: Flowthru.Core.dll  

Represents the result of a Flow execution.

```csharp
public class FlowResult
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[FlowResult](Flowthru.Core.Flows.FlowResult.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

<p>
This class provides comprehensive execution information including success status,
timing, individual step results, and error details.
</p>
<p>
<strong>Usage Pattern:</strong>
</p>
<pre><code class="lang-csharp">var result = await flow.RunAsync();

if (result.Success)
{
    Console.WriteLine($"Flow completed in {result.ExecutionTime.TotalSeconds:F2}s");
    foreach (var stepResult in result.StepResults.Values)
    {
        Console.WriteLine($"  {stepResult.StepName}: {stepResult.ExecutionTime.TotalSeconds:F2}s");
    }
}
else
{
    Console.WriteLine($"Flow failed: {result.Exception?.Message}");
}</code></pre>

## Properties

### <a id="Flowthru_Core_Flows_FlowResult_Exception"></a> Exception

Exception that caused Flow failure, if any.

```csharp
public Exception? Exception { get; init; }
```

#### Property Value

 [Exception](https://learn.microsoft.com/dotnet/api/system.exception)?

#### Remarks

Null if Success is true. Contains the first exception that caused
Flow execution to halt if Success is false.

### <a id="Flowthru_Core_Flows_FlowResult_ExecutionTime"></a> ExecutionTime

Total execution time for the entire flow.

```csharp
public TimeSpan ExecutionTime { get; init; }
```

#### Property Value

 [TimeSpan](https://learn.microsoft.com/dotnet/api/system.timespan)

### <a id="Flowthru_Core_Flows_FlowResult_FlowName"></a> FlowName

The name of the Flow that was executed.

```csharp
public string? FlowName { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="Flowthru_Core_Flows_FlowResult_IsDryRun"></a> IsDryRun

Indicates whether this was a dry run (pre-flight checks only).

```csharp
public bool IsDryRun { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="Flowthru_Core_Flows_FlowResult_StepResults"></a> StepResults

Results for individual steps, keyed by step name.

```csharp
public Dictionary<string, StepResult> StepResults { get; init; }
```

#### Property Value

 [Dictionary](https://learn.microsoft.com/dotnet/api/system.collections.generic.dictionary\-2)<[string](https://learn.microsoft.com/dotnet/api/system.string), [StepResult](Flowthru.Core.Flows.StepResult.md)\>

#### Remarks

Dictionary keys are the step names as specified in the Flow definition.
Values contain execution details for each step.

### <a id="Flowthru_Core_Flows_FlowResult_Success"></a> Success

Indicates whether the Flow executed successfully.

```csharp
public bool Success { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="Flowthru_Core_Flows_FlowResult_CreateDryRunSuccess_System_TimeSpan_System_Int32_System_Int32_System_Int32_System_String_"></a> CreateDryRunSuccess\(TimeSpan, int, int, int, string?\)

Creates a successful dry run result.

```csharp
public static FlowResult CreateDryRunSuccess(TimeSpan preFlightDuration, int stepCount, int layerCount, int validatedInputCount, string? flowName = null)
```

#### Parameters

`preFlightDuration` [TimeSpan](https://learn.microsoft.com/dotnet/api/system.timespan)

Time spent on pre-flight checks

`stepCount` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Total number of steps in the flow

`layerCount` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Number of execution layers

`validatedInputCount` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Number of external inputs validated

`flowName` [string](https://learn.microsoft.com/dotnet/api/system.string)?

Name of the flow

#### Returns

 [FlowResult](Flowthru.Core.Flows.FlowResult.md)

A successful dry run result

### <a id="Flowthru_Core_Flows_FlowResult_CreateFailure_System_TimeSpan_System_Exception_System_Collections_Generic_Dictionary_System_String_Flowthru_Core_Flows_StepResult__System_String_"></a> CreateFailure\(TimeSpan, Exception, Dictionary<string, StepResult\>?, string?\)

Creates a failed Flow result.

```csharp
public static FlowResult CreateFailure(TimeSpan executionTime, Exception exception, Dictionary<string, StepResult>? stepResults = null, string? flowName = null)
```

#### Parameters

`executionTime` [TimeSpan](https://learn.microsoft.com/dotnet/api/system.timespan)

`exception` [Exception](https://learn.microsoft.com/dotnet/api/system.exception)

`stepResults` [Dictionary](https://learn.microsoft.com/dotnet/api/system.collections.generic.dictionary\-2)<[string](https://learn.microsoft.com/dotnet/api/system.string), [StepResult](Flowthru.Core.Flows.StepResult.md)\>?

`flowName` [string](https://learn.microsoft.com/dotnet/api/system.string)?

#### Returns

 [FlowResult](Flowthru.Core.Flows.FlowResult.md)

### <a id="Flowthru_Core_Flows_FlowResult_CreateSuccess_System_TimeSpan_System_Collections_Generic_Dictionary_System_String_Flowthru_Core_Flows_StepResult__System_String_"></a> CreateSuccess\(TimeSpan, Dictionary<string, StepResult\>, string?\)

Creates a successful Flow result.

```csharp
public static FlowResult CreateSuccess(TimeSpan executionTime, Dictionary<string, StepResult> stepResults, string? flowName = null)
```

#### Parameters

`executionTime` [TimeSpan](https://learn.microsoft.com/dotnet/api/system.timespan)

`stepResults` [Dictionary](https://learn.microsoft.com/dotnet/api/system.collections.generic.dictionary\-2)<[string](https://learn.microsoft.com/dotnet/api/system.string), [StepResult](Flowthru.Core.Flows.StepResult.md)\>

`flowName` [string](https://learn.microsoft.com/dotnet/api/system.string)?

#### Returns

 [FlowResult](Flowthru.Core.Flows.FlowResult.md)

