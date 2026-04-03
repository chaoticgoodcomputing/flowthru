# <a id="Flowthru_Results_IFlowResultFormatter"></a> Interface IFlowResultFormatter

Namespace: [Flowthru.Results](Flowthru.Results.md)  
Assembly: Flowthru.Core.dll  

Interface for formatting flow execution results.

```csharp
public interface IFlowResultFormatter
```

## Remarks

<p>
Result formatters transform a FlowResult into human-readable or
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

### <a id="Flowthru_Results_IFlowResultFormatter_Format_Flowthru_Flows_FlowResult_Microsoft_Extensions_Logging_ILogger_"></a> Format\(FlowResult, ILogger\)

Formats and outputs the flow result.

```csharp
void Format(FlowResult result, ILogger logger)
```

#### Parameters

`result` [FlowResult](Flowthru.Flows.FlowResult.md)

The flow execution result

`logger` [ILogger](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging.ilogger)

The logger to write output to

