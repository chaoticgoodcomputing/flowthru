# Create a Pipeline

You've defined schemas and registered raw datasets in the catalog. Now it's time to create your first data processing pipeline!

There are two ways to approach this: use the pipeline template to generate scaffolding, or create the files manually. We'll cover both approaches.

## Option 1: Using the Pipeline Template (Recommended)

Flowthru provides an item template that generates pipeline scaffolding. From the `Pipelines/` directory:

```bash
cd Pipelines
dotnet new flowthru-pipeline --name DataProcessing --ProjectName Spaceflights
```

This creates:

```
DataProcessing/
├── DataProcessingPipeline.cs       # Pipeline definition
└── Nodes/
    └── DataProcessingDummyNode.cs  # Starter node
```

The generated pipeline uses `NoData` for input/output, so you can verify the structure works before connecting real catalog entries. After generation:

1. **Register the pipeline** in [Program.cs](Program.cs) inside the `ConfigureServices` method:
   ```csharp
   flowthru.RegisterPipeline<DataProcessingPipeline>();
   ```

2. **Replace the dummy node** with your actual transformation logic (we'll do this in the next steps).

## Option 2: Creating Files Manually

If you prefer to understand the structure by building it yourself, create the pipeline and node files from scratch following the patterns in the Minimal starter.

## Next Steps

Now that you have pipeline scaffolding, you'll need to:

1. Define intermediate schemas for processed data
2. Create catalog entries for those schemas  
3. Implement nodes that transform raw data into intermediate data
4. Wire those nodes into your pipeline

We'll tackle each of these in the following sections.

**Continue to: [Add Another Pipeline](04-add-another-pipeline.md)**
