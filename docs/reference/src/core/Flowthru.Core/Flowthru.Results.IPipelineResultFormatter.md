# <a id="Flowthru_Results_IPipelineResultFormatter"></a> Interface IPipelineResultFormatter

Namespace: [Flowthru.Results](Flowthru.Results.md)  
Assembly: Flowthru.Core.dll  

Interface for formatting pipeline execution results.

```csharp
public interface IPipelineResultFormatter
```

## Remarks

<p>
Result formatters transform a PipelineResult into human-readable or
machine-readable output via logging.
</p>
<p>
Built-in formatters:
- <xref href="Flowthru.Results.ConsoleResultFormatter" data-throw-if-not-resolved="false"></xref> - Human-readable console output (default)
</p>
<p>
Future formatters: JSON, Markdown, compact CI/CD format.
</p>

## Methods

### <a id="Flowthru_Results_IPipelineResultFormatter_Format_Flowthru_Pipelines_PipelineResult_Microsoft_Extensions_Logging_ILogger_"></a> Format\(PipelineResult, ILogger\)

Formats and outputs the pipeline result.

```csharp
void Format(PipelineResult result, ILogger logger)
```

#### Parameters

`result` [PipelineResult](Flowthru.Pipelines.PipelineResult.md)

The pipeline execution result

`logger` [ILogger](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging.ilogger)

The logger to write output to

