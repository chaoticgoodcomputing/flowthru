# How to Register Pipelines with Parameters

Pipeline parameters are captured when registering, not when running. Flowthru supports two registration approaches.

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
```

### Inline Registration (Recommended)

```csharp
public class Program
{
    public static Task<int> Main(string[] args)
    {
        return FlowthruApplication.Create(args, builder =>
        {
            builder.UseCatalog(new MyCatalog());
            
            builder
                .RegisterPipeline<MyCatalog>("etl", EtlPipeline.Create)
                .WithDescription("Extract, transform, load")
                .WithTags("data-processing");
        });
    }
}
```

### Registry Class Alternative

```csharp
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

public class Program
{
    public static Task<int> Main(string[] args)
    {
        return FlowthruApplication.Create(args, builder =>
        {
            builder
                .UseCatalog(new MyCatalog())
                .RegisterPipelines<MyPipelineRegistry>();
        });
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
```

### Inline Registration (Recommended)

```csharp
public class Program
{
    public static Task<int> Main(string[] args)
    {
        return FlowthruApplication.Create(args, builder =>
        {
            builder.UseCatalog(new MyCatalog());
            
            builder
                .RegisterPipeline<MyCatalog, ModelOptions>("ml_training", MlPipeline.Create, new ModelOptions
                {
                    TestSize = 0.2,
                    RandomState = 42,
                    Features = new List<string> { "feature1", "feature2" }
                })
                .WithDescription("Train and evaluate ML model")
                .WithTags("machine-learning", "training");
        });
    }
}
```

### Registry Class Alternative

```csharp
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
- Parameters are defined once during registration
- Factory method signature enforces parameter type
- No runtime parameter parsing or validation needed
- Different registrations can use different parameters
- Both approaches provide identical compile-time safety
