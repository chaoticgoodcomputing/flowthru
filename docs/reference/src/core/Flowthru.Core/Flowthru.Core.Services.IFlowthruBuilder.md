# <a id="Flowthru_Core_Services_IFlowthruBuilder"></a> Interface IFlowthruBuilder

Namespace: [Flowthru.Core.Services](Flowthru.Core.Services.md)  
Assembly: Flowthru.Core.dll  

Builder interface for configuring Flowthru service registration.

```csharp
public interface IFlowthruBuilder
```

## Remarks

<p>
Implement extension methods on this interface to add optional Flowthru features.
Access <xref href="Flowthru.Core.Services.IFlowthruBuilder.Services" data-throw-if-not-resolved="false"></xref> to register components and <xref href="Flowthru.Core.Services.IFlowthruBuilder.Configuration" data-throw-if-not-resolved="false"></xref>
to bind strongly-typed options:
</p>
<pre><code class="lang-csharp">public static IFlowthruBuilder UseSpark(this IFlowthruBuilder builder)
{
    builder.Services.AddOptions&lt;SparkConnectOptions&gt;()
        .Configure&lt;IConfiguration&gt;((opts, config) =&gt;
            config.GetSection("Flowthru:Spark").Bind(opts))
        .ValidateOnStart();
    builder.Services.AddSingleton&lt;SparkFrameProvider&gt;();
    return builder;
}</code></pre>

## Properties

### <a id="Flowthru_Core_Services_IFlowthruBuilder_Configuration"></a> Configuration

The application configuration passed to <code>AddFlowthru</code>.
Available to extensions that need to read config values at registration time.

```csharp
IConfiguration Configuration { get; }
```

#### Property Value

 [IConfiguration](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration.iconfiguration)

### <a id="Flowthru_Core_Services_IFlowthruBuilder_Services"></a> Services

The underlying DI service collection.
Use this to register services, options, and validators.

```csharp
IServiceCollection Services { get; }
```

#### Property Value

 [IServiceCollection](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.iservicecollection)

## Methods

### <a id="Flowthru_Core_Services_IFlowthruBuilder_ConfigureExecution_System_Action_Flowthru_Core_Flows_ExecutionOptions__"></a> ConfigureExecution\(Action<ExecutionOptions\>\)

Configures service-level default execution behaviour for all flows.

```csharp
IFlowthruBuilder ConfigureExecution(Action<ExecutionOptions> configure)
```

#### Parameters

`configure` [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<[ExecutionOptions](Flowthru.Core.Flows.ExecutionOptions.md)\>

#### Returns

 [IFlowthruBuilder](Flowthru.Core.Services.IFlowthruBuilder.md)

#### Remarks

Code-first overrides take effect after config-file binding. Values set here
override anything set in the <code>Flowthru:Execution</code> appsettings section.
Per-call <xref href="Flowthru.Core.Flows.ExecutionOptions" data-throw-if-not-resolved="false"></xref> passed to
<xref href="Flowthru.Core.Services.IFlowthruService.ExecuteFlowAsync(Flowthru.Core.Flows.ExecutionOptions%2cSystem.Boolean%2cSystem.Threading.CancellationToken)" data-throw-if-not-resolved="false"></xref> take precedence over both.

### <a id="Flowthru_Core_Services_IFlowthruBuilder_ConfigureMetadata_System_Action_Flowthru_Core_Meta_FlowthruMetadataBuilder__"></a> ConfigureMetadata\(Action<FlowthruMetadataBuilder\>\)

Configures metadata export (DAG diagrams, JSON manifests, etc.).

```csharp
IFlowthruBuilder ConfigureMetadata(Action<FlowthruMetadataBuilder> configure)
```

#### Parameters

`configure` [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<[FlowthruMetadataBuilder](Flowthru.Core.Meta.FlowthruMetadataBuilder.md)\>

#### Returns

 [IFlowthruBuilder](Flowthru.Core.Services.IFlowthruBuilder.md)

### <a id="Flowthru_Core_Services_IFlowthruBuilder_ConfigureServices_System_Action_Microsoft_Extensions_DependencyInjection_IServiceCollection__"></a> ConfigureServices\(Action<IServiceCollection\>\)

Convenience escape hatch for registering additional services with the underlying
<xref href="Flowthru.Core.Services.IFlowthruBuilder.Services" data-throw-if-not-resolved="false"></xref> collection. Prefer using <xref href="Flowthru.Core.Services.IFlowthruBuilder.Services" data-throw-if-not-resolved="false"></xref> directly.

```csharp
IFlowthruBuilder ConfigureServices(Action<IServiceCollection> configure)
```

#### Parameters

`configure` [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<[IServiceCollection](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.iservicecollection)\>

#### Returns

 [IFlowthruBuilder](Flowthru.Core.Services.IFlowthruBuilder.md)

### <a id="Flowthru_Core_Services_IFlowthruBuilder_RegisterCatalog__1"></a> RegisterCatalog<TCatalog\>\(\)

Registers a catalog type with DI constructor injection.

```csharp
IFlowthruBuilder RegisterCatalog<TCatalog>() where TCatalog : CatalogAbstract
```

#### Returns

 [IFlowthruBuilder](Flowthru.Core.Services.IFlowthruBuilder.md)

#### Type Parameters

`TCatalog` 

### <a id="Flowthru_Core_Services_IFlowthruBuilder_RegisterCatalog_Flowthru_Core_Data_CatalogAbstract_"></a> RegisterCatalog\(CatalogAbstract\)

Registers a catalog instance directly.

```csharp
IFlowthruBuilder RegisterCatalog(CatalogAbstract catalog)
```

#### Parameters

`catalog` [CatalogAbstract](Flowthru.Core.Data.CatalogAbstract.md)

#### Returns

 [IFlowthruBuilder](Flowthru.Core.Services.IFlowthruBuilder.md)

### <a id="Flowthru_Core_Services_IFlowthruBuilder_RegisterCatalog__1_System_Func_System_IServiceProvider___0__"></a> RegisterCatalog<TCatalog\>\(Func<IServiceProvider, TCatalog\>\)

Registers a catalog via a factory that receives the service provider.

```csharp
IFlowthruBuilder RegisterCatalog<TCatalog>(Func<IServiceProvider, TCatalog> catalogFactory) where TCatalog : CatalogAbstract
```

#### Parameters

`catalogFactory` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[IServiceProvider](https://learn.microsoft.com/dotnet/api/system.iserviceprovider), TCatalog\>

#### Returns

 [IFlowthruBuilder](Flowthru.Core.Services.IFlowthruBuilder.md)

#### Type Parameters

`TCatalog` 

### <a id="Flowthru_Core_Services_IFlowthruBuilder_RegisterCatalogs_System_Collections_Generic_IEnumerable_Flowthru_Core_Data_CatalogAbstract__"></a> RegisterCatalogs\(IEnumerable<CatalogAbstract\>\)

Registers a collection of pre-built catalog instances (fan-out pattern).

```csharp
IFlowthruBuilder RegisterCatalogs(IEnumerable<CatalogAbstract> catalogs)
```

#### Parameters

`catalogs` [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<[CatalogAbstract](Flowthru.Core.Data.CatalogAbstract.md)\>

#### Returns

 [IFlowthruBuilder](Flowthru.Core.Services.IFlowthruBuilder.md)

### <a id="Flowthru_Core_Services_IFlowthruBuilder_RegisterCatalogs_System_Func_System_IServiceProvider_System_Collections_Generic_IEnumerable_Flowthru_Core_Data_CatalogAbstract___"></a> RegisterCatalogs\(Func<IServiceProvider, IEnumerable<CatalogAbstract\>\>\)

Registers multiple catalogs via a factory that receives the service provider.

```csharp
IFlowthruBuilder RegisterCatalogs(Func<IServiceProvider, IEnumerable<CatalogAbstract>> catalogsFactory)
```

#### Parameters

`catalogsFactory` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[IServiceProvider](https://learn.microsoft.com/dotnet/api/system.iserviceprovider), [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<[CatalogAbstract](Flowthru.Core.Data.CatalogAbstract.md)\>\>

#### Returns

 [IFlowthruBuilder](Flowthru.Core.Services.IFlowthruBuilder.md)

### <a id="Flowthru_Core_Services_IFlowthruBuilder_RegisterFlow_System_String_System_Delegate_"></a> RegisterFlow\(string, Delegate\)

Registers a flow by inspecting the delegate's parameter types.
Catalog parameters are resolved from DI; all others are resolved from DI as services.

```csharp
IFlowthruBuilder RegisterFlow(string label, Delegate flow)
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique flow name.

`flow` [Delegate](https://learn.microsoft.com/dotnet/api/system.delegate)

Delegate whose parameters are catalogs or DI-registered services.

#### Returns

 [IFlowthruBuilder](Flowthru.Core.Services.IFlowthruBuilder.md)

### <a id="Flowthru_Core_Services_IFlowthruBuilder_RegisterFlows_System_Func_System_IServiceProvider_System_Collections_Generic_Dictionary_System_String_Flowthru_Core_Flows_Flow___"></a> RegisterFlows\(Func<IServiceProvider, Dictionary<string, Flow\>\>\)

Escape-hatch for registering flows via a full-access service provider factory.
Prefer <xref href="Flowthru.Core.Services.IFlowthruBuilder.RegisterFlow(System.String%2cSystem.Delegate)" data-throw-if-not-resolved="false"></xref> for standard flow registration.

```csharp
IFlowthruBuilder RegisterFlows(Func<IServiceProvider, Dictionary<string, Flow>> flowFactory)
```

#### Parameters

`flowFactory` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[IServiceProvider](https://learn.microsoft.com/dotnet/api/system.iserviceprovider), [Dictionary](https://learn.microsoft.com/dotnet/api/system.collections.generic.dictionary\-2)<[string](https://learn.microsoft.com/dotnet/api/system.string), [Flow](Flowthru.Core.Flows.Flow.md)\>\>

#### Returns

 [IFlowthruBuilder](Flowthru.Core.Services.IFlowthruBuilder.md)

### <a id="Flowthru_Core_Services_IFlowthruBuilder_UseStorageStrategy__1"></a> UseStorageStrategy<TStrategy\>\(\)

Registers a storage entry factory type.

```csharp
IFlowthruBuilder UseStorageStrategy<TStrategy>() where TStrategy : class, IStorageEntryFactory
```

#### Returns

 [IFlowthruBuilder](Flowthru.Core.Services.IFlowthruBuilder.md)

#### Type Parameters

`TStrategy` 

### <a id="Flowthru_Core_Services_IFlowthruBuilder_UseStorageStrategy_Flowthru_Core_Data_Storage_Strategies_IStorageEntryFactory_"></a> UseStorageStrategy\(IStorageEntryFactory\)

Registers a storage entry factory instance.

```csharp
IFlowthruBuilder UseStorageStrategy(IStorageEntryFactory strategy)
```

#### Parameters

`strategy` [IStorageEntryFactory](Flowthru.Core.Data.Storage.Strategies.IStorageEntryFactory.md)

#### Returns

 [IFlowthruBuilder](Flowthru.Core.Services.IFlowthruBuilder.md)

### <a id="Flowthru_Core_Services_IFlowthruBuilder_UseStorageStrategy_System_Func_System_IServiceProvider_Flowthru_Core_Data_Storage_Strategies_IStorageEntryFactory__"></a> UseStorageStrategy\(Func<IServiceProvider, IStorageEntryFactory\>\)

Registers a storage entry factory via a service-provider factory.

```csharp
IFlowthruBuilder UseStorageStrategy(Func<IServiceProvider, IStorageEntryFactory> strategyFactory)
```

#### Parameters

`strategyFactory` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[IServiceProvider](https://learn.microsoft.com/dotnet/api/system.iserviceprovider), [IStorageEntryFactory](Flowthru.Core.Data.Storage.Strategies.IStorageEntryFactory.md)\>

#### Returns

 [IFlowthruBuilder](Flowthru.Core.Services.IFlowthruBuilder.md)

### <a id="Flowthru_Core_Services_IFlowthruBuilder_WithDescription_System_String_"></a> WithDescription\(string\)

Adds a description to the most recently registered flow.

```csharp
IFlowthruBuilder WithDescription(string description)
```

#### Parameters

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [IFlowthruBuilder](Flowthru.Core.Services.IFlowthruBuilder.md)

