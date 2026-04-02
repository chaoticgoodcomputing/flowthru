# <a id="Flowthru_Services_FlowthruServiceBuilder"></a> Class FlowthruServiceBuilder

Namespace: [Flowthru.Services](Flowthru.Services.md)  
Assembly: Flowthru.Core.dll  

Fluent builder for configuring Flowthru service registration.

```csharp
public sealed class FlowthruServiceBuilder
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[FlowthruServiceBuilder](Flowthru.Services.FlowthruServiceBuilder.md)

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
<pre><code class="lang-csharp">services.AddFlowthru(flowthru =&gt;
{
    flowthru.RegisterCatalog(_ =&gt; new MyCatalog(dataPath));
    flowthru.RegisterFlow("my_flow", MyFlow.Create);
});</code></pre>
</p>

## Methods

### <a id="Flowthru_Services_FlowthruServiceBuilder_ConfigureMetadata_System_Action_Flowthru_Meta_FlowthruMetadataBuilder__"></a> ConfigureMetadata\(Action<FlowthruMetadataBuilder\>\)

Configures metadata export.

```csharp
public FlowthruServiceBuilder ConfigureMetadata(Action<FlowthruMetadataBuilder> configure)
```

#### Parameters

`configure` [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<[FlowthruMetadataBuilder](Flowthru.Meta.FlowthruMetadataBuilder.md)\>

Action to configure the metadata builder

#### Returns

 [FlowthruServiceBuilder](Flowthru.Services.FlowthruServiceBuilder.md)

This builder for method chaining

#### Remarks

<p>
Metadata export is optional. If not configured, flows will execute
without generating DAG diagrams or metadata files.
</p>
<p>
<strong>Example:</strong>
<pre><code class="lang-csharp">flowthru.ConfigureMetadata(meta =&gt;
{
    meta.WithOutputDirectory("metadata")
        .AddProvider&lt;JsonMetadataProvider, JsonMetadataProviderBuilder&gt;()
        .AddProvider&lt;MermaidMetadataProvider, MermaidMetadataProviderBuilder&gt;();
});</code></pre>
</p>

### <a id="Flowthru_Services_FlowthruServiceBuilder_RegisterCatalog__1"></a> RegisterCatalog<TCatalog\>\(\)

Registers a catalog type with constructor injection.

```csharp
public FlowthruServiceBuilder RegisterCatalog<TCatalog>() where TCatalog : CatalogAbstract
```

#### Returns

 [FlowthruServiceBuilder](Flowthru.Services.FlowthruServiceBuilder.md)

This builder for method chaining

#### Type Parameters

`TCatalog` 

The catalog type

#### Remarks

The catalog will be resolved from the DI container, allowing constructor
parameter injection (e.g., IConfiguration, IOptions).

### <a id="Flowthru_Services_FlowthruServiceBuilder_RegisterCatalog_Flowthru_Data_CatalogAbstract_"></a> RegisterCatalog\(CatalogAbstract\)

Registers a catalog instance directly.

```csharp
public FlowthruServiceBuilder RegisterCatalog(CatalogAbstract catalog)
```

#### Parameters

`catalog` [CatalogAbstract](Flowthru.Data.CatalogAbstract.md)

The catalog instance

#### Returns

 [FlowthruServiceBuilder](Flowthru.Services.FlowthruServiceBuilder.md)

This builder for method chaining

#### Remarks

Use this when the catalog doesn't require dependency injection.

### <a id="Flowthru_Services_FlowthruServiceBuilder_RegisterCatalog__1_System_Func_System_IServiceProvider___0__"></a> RegisterCatalog<TCatalog\>\(Func<IServiceProvider, TCatalog\>\)

Registers a catalog factory that receives the service provider.

```csharp
public FlowthruServiceBuilder RegisterCatalog<TCatalog>(Func<IServiceProvider, TCatalog> catalogFactory) where TCatalog : CatalogAbstract
```

#### Parameters

`catalogFactory` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[IServiceProvider](https://learn.microsoft.com/dotnet/api/system.iserviceprovider), TCatalog\>

Factory function to create the catalog

#### Returns

 [FlowthruServiceBuilder](Flowthru.Services.FlowthruServiceBuilder.md)

This builder for method chaining

#### Type Parameters

`TCatalog` 

The concrete catalog type

#### Remarks

Use this when the catalog needs to resolve services during construction,
or when construction requires parameters unavailable at the call site.

### <a id="Flowthru_Services_FlowthruServiceBuilder_RegisterCatalogs_System_Collections_Generic_IEnumerable_Flowthru_Data_CatalogAbstract__"></a> RegisterCatalogs\(IEnumerable<CatalogAbstract\>\)

Registers a collection of pre-built catalog instances produced by iterative or dynamic
construction — the fan-out pattern where N identical-shaped catalogs differ only by
their construction parameters (e.g., one catalog per US state).

```csharp
public FlowthruServiceBuilder RegisterCatalogs(IEnumerable<CatalogAbstract> catalogs)
```

#### Parameters

`catalogs` [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<[CatalogAbstract](Flowthru.Data.CatalogAbstract.md)\>

The catalog instances to register

#### Returns

 [FlowthruServiceBuilder](Flowthru.Services.FlowthruServiceBuilder.md)

This builder for method chaining

#### Remarks

All registered catalogs will receive DI service injection and appear in
<xref href="Flowthru.Services.IFlowthruService.Catalogs" data-throw-if-not-resolved="false"></xref>. Use with <xref href="Flowthru.Services.FlowthruServiceBuilder.RegisterFlows(System.Func%7bSystem.IServiceProvider%2cSystem.Collections.Generic.Dictionary%7bSystem.String%2cFlowthru.Flows.Flow%7d%7d)" data-throw-if-not-resolved="false"></xref> to
wire per-catalog flows in a loop.

### <a id="Flowthru_Services_FlowthruServiceBuilder_RegisterCatalogs_System_Func_System_IServiceProvider_System_Collections_Generic_IEnumerable_Flowthru_Data_CatalogAbstract___"></a> RegisterCatalogs\(Func<IServiceProvider, IEnumerable<CatalogAbstract\>\>\)

Registers catalogs produced by a factory that receives the service provider —
useful when catalog construction itself requires DI resolution.

```csharp
public FlowthruServiceBuilder RegisterCatalogs(Func<IServiceProvider, IEnumerable<CatalogAbstract>> catalogsFactory)
```

#### Parameters

`catalogsFactory` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[IServiceProvider](https://learn.microsoft.com/dotnet/api/system.iserviceprovider), [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<[CatalogAbstract](Flowthru.Data.CatalogAbstract.md)\>\>

Factory that returns the catalog collection

#### Returns

 [FlowthruServiceBuilder](Flowthru.Services.FlowthruServiceBuilder.md)

This builder for method chaining

### <a id="Flowthru_Services_FlowthruServiceBuilder_RegisterFlow_System_String_System_Delegate_System_String_"></a> RegisterFlow\(string, Delegate, string?\)

Registers a flow by inspecting the delegate's parameter types at runtime.
Parameters that extend <xref href="Flowthru.Data.CatalogAbstract" data-throw-if-not-resolved="false"></xref> are resolved from DI as catalogs.
All other parameters are resolved from DI as services.

```csharp
public FlowthruServiceBuilder RegisterFlow(string label, Delegate flow, string? configurationSection = null)
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique flow name

`flow` [Delegate](https://learn.microsoft.com/dotnet/api/system.delegate)

A method group or delegate whose parameters are catalogs, services, or config objects

`configurationSection` [string](https://learn.microsoft.com/dotnet/api/system.string)?

Optional configuration section path. When provided, the last non-catalog, non-service parameter
is bound from configuration instead of DI.

#### Returns

 [FlowthruServiceBuilder](Flowthru.Services.FlowthruServiceBuilder.md)

This builder for method chaining

### <a id="Flowthru_Services_FlowthruServiceBuilder_RegisterFlows_System_Func_System_IServiceProvider_System_Collections_Generic_Dictionary_System_String_Flowthru_Flows_Flow___"></a> RegisterFlows\(Func<IServiceProvider, Dictionary<string, Flow\>\>\)

Escape-hatch for registering flows via a full-access service provider factory.

```csharp
public FlowthruServiceBuilder RegisterFlows(Func<IServiceProvider, Dictionary<string, Flow>> flowFactory)
```

#### Parameters

`flowFactory` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[IServiceProvider](https://learn.microsoft.com/dotnet/api/system.iserviceprovider), [Dictionary](https://learn.microsoft.com/dotnet/api/system.collections.generic.dictionary\-2)<[string](https://learn.microsoft.com/dotnet/api/system.string), [Flow](Flowthru.Flows.Flow.md)\>\>

Factory function that receives the service provider and returns the flow dictionary

#### Returns

 [FlowthruServiceBuilder](Flowthru.Services.FlowthruServiceBuilder.md)

This builder for method chaining

#### Remarks

Prefer <xref href="Flowthru.Services.FlowthruServiceBuilder.RegisterFlow(System.String%2cSystem.Delegate%2cSystem.String)" data-throw-if-not-resolved="false"></xref> for standard flow registration.
Use this only when you need full service provider access during flow construction.

### <a id="Flowthru_Services_FlowthruServiceBuilder_UseConfiguration_System_Action_Flowthru_Configuration_FlowthruConfigurationOptions__"></a> UseConfiguration\(Action<FlowthruConfigurationOptions\>?\)

Enables configuration loading from JSON and YAML files.

```csharp
public FlowthruServiceBuilder UseConfiguration(Action<FlowthruConfigurationOptions>? configure = null)
```

#### Parameters

`configure` [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<[FlowthruConfigurationOptions](Flowthru.Configuration.FlowthruConfigurationOptions.md)\>?

Optional action to configure how configuration files are loaded

#### Returns

 [FlowthruServiceBuilder](Flowthru.Services.FlowthruServiceBuilder.md)

This builder for method chaining

#### Remarks

By default, configuration is loaded from appsettings.json and environment-specific overrides.

### <a id="Flowthru_Services_FlowthruServiceBuilder_UseStorageStrategy__1"></a> UseStorageStrategy<TStrategy\>\(\)

Registers a storage entry factory (for environment-specific entries).

```csharp
public FlowthruServiceBuilder UseStorageStrategy<TStrategy>() where TStrategy : class, IStorageEntryFactory
```

#### Returns

 [FlowthruServiceBuilder](Flowthru.Services.FlowthruServiceBuilder.md)

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

### <a id="Flowthru_Services_FlowthruServiceBuilder_UseStorageStrategy_Flowthru_Data_Storage_Strategies_IStorageEntryFactory_"></a> UseStorageStrategy\(IStorageEntryFactory\)

Registers a storage entry factory instance.

```csharp
public FlowthruServiceBuilder UseStorageStrategy(IStorageEntryFactory strategy)
```

#### Parameters

`strategy` [IStorageEntryFactory](Flowthru.Data.Storage.Strategies.IStorageEntryFactory.md)

The storage strategy instance

#### Returns

 [FlowthruServiceBuilder](Flowthru.Services.FlowthruServiceBuilder.md)

This builder for method chaining

### <a id="Flowthru_Services_FlowthruServiceBuilder_UseStorageStrategy_System_Func_System_IServiceProvider_Flowthru_Data_Storage_Strategies_IStorageEntryFactory__"></a> UseStorageStrategy\(Func<IServiceProvider, IStorageEntryFactory\>\)

Registers a storage entry factory using a factory function.

```csharp
public FlowthruServiceBuilder UseStorageStrategy(Func<IServiceProvider, IStorageEntryFactory> strategyFactory)
```

#### Parameters

`strategyFactory` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[IServiceProvider](https://learn.microsoft.com/dotnet/api/system.iserviceprovider), [IStorageEntryFactory](Flowthru.Data.Storage.Strategies.IStorageEntryFactory.md)\>

Factory function to create the strategy

#### Returns

 [FlowthruServiceBuilder](Flowthru.Services.FlowthruServiceBuilder.md)

This builder for method chaining

### <a id="Flowthru_Services_FlowthruServiceBuilder_WithDescription_System_String_"></a> WithDescription\(string\)

Adds a description to the most recently registered flow.

```csharp
public FlowthruServiceBuilder WithDescription(string description)
```

#### Parameters

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Human-readable description of what the flow does

#### Returns

 [FlowthruServiceBuilder](Flowthru.Services.FlowthruServiceBuilder.md)

This builder for method chaining

