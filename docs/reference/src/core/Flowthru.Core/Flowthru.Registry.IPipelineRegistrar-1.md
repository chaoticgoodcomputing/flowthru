# <a id="Flowthru_Registry_IFlowRegistrar_1"></a> Interface IFlowRegistrar<TCatalog\>

Namespace: [Flowthru.Registry](Flowthru.Registry.md)  
Assembly: Flowthru.Core.dll  

Fluent interface for registering pipelines in a type-safe manner.

```csharp
public interface IFlowRegistrar<TCatalog> where TCatalog : DataCatalogBase
```

#### Type Parameters

`TCatalog` 

The catalog type that pipelines will use

## Remarks

<p>
This interface provides compile-time type safety by tying pipeline factories
to a specific catalog type. The registrar validates that all registered
pipelines accept the correct catalog type.
</p>
<p>
<strong>Usage:</strong>
<pre><code class="lang-csharp">protected override void RegisterPipelines(IFlowRegistrar&lt;MyCatalog&gt; registrar)
{
    // Pipeline without parameters
    registrar.Register("processing", ProcessingPipeline.Create);

    // Pipeline with parameters
    registrar.Register("training", TrainPipeline.Create, new TrainOptions());

    // Add metadata
    registrar.WithDescription("processing", "Cleans and transforms raw data");
}</code></pre>
</p>

## Methods

### <a id="Flowthru_Registry_IFlowRegistrar_1_Register_System_String_System_Func__0_Flowthru_Pipelines_Pipeline__"></a> Register\(string, Func<TCatalog, Pipeline\>\)

Registers a pipeline with a parameterless factory function.

```csharp
IFlowRegistrar<TCatalog> Register(string name, Func<TCatalog, Pipeline> pipelineFactory)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique pipeline name

`pipelineFactory` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TCatalog, [Pipeline](Flowthru.Flows.Pipeline.md)\>

Factory function that creates the pipeline from catalog

#### Returns

 [IFlowRegistrar](Flowthru.Registry.IFlowRegistrar\-1.md)<TCatalog\>

This registrar for method chaining

#### Remarks

Use this overload when the pipeline doesn't require parameters.

### <a id="Flowthru_Registry_IFlowRegistrar_1_Register__1_System_String_System_Func__0___0_Flowthru_Pipelines_Pipeline____0_"></a> Register<TParams\>\(string, Func<TCatalog, TParams, Pipeline\>, TParams\)

Registers a pipeline with a parameterized factory function.

```csharp
IFlowRegistrar<TCatalog> Register<TParams>(string name, Func<TCatalog, TParams, Pipeline> pipelineFactory, TParams parameters)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique pipeline name

`pipelineFactory` [Func](https://learn.microsoft.com/dotnet/api/system.func\-3)<TCatalog, TParams, [Pipeline](Flowthru.Flows.Pipeline.md)\>

Factory function that creates the pipeline from catalog and parameters

`parameters` TParams

Parameter instance to pass to the pipeline

#### Returns

 [IFlowRegistrar](Flowthru.Registry.IFlowRegistrar\-1.md)<TCatalog\>

This registrar for method chaining

#### Type Parameters

`TParams` 

The type of parameters the pipeline requires

#### Remarks

<p>
Use this overload when the pipeline requires configuration parameters.
Parameters are strongly typed and checked at compile time.
</p>
<p>
The factory signature must match: <code>Func&lt;TCatalog, TParams, Pipeline&gt;</code>
</p>

### <a id="Flowthru_Registry_IFlowRegistrar_1_WithDescription_System_String_"></a> WithDescription\(string\)

Adds a description to the most recently registered pipeline.

```csharp
IFlowRegistrar<TCatalog> WithDescription(string description)
```

#### Parameters

`description` [string](https://learn.microsoft.com/dotnet/api/system.string)

Human-readable description of what the pipeline does

#### Returns

 [IFlowRegistrar](Flowthru.Registry.IFlowRegistrar\-1.md)<TCatalog\>

This registrar for method chaining

#### Remarks

Use this overload when fluently chaining after Register().

### <a id="Flowthru_Registry_IFlowRegistrar_1_WithValidation_System_Action_Flowthru_Pipelines_Validation_ValidationOptions__"></a> WithValidation\(Action<ValidationOptions\>\)

Configures validation options for the most recently registered pipeline.

```csharp
IFlowRegistrar<TCatalog> WithValidation(Action<ValidationOptions> configure)
```

#### Parameters

`configure` [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<[ValidationOptions](Flowthru.Flows.Validation.ValidationOptions.md)\>

Action to configure validation behavior

#### Returns

 [IFlowRegistrar](Flowthru.Registry.IFlowRegistrar\-1.md)<TCatalog\>

This registrar for method chaining

#### Remarks

<p>
Use this to opt into deep inspection for critical external data sources
or to explicitly disable inspection for specific inputs.
</p>
<p>
<strong>Example:</strong>
</p>
<pre><code class="lang-csharp">registrar.Register("data_processing", ProcessingPipeline.Create)
  .WithValidation(validation =&gt; {
    validation.Inspect(catalog.Companies, InspectionLevel.Deep);
    validation.Inspect(catalog.Shuttles, InspectionLevel.Deep);
  });</code></pre>

