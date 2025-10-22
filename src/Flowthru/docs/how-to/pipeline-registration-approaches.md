# How to Choose Pipeline Registration Approach

Flowthru offers two ways to register pipelines: **inline registration** and **registry classes**. Both provide identical compile-time safety.

## Inline Registration

Register pipelines directly in `Program.cs` using the builder API.

### When to Use

- ✅ **Simple applications** (1-5 pipelines)
- ✅ **Getting started** with Flowthru
- ✅ **Single entry point** applications
- ✅ **Rapid prototyping**
- ✅ **Prefer simplicity** over testability

### Example

```csharp
public class Program
{
    public static Task<int> Main(string[] args)
    {
        return FlowthruApplication.Create(args, builder =>
        {
            builder.UseCatalog(new MyCatalog("Data"));
            
            // Register pipelines inline
            builder
                .RegisterPipeline<MyCatalog>("etl", EtlPipeline.Create)
                .WithDescription("Data processing pipeline")
                .WithTags("data");
            
            builder
                .RegisterPipeline<MyCatalog, ModelOptions>("ml", MlPipeline.Create, new ModelOptions
                {
                    TestSize = 0.2,
                    RandomState = 42
                })
                .WithDescription("ML training pipeline")
                .WithTags("ml");
        });
    }
}
```

### Benefits

- **No separate class needed** - Everything in one file
- **Less boilerplate** - No registry class definition
- **Easier to understand** - All configuration visible in one place
- **Faster to write** - Fewer files to create

### Drawbacks

- **Cannot test in isolation** - Registration logic tied to `Program.cs`
- **Not reusable** - Can't share registrations across multiple entry points
- **Harder to organize** - Large applications (10+ pipelines) become cluttered

---

## Registry Classes

Extract pipeline registration to a separate class inheriting from `PipelineRegistry<TCatalog>`.

### When to Use

- ✅ **Complex applications** (10+ pipelines)
- ✅ **Multiple entry points** (CLI tool, web API, worker service)
- ✅ **Need testability** - Want to unit test registration logic
- ✅ **Team collaboration** - Multiple developers working on pipelines
- ✅ **Conditional registration** - Complex logic for which pipelines to register

### Example

```csharp
// Pipelines/MyPipelineRegistry.cs
public class MyPipelineRegistry : PipelineRegistry<MyCatalog>
{
    protected override void RegisterPipelines(IPipelineRegistrar<MyCatalog> registrar)
    {
        registrar
            .Register("etl", EtlPipeline.Create)
            .WithDescription("Data processing pipeline")
            .WithTags("data");
        
        registrar
            .Register("ml", MlPipeline.Create, new ModelOptions
            {
                TestSize = 0.2,
                RandomState = 42
            })
            .WithDescription("ML training pipeline")
            .WithTags("ml");
    }
}

// Program.cs
public class Program
{
    public static Task<int> Main(string[] args)
    {
        return FlowthruApplication.Create(args, builder =>
        {
            builder
                .UseCatalog(new MyCatalog("Data"))
                .RegisterPipelines<MyPipelineRegistry>();
        });
    }
}
```

### Benefits

- **Testable** - Can unit test `GetPipelines()` independently
- **Reusable** - Share same registry across CLI, API, tests
- **Better organization** - Separate file keeps `Program.cs` minimal
- **Conditional logic** - Easy to add environment-based registration

### Drawbacks

- **More boilerplate** - Extra class and file to maintain
- **Indirection** - Have to navigate to separate file to see registrations
- **Overkill for simple apps** - Unnecessary complexity for 1-3 pipelines

---

## Testing Considerations

### Inline Registration Testing

Test the full application end-to-end:

```csharp
[Fact]
public async Task Application_RunsPipeline()
{
    var app = FlowthruApplication.Create(new[] { "etl" }, builder =>
    {
        builder.UseCatalog(new TestCatalog());
        builder.RegisterPipeline<TestCatalog>("etl", TestPipeline.Create);
    });
    
    var exitCode = await app.RunAsync();
    Assert.Equal(0, exitCode);
}
```

### Registry Class Testing

Test registration logic in isolation:

```csharp
[Fact]
public void Registry_RegistersExpectedPipelines()
{
    var catalog = new TestCatalog();
    var registry = new MyPipelineRegistry();
    var pipelines = registry.GetPipelines(catalog);
    
    Assert.Contains("etl", pipelines.Keys);
    Assert.Contains("ml", pipelines.Keys);
    Assert.Equal("Data processing pipeline", pipelines["etl"].Description);
}
```

---

## Recommendation

**Start with inline registration.** It's simpler and faster for most projects.

**Migrate to registry classes when:**
- You have 10+ pipelines
- You need multiple entry points (CLI + API)
- Registration logic becomes complex
- Team size grows and you need better organization

Both approaches are first-class citizens in Flowthru. Choose based on your application's complexity, not on principle.

---

## Migration Path

Moving from inline to registry is straightforward:

### Before (Inline)

```csharp
builder
    .RegisterPipeline<MyCatalog>("etl", EtlPipeline.Create)
    .WithDescription("Data processing")
    .WithTags("data");
```

### After (Registry)

```csharp
// Create MyPipelineRegistry.cs
public class MyPipelineRegistry : PipelineRegistry<MyCatalog>
{
    protected override void RegisterPipelines(IPipelineRegistrar<MyCatalog> registrar)
    {
        registrar
            .Register("etl", EtlPipeline.Create)
            .WithDescription("Data processing")
            .WithTags("data");
    }
}

// Update Program.cs
builder.RegisterPipelines<MyPipelineRegistry>();
```

The registration calls are **identical** - just move them from `Program.cs` to the registry class.
