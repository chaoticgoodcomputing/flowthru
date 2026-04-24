# <a id="Flowthru_Core_Results_RuntimeErrorReport"></a> Class RuntimeErrorReport

Namespace: [Flowthru.Core.Results](Flowthru.Core.Results.md)  
Assembly: Flowthru.Core.dll  

Captures the context of a runtime pipeline failure for error reporting.

```csharp
public class RuntimeErrorReport
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[RuntimeErrorReport](Flowthru.Core.Results.RuntimeErrorReport.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Properties

### <a id="Flowthru_Core_Results_RuntimeErrorReport_Classification"></a> Classification

Heuristic classification of the failure.

```csharp
public required ErrorClassification Classification { get; init; }
```

#### Property Value

 [ErrorClassification](Flowthru.Core.Results.ErrorClassification.md)

### <a id="Flowthru_Core_Results_RuntimeErrorReport_CompletedSteps"></a> CompletedSteps

Names of steps that completed successfully before the failure.

```csharp
public IReadOnlyList<string> CompletedSteps { get; init; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="Flowthru_Core_Results_RuntimeErrorReport_Exception"></a> Exception

The exception that caused the failure.

```csharp
public required Exception Exception { get; init; }
```

#### Property Value

 [Exception](https://learn.microsoft.com/dotnet/api/system.exception)

### <a id="Flowthru_Core_Results_RuntimeErrorReport_FailedStepName"></a> FailedStepName

Name of the step that failed, if the failure is step-scoped.

```csharp
public string? FailedStepName { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="Flowthru_Core_Results_RuntimeErrorReport_FlowName"></a> FlowName

Name of the flow that failed.

```csharp
public string? FlowName { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="Flowthru_Core_Results_RuntimeErrorReport_FlowthruVersion"></a> FlowthruVersion

The Flowthru library version that produced this report.

```csharp
public required string FlowthruVersion { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="Flowthru_Core_Results_RuntimeErrorReport_OperatingSystem"></a> OperatingSystem

The operating system description.

```csharp
public required string OperatingSystem { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="Flowthru_Core_Results_RuntimeErrorReport_RuntimeVersion"></a> RuntimeVersion

The .NET runtime version (e.g. "8.0.5").

```csharp
public required string RuntimeVersion { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="Flowthru_Core_Results_RuntimeErrorReport_FromFlowResult_Flowthru_Core_Flows_FlowResult_"></a> FromFlowResult\(FlowResult\)

Creates a <xref href="Flowthru.Core.Results.RuntimeErrorReport" data-throw-if-not-resolved="false"></xref> from a failed <xref href="Flowthru.Core.Flows.FlowResult" data-throw-if-not-resolved="false"></xref>.

```csharp
public static RuntimeErrorReport FromFlowResult(FlowResult result)
```

#### Parameters

`result` [FlowResult](Flowthru.Core.Flows.FlowResult.md)

#### Returns

 [RuntimeErrorReport](Flowthru.Core.Results.RuntimeErrorReport.md)

