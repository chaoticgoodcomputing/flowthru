# <a id="Flowthru_Core_Flows_StepResult"></a> Class StepResult

Namespace: [Flowthru.Core.Flows](Flowthru.Core.Flows.md)  
Assembly: Flowthru.Core.dll  

Represents the execution result of a single Flow step.

```csharp
public class StepResult
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[StepResult](Flowthru.Core.Flows.StepResult.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Properties

### <a id="Flowthru_Core_Flows_StepResult_Exception"></a> Exception

Exception that occurred during step execution, if any.

```csharp
public Exception? Exception { get; init; }
```

#### Property Value

 [Exception](https://learn.microsoft.com/dotnet/api/system.exception)?

#### Remarks

Null if Success is true. Contains the exception that caused
the step to fail if Success is false.

### <a id="Flowthru_Core_Flows_StepResult_ExecutionTime"></a> ExecutionTime

Execution time for this specific step.

```csharp
public TimeSpan ExecutionTime { get; init; }
```

#### Property Value

 [TimeSpan](https://learn.microsoft.com/dotnet/api/system.timespan)

### <a id="Flowthru_Core_Flows_StepResult_StepName"></a> StepName

The name of the step that was executed.

```csharp
public required string StepName { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="Flowthru_Core_Flows_StepResult_Success"></a> Success

Indicates whether the step executed successfully.

```csharp
public bool Success { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="Flowthru_Core_Flows_StepResult_CreateFailure_System_String_System_TimeSpan_System_Exception_"></a> CreateFailure\(string, TimeSpan, Exception\)

Creates a failed step result.

```csharp
public static StepResult CreateFailure(string stepName, TimeSpan executionTime, Exception exception)
```

#### Parameters

`stepName` [string](https://learn.microsoft.com/dotnet/api/system.string)

`executionTime` [TimeSpan](https://learn.microsoft.com/dotnet/api/system.timespan)

`exception` [Exception](https://learn.microsoft.com/dotnet/api/system.exception)

#### Returns

 [StepResult](Flowthru.Core.Flows.StepResult.md)

### <a id="Flowthru_Core_Flows_StepResult_CreateSuccess_System_String_System_TimeSpan_"></a> CreateSuccess\(string, TimeSpan\)

Creates a successful step result.

```csharp
public static StepResult CreateSuccess(string stepName, TimeSpan executionTime)
```

#### Parameters

`stepName` [string](https://learn.microsoft.com/dotnet/api/system.string)

`executionTime` [TimeSpan](https://learn.microsoft.com/dotnet/api/system.timespan)

#### Returns

 [StepResult](Flowthru.Core.Flows.StepResult.md)

