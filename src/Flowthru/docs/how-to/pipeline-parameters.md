# How to Register Pipelines with Parameters

Pipeline parameters are captured when registering, not when running.

## Parameterless Pipeline

```csharp
public static class EtlPipeline
{
    public static Pipeline Create(MyCatalog catalog)
    {
        return new PipelineBuilder()
            .AddNode<ExtractNode>(/* ... */)
            .AddNode<TransformNode>(/* ... */)
            .AddNode<LoadNode>(/* ... */)
            .Build();
    }
}

// Register without parameters
public class MyPipelineRegistry : PipelineRegistry<MyCatalog>
{
    protected override void RegisterPipelines(IPipelineRegistrar<MyCatalog> registrar)
    {
        registrar
            .Register("etl", EtlPipeline.Create)
            .WithDescription("Extract, transform, load")
            .WithTags("data-processing");
    }
}
```

## Parameterized Pipeline

```csharp
public static class MlPipeline
{
    public static Pipeline Create(MyCatalog catalog, ModelOptions options)
    {
        var inputMap = new CatalogMap<SplitInputs>();
        inputMap.Map(i => i.Data, catalog.Features);
        inputMap.MapParameter(i => i.Options, options); // Inject parameters
        
        return new PipelineBuilder()
            .AddNode<SplitDataNode>(inputMap: inputMap, /* ... */)
            .AddNode<TrainModelNode>(/* ... */)
            .AddNode<EvaluateModelNode>(/* ... */)
            .Build();
    }
}

// Register with parameters
public class MyPipelineRegistry : PipelineRegistry<MyCatalog>
{
    protected override void RegisterPipelines(IPipelineRegistrar<MyCatalog> registrar)
    {
        var options = new ModelOptions
        {
            TestSize = 0.2,
            RandomState = 42,
            Features = new List<string> { "feature1", "feature2" }
        };
        
        registrar
            .Register("ml_training", MlPipeline.Create, options)
            .WithDescription("Train and evaluate ML model")
            .WithTags("machine-learning", "training");
    }
}
```

**Key Points:**
- Parameters are defined once in the registry
- Factory method signature enforces parameter type
- No runtime parameter parsing or validation needed
- Different registrations can use different parameters
