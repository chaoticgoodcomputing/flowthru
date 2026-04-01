# <a id="Flowthru_Results_ConsoleResultFormatter"></a> Class ConsoleResultFormatter

Namespace: [Flowthru.Results](Flowthru.Results.md)  
Assembly: Flowthru.Core.dll  

Formats pipeline results as human-readable console output.

```csharp
public class ConsoleResultFormatter : IPipelineResultFormatter
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ConsoleResultFormatter](Flowthru.Results.ConsoleResultFormatter.md)

#### Implements

[IPipelineResultFormatter](Flowthru.Results.IPipelineResultFormatter.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

This is the default formatter used by the CLI.
Produces colorful, detailed output suitable for interactive terminal sessions.

## Methods

### <a id="Flowthru_Results_ConsoleResultFormatter_Format_Flowthru_Pipelines_PipelineResult_Microsoft_Extensions_Logging_ILogger_"></a> Format\(PipelineResult, ILogger\)

Formats and outputs the pipeline result.

```csharp
public void Format(PipelineResult result, ILogger logger)
```

#### Parameters

`result` [PipelineResult](Flowthru.Pipelines.PipelineResult.md)

The pipeline execution result

`logger` [ILogger](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging.ilogger)

The logger to write output to

