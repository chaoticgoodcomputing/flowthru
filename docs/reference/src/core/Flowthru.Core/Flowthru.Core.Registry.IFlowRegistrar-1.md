# <a id="Flowthru_Core_Registry_IFlowRegistrar_1"></a> Interface IFlowRegistrar<TCatalog\>

Namespace: [Flowthru.Core.Registry](Flowthru.Core.Registry.md)  
Assembly: Flowthru.Core.dll  

Fluent interface for registering flows in a type-safe manner.

```csharp
public interface IFlowRegistrar<TCatalog> where TCatalog : CatalogAbstract
```

#### Type Parameters

`TCatalog` 

The catalog type that flows will use

## Remarks

<p>
This interface provides compile-time type safety by tying Flow factories
to a specific catalog type. The registrar validates that all registered
flows accept the correct catalog type.
</p>
<p>
<strong>Usage:</strong>
<pre><code class="lang-csharp">protected override void RegisterFlows(IFlowRegistrar&lt;MyCatalog&gt; registrar)
{
    // Flow without parameters
    registrar.Register("processing", ProcessingFlow.Create);

    // Flow with parameters
    registrar.Register("training", TrainFlow.Create, new TrainOptions());

    // Add metadata
    registrar.WithDescription("processing", "Cleans and transforms raw data");
}</code></pre>
</p>

## Methods

### <a id="Flowthru_Core_Registry_IFlowRegistrar_1_Register_System_String_System_Func__0_Flowthru_Core_Flows_Flow__"></a> Register\(string, Func<TCatalog, Flow\>\)

Registers a Flow with a parameterless factory function.

```csharp
IFlowRegistrar<TCatalog> Register(string name, Func<TCatalog, Flow> flowFactory)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique Flow name

`flowFactory` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TCatalog, [Flow](Flowthru.Core.Flows.Flow.md)\>

Factory function that creates the Flow from catalog

#### Returns

 [IFlowRegistrar](Flowthru.Core.Registry.IFlowRegistrar\-1.md)<TCatalog\>

This registrar for method chaining

#### Remarks

Use this overload when the Flow doesn't require parameters.

### <a id="Flowthru_Core_Registry_IFlowRegistrar_1_Register__1_System_String_System_Func__0___0_Flowthru_Core_Flows_Flow____0_"></a> Register<TParams\>\(string, Func<TCatalog, TParams, Flow\>, TParams\)

Registers a Flow with a parameterized factory function.

```csharp
IFlowRegistrar<TCatalog> Register<TParams>(string name, Func<TCatalog, TParams, Flow> flowFactory, TParams parameters)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique Flow name

`flowFactory` [Func](https://learn.microsoft.com/dotnet/api/system.func\-3)<TCatalog, TParams, [Flow](Flowthru.Core.Flows.Flow.md)\>

Factory function that creates the Flow from catalog and parameters

`parameters` TParams

Parameter instance to pass to the flow

#### Returns

 [IFlowRegistrar](Flowthru.Core.Registry.IFlowRegistrar\-1.md)<TCatalog\>

This registrar for method chaining

#### Type Parameters

`TParams` 

The type of parameters the Flow requires

#### Remarks

<p>
Use this overload when the Flow requires configuration parameters.
Parameters are strongly typed and checked at compile time.
</p>
<p>
The factory signature must match: <code>Func&lt;TCatalog, TParams, Flow&gt;</code>
</p>

### <a id="Flowthru_Core_Registry_IFlowRegistrar_1_WithDescription_System_String_"></a> WithDescription\(string\)

Adds a description to the most recently registered flow.

```csharp
IFlowRegistrar<TCatalog> WithDescription(string description)
```

#### Parameters

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Human-readable description of what the Flow does

#### Returns

 [IFlowRegistrar](Flowthru.Core.Registry.IFlowRegistrar\-1.md)<TCatalog\>

This registrar for method chaining

#### Remarks

Use this overload when fluently chaining after Register().

### <a id="Flowthru_Core_Registry_IFlowRegistrar_1_WithValidation_System_Action_Flowthru_Core_Graph_Validation_ValidationOptions__"></a> WithValidation\(Action<ValidationOptions\>\)

Configures validation options for the most recently registered flow.

```csharp
IFlowRegistrar<TCatalog> WithValidation(Action<ValidationOptions> configure)
```

#### Parameters

`configure` [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<[ValidationOptions](Flowthru.Core.Graph.Validation.ValidationOptions.md)\>

Action to configure validation behavior

#### Returns

 [IFlowRegistrar](Flowthru.Core.Registry.IFlowRegistrar\-1.md)<TCatalog\>

This registrar for method chaining

#### Remarks

<p>
Use this to opt into deep inspection for critical external data sources
or to explicitly disable inspection for specific inputs.
</p>
<p>
<strong>Example:</strong>
</p>
<pre><code class="lang-csharp">registrar.Register("data_processing", ProcessingFlow.Create)
  .WithValidation(validation =&gt; {
    validation.Inspect(catalog.Companies, InspectionLevel.Deep);
    validation.Inspect(catalog.Shuttles, InspectionLevel.Deep);
  });</code></pre>

