# <a id="Flowthru_Core_Services_FlowthruServiceBuilder"></a> Class FlowthruServiceBuilder

Namespace: [Flowthru.Core.Services](Flowthru.Core.Services.md)  
Assembly: Flowthru.Core.dll  

Fluent builder for configuring Flowthru service registration.

```csharp
public sealed class FlowthruServiceBuilder : IFlowthruBuilder
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[FlowthruServiceBuilder](Flowthru.Core.Services.FlowthruServiceBuilder.md)

#### Implements

[IFlowthruBuilder](Flowthru.Core.Services.IFlowthruBuilder.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

<p>
This builder configures the service layer without CLI coupling.
Use it to register catalogs, flows, and optional features.
</p>
<p>
<strong>Basic Usage:</strong>
<pre><code class="lang-csharp">services.AddFlowthru(configuration, flowthru =&gt;
{
    flowthru.RegisterCatalog(_ =&gt; new MyCatalog(dataPath));
    flowthru.RegisterFlow("my_flow", MyFlow.Create);
});</code></pre>
</p>

## Properties

### <a id="Flowthru_Core_Services_FlowthruServiceBuilder_Configuration"></a> Configuration

The application configuration passed to <code>AddFlowthru</code>.
Available to extensions that need to read config values at registration time.

```csharp
public IConfiguration Configuration { get; }
```

#### Property Value

 [IConfiguration](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration.iconfiguration)

### <a id="Flowthru_Core_Services_FlowthruServiceBuilder_Services"></a> Services

The underlying DI service collection.
Use this to register services, options, and validators.

```csharp
public IServiceCollection Services { get; }
```

#### Property Value

 [IServiceCollection](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.iservicecollection)

## Methods

### <a id="Flowthru_Core_Services_FlowthruServiceBuilder_ConfigureExecution_System_Action_Flowthru_Core_Flows_ExecutionOptions__"></a> ConfigureExecution\(Action<ExecutionOptions\>\)

Configures default execution behaviour for all pipelines run through this service.

```csharp
public IFlowthruBuilder ConfigureExecution(Action<ExecutionOptions> configure)
```

#### Parameters

`configure` [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<[ExecutionOptions](Flowthru.Core.Flows.ExecutionOptions.md)\>

#### Returns

 [IFlowthruBuilder](Flowthru.Core.Services.IFlowthruBuilder.md)

#### Examples

<pre><code class="lang-csharp">services.AddFlowthru(flowthru =&gt;
{
    flowthru.ConfigureExecution(opts =&gt;
    {
        opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
    });
});</code></pre>

#### Remarks

Settings applied here act as the service-level default. They are overridden by any
value explicitly supplied in the <xref href="Flowthru.Core.Flows.ExecutionOptions" data-throw-if-not-resolved="false"></xref> passed to
<xref href="Flowthru.Core.Services.IFlowthruService.ExecuteFlowAsync(Flowthru.Core.Flows.ExecutionOptions%2cSystem.Boolean%2cSystem.Threading.CancellationToken)" data-throw-if-not-resolved="false"></xref> (e.g. from the CLI <code>--parallelism</code>
flag). Properties left <code>null</code> or at their default in the per-call options fall
back to these values.

### <a id="Flowthru_Core_Services_FlowthruServiceBuilder_ConfigureMetadata_System_Action_Flowthru_Core_Meta_FlowthruMetadataBuilder__"></a> ConfigureMetadata\(Action<FlowthruMetadataBuilder\>\)

Configures metadata export.

```csharp
public IFlowthruBuilder ConfigureMetadata(Action<FlowthruMetadataBuilder> configure)
```

#### Parameters

`configure` [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<[FlowthruMetadataBuilder](Flowthru.Core.Meta.FlowthruMetadataBuilder.md)\>

Action to configure the metadata builder

#### Returns

 [IFlowthruBuilder](Flowthru.Core.Services.IFlowthruBuilder.md)

This builder for method chaining

#### Remarks

<p>
Metadata export is optional. If not configured, flows will execute
without generating DAG diagrams or metadata files.
</p>

### <a id="Flowthru_Core_Services_FlowthruServiceBuilder_RegisterCatalog__1"></a> RegisterCatalog<TCatalog\>\(\)

Registers a catalog type with constructor injection.

```csharp
public IFlowthruBuilder RegisterCatalog<TCatalog>() where TCatalog : CatalogAbstract
```

#### Returns

 [IFlowthruBuilder](Flowthru.Core.Services.IFlowthruBuilder.md)

This builder for method chaining

#### Type Parameters

`TCatalog` 

The catalog type

#### Remarks

The catalog will be resolved from the DI container, allowing constructor
parameter injection (e.g., IConfiguration, IOptions).

### <a id="Flowthru_Core_Services_FlowthruServiceBuilder_RegisterCatalog_Flowthru_Core_Data_CatalogAbstract_"></a> RegisterCatalog\(CatalogAbstract\)

Registers a catalog instance directly.

```csharp
public IFlowthruBuilder RegisterCatalog(CatalogAbstract catalog)
```

#### Parameters

`catalog` [CatalogAbstract](Flowthru.Core.Data.CatalogAbstract.md)

The catalog instance

#### Returns

 [IFlowthruBuilder](Flowthru.Core.Services.IFlowthruBuilder.md)

This builder for method chaining

#### Remarks

Use this when the catalog doesn't require dependency injection.

### <a id="Flowthru_Core_Services_FlowthruServiceBuilder_RegisterCatalog__1_System_Func_System_IServiceProvider___0__"></a> RegisterCatalog<TCatalog\>\(Func<IServiceProvider, TCatalog\>\)

Registers a catalog factory that receives the service provider.

```csharp
public IFlowthruBuilder RegisterCatalog<TCatalog>(Func<IServiceProvider, TCatalog> catalogFactory) where TCatalog : CatalogAbstract
```

#### Parameters

`catalogFactory` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[IServiceProvider](https://learn.microsoft.com/dotnet/api/system.iserviceprovider), TCatalog\>

Factory function to create the catalog

#### Returns

 [IFlowthruBuilder](Flowthru.Core.Services.IFlowthruBuilder.md)

This builder for method chaining

#### Type Parameters

`TCatalog` 

The concrete catalog type

#### Remarks

Use this when the catalog needs to resolve services during construction,
or when construction requires parameters unavailable at the call site.

### <a id="Flowthru_Core_Services_FlowthruServiceBuilder_RegisterCatalogs_System_Collections_Generic_IEnumerable_Flowthru_Core_Data_CatalogAbstract__"></a> RegisterCatalogs\(IEnumerable<CatalogAbstract\>\)

Registers a collection of pre-built catalog instances produced by iterative or dynamic
construction — the fan-out pattern where N identical-shaped catalogs differ only by
their construction parameters (e.g., one catalog per US state).

```csharp
public IFlowthruBuilder RegisterCatalogs(IEnumerable<CatalogAbstract> catalogs)
```

#### Parameters

`catalogs` [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<[CatalogAbstract](Flowthru.Core.Data.CatalogAbstract.md)\>

The catalog instances to register

#### Returns

 [IFlowthruBuilder](Flowthru.Core.Services.IFlowthruBuilder.md)

This builder for method chaining

#### Remarks

All registered catalogs will receive DI service injection and appear in
<xref href="Flowthru.Core.Services.IFlowthruService.Catalogs" data-throw-if-not-resolved="false"></xref>. Use with <xref href="Flowthru.Core.Services.FlowthruServiceBuilder.RegisterFlows(System.Func%7bSystem.IServiceProvider%2cSystem.Collections.Generic.Dictionary%7bSystem.String%2cFlowthru.Core.Flows.Flow%7d%7d)" data-throw-if-not-resolved="false"></xref> to
wire per-catalog flows in a loop.

### <a id="Flowthru_Core_Services_FlowthruServiceBuilder_RegisterCatalogs_System_Func_System_IServiceProvider_System_Collections_Generic_IEnumerable_Flowthru_Core_Data_CatalogAbstract___"></a> RegisterCatalogs\(Func<IServiceProvider, IEnumerable<CatalogAbstract\>\>\)

Registers catalogs produced by a factory that receives the service provider —
useful when catalog construction itself requires DI resolution.

```csharp
public IFlowthruBuilder RegisterCatalogs(Func<IServiceProvider, IEnumerable<CatalogAbstract>> catalogsFactory)
```

#### Parameters

`catalogsFactory` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[IServiceProvider](https://learn.microsoft.com/dotnet/api/system.iserviceprovider), [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<[CatalogAbstract](Flowthru.Core.Data.CatalogAbstract.md)\>\>

Factory that returns the catalog collection

#### Returns

 [IFlowthruBuilder](Flowthru.Core.Services.IFlowthruBuilder.md)

This builder for method chaining

### <a id="Flowthru_Core_Services_FlowthruServiceBuilder_RegisterFlow_System_String_System_Delegate_"></a> RegisterFlow\(string, Delegate\)

Registers a Flow by inspecting the delegate's parameter types at runtime.
Parameters that extend <xref href="Flowthru.Core.Data.CatalogAbstract" data-throw-if-not-resolved="false"></xref> are resolved from DI as catalogs.
All other parameters are resolved from DI as services.

```csharp
public IFlowthruBuilder RegisterFlow(string label, Delegate flow)
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique Flow name

`flow` [Delegate](https://learn.microsoft.com/dotnet/api/system.delegate)

A method group or delegate whose parameters are catalogs or DI-registered services.

#### Returns

 [IFlowthruBuilder](Flowthru.Core.Services.IFlowthruBuilder.md)

This builder for method chaining

### <a id="Flowthru_Core_Services_FlowthruServiceBuilder_RegisterFlows_System_Func_System_IServiceProvider_System_Collections_Generic_Dictionary_System_String_Flowthru_Core_Flows_Flow___"></a> RegisterFlows\(Func<IServiceProvider, Dictionary<string, Flow\>\>\)

Escape-hatch for registering flows via a full-access service provider factory.

```csharp
public IFlowthruBuilder RegisterFlows(Func<IServiceProvider, Dictionary<string, Flow>> flowFactory)
```

#### Parameters

`flowFactory` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[IServiceProvider](https://learn.microsoft.com/dotnet/api/system.iserviceprovider), [Dictionary](https://learn.microsoft.com/dotnet/api/system.collections.generic.dictionary\-2)<[string](https://learn.microsoft.com/dotnet/api/system.string), [Flow](Flowthru.Core.Flows.Flow.md)\>\>

Factory function that receives the service provider and returns the Flow dictionary

#### Returns

 [IFlowthruBuilder](Flowthru.Core.Services.IFlowthruBuilder.md)

This builder for method chaining

#### Remarks

Prefer RegisterFlow(string, Delegate, string?) for standard Flow registration.
Use this only when you need full service provider access during Flow construction.

### <a id="Flowthru_Core_Services_FlowthruServiceBuilder_UseStorageStrategy__1"></a> UseStorageStrategy<TStrategy\>\(\)

Registers a storage entry factory (for environment-specific entries).

```csharp
public IFlowthruBuilder UseStorageStrategy<TStrategy>() where TStrategy : class, IStorageEntryFactory
```

#### Returns

 [IFlowthruBuilder](Flowthru.Core.Services.IFlowthruBuilder.md)

This builder for method chaining

#### Type Parameters

`TStrategy` 

The storage strategy type

#### Remarks

<p>
Storage strategies enable environment-specific catalog entries:
</p>
<pre><code class="lang-csharp">if (env.IsDevelopment())
{
    flowthru.UseStorageStrategy&lt;CsvStorageEntryFactory&gt;();
}
else if (env.IsProduction())
{
    flowthru.UseStorageStrategy&lt;DatabaseStorageEntryFactory&gt;();
}
else if (env.IsTest())
{
    flowthru.UseStorageStrategy&lt;MemoryStorageEntryFactory&gt;();
}</code></pre>

### <a id="Flowthru_Core_Services_FlowthruServiceBuilder_WithDescription_System_String_"></a> WithDescription\(string\)

Adds a description to the most recently registered flow.

```csharp
public IFlowthruBuilder WithDescription(string description)
```

#### Parameters

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Human-readable description of what the Flow does

#### Returns

 [IFlowthruBuilder](Flowthru.Core.Services.IFlowthruBuilder.md)

This builder for method chaining

