# <a id="Flowthru_Core_Cli_FlowthruCli"></a> Class FlowthruCli

Namespace: [Flowthru.Core.Cli](Flowthru.Core.Cli.md)  
Assembly: Flowthru.Core.dll  

Command-line interface wrapper for IFlowthruService.

```csharp
public sealed class FlowthruCli
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[FlowthruCli](Flowthru.Core.Cli.FlowthruCli.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

<p>
FlowthruCli provides a thin CLI layer over the core IFlowthruService.
It handles:
- Command-line argument parsing
- Help/version display
- Result formatting
- Exit code generation
</p>
<p>
The CLI delegates all business logic to IFlowthruService, making the
service layer testable and reusable in non-CLI scenarios.
</p>

## Constructors

### <a id="Flowthru_Core_Cli_FlowthruCli__ctor_Flowthru_Core_Services_IFlowthruService_Microsoft_Extensions_Logging_ILogger_Flowthru_Core_Cli_FlowthruCli__System_IO_TextWriter_"></a> FlowthruCli\(IFlowthruService, ILogger<FlowthruCli\>, TextWriter?\)

Initializes a new CLI instance.

```csharp
public FlowthruCli(IFlowthruService service, ILogger<FlowthruCli> logger, TextWriter? output = null)
```

#### Parameters

`service` [IFlowthruService](Flowthru.Core.Services.IFlowthruService.md)

Flowthru service

`logger` [ILogger](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging.ilogger\-1)<[FlowthruCli](Flowthru.Core.Cli.FlowthruCli.md)\>

Logger instance

`output` [TextWriter](https://learn.microsoft.com/dotnet/api/system.io.textwriter)?

Output writer (defaults to Console.Out)

## Methods

### <a id="Flowthru_Core_Cli_FlowthruCli_RunAsync_System_String___System_Threading_CancellationToken_"></a> RunAsync\(string\[\], CancellationToken\)

Runs the CLI with the specified arguments.

```csharp
public Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
```

#### Parameters

`args` [string](https://learn.microsoft.com/dotnet/api/system.string)\[\]

Command-line arguments

`cancellationToken` [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken)

Cancellation token

#### Returns

 [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<[int](https://learn.microsoft.com/dotnet/api/system.int32)\>

Exit code (0 for success, non-zero for errors)

### <a id="Flowthru_Core_Cli_FlowthruCli_RunStandaloneAsync_System_String___System_Action_Microsoft_Extensions_DependencyInjection_IServiceCollection__System_Threading_CancellationToken_"></a> RunStandaloneAsync\(string\[\], Action<IServiceCollection\>, CancellationToken\)

Creates and runs a standalone Flowthru CLI application with automatic service provider lifecycle management.

```csharp
public static Task<int> RunStandaloneAsync(string[] args, Action<IServiceCollection> configure, CancellationToken cancellationToken = default)
```

#### Parameters

`args` [string](https://learn.microsoft.com/dotnet/api/system.string)\[\]

Command-line arguments

`configure` [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<[IServiceCollection](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.iservicecollection)\>

Configuration callback to register pipelines and services

`cancellationToken` [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken)

Cancellation token

#### Returns

 [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<[int](https://learn.microsoft.com/dotnet/api/system.int32)\>

Exit code (0 for success, non-zero for errors)

#### Remarks

<p>
This is the recommended entry point for standalone console applications using Flowthru.Core.
It manages the ServiceProvider lifecycle automatically, ensuring proper disposal of
logging providers and other resources so the process exits cleanly after pipeline completion.
</p>
<p>
For applications that integrate Flowthru into an existing DI container (e.g., ASP.NET Core),
use the standard constructor and let the host application manage the ServiceProvider lifecycle.
</p>

