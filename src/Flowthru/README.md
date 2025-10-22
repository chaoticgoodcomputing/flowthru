# Flowthru

A type-safe data engineering framework for .NET inspired by Kedro's declarative pipelines.

**Version:** 0.3.0 (Alpha - Simplified AddNode API)  
**Status:** Active Development

---

## Quick Start

```bash
# Install
dotnet add package Flowthru

# Create your catalog
public class MyCatalog : DataCatalogBase
{
    public CsvCatalogDataset<RawData> RawData { get; }
    public ParquetCatalogDataset<ProcessedData> ProcessedData { get; }
    
    public MyCatalog()
    {
        RawData = CreateCsvDataset<RawData>("raw", "data/raw.csv");
        ProcessedData = CreateParquetDataset<ProcessedData>("processed", "data/processed.parquet");
    }
}

// Run your application (inline registration)
public class Program
{
    public static Task<int> Main(string[] args)
    {
        return FlowthruApplication.Create(args, builder =>
        {
            builder.UseCatalog(new MyCatalog());
            
            // Register pipelines directly in the builder
            builder
                .RegisterPipeline<MyCatalog>("data_processing", DataProcessingPipeline.Create)
                .WithDescription("Process raw data")
                .WithTags("etl");
        });
    }
}
```

```bash
# Execute
dotnet run data_processing
```

---

## Documentation

### [Tutorials](docs/tutorials.md)

**Learning-oriented:** Step-by-step guides to build your first pipeline application.

- Setting Up Your First Pipeline Application
- Working with Data (Catalogs, Schemas, Storage Formats)
- Building Pipelines (Nodes, DAGs, Execution)
- Working with Nodes (Single/Multi-Input/Output, Parameters)

### [How-To Guides](docs/how-to/)

**Problem-oriented:** Practical guides for specific tasks.

- [Choose Pipeline Registration Approach](docs/how-to/pipeline-registration-approaches.md) - Inline vs registry classes
- [Leverage Compile-Time Safety](docs/how-to/compile-time-safety.md) - Maximize type safety
- [Configure Dataset Inspection](docs/how-to/dataset-inspection.md) - Validate external data before execution
- [Structure Multi-Input/Output Nodes](docs/how-to/multi-input-output.md) - Handle complex data flows
- [Add Logging and Dependencies](docs/how-to/logging-dependencies.md) - Inject services into nodes
- [Register Pipelines with Parameters](docs/how-to/pipeline-parameters.md) - Configure pipeline behavior
- [Choose CatalogMap Modes](docs/how-to/catalog-map-modes.md) - Pass-through vs mapped
- [Structure Your Project](docs/how-to/project-structure.md) - Organize code for maintainability
- [Work with Read-Only Data Sources](docs/how-to/read-only-data-sources.md) - Excel files, APIs, immutable data

### [Explanation](docs/explanation.md)

**Understanding-oriented:** Why Flowthru works the way it does.

- Why Compile-Time Safety Matters
- Understanding the Three Compile-Time Safety Layers
- Common Pitfalls and How to Avoid Them
- Design Rationale: Why These Choices?
- Comparing with Kedro: Key Differences
- When to Use Flowthru vs Kedro
- Future Enhancements

---

## Core Features

✅ **Compile-Time Type Safety** - Catch errors before execution, not during  
✅ **Eager Schema Validation** - Validate external data before pipeline runs (optional)  
✅ **Read/Write Capability Enforcement** - Prevent writes to read-only sources (Excel, APIs)  
✅ **Generic Pipeline Registry** - Type-safe pipeline registration with parameters  
✅ **Property-Based Catalogs** - IntelliSense-driven dataset access  
✅ **Expression-Based Mapping** - Refactoring-safe multi-input/output nodes  
✅ **Application Builder Pattern** - Minimal boilerplate (~15 lines)  
✅ **Formatted Console Output** - Beautiful execution logs with progress tracking  
✅ **Dependency Injection** - Property injection for services and loggers  

---

## Philosophy

**Fail at compile-time, not runtime.**

Every design decision in Flowthru prioritizes catching errors during compilation:
- Typos in catalog keys → **Compile error** (property doesn't exist)
- Wrong data types → **Compile error** (generic constraint violation)
- Missing parameters → **Compile error** (property not found)
- Type mismatches → **Compile error** (incompatible types)

The C# compiler becomes your documentation, showing all available datasets, parameters, and types through IntelliSense.

---

## Comparison with Kedro

| Aspect              | Kedro                     | Flowthru                                     |
| ------------------- | ------------------------- | -------------------------------------------- |
| **Type Safety**     | Runtime (duck typing)     | Compile-time (generics)                      |
| **Catalog**         | YAML + string keys        | C# properties                                |
| **Nodes**           | Functions with decorators | Classes inheriting `NodeBase<TIn,TOut>`      |
| **Parameters**      | YAML dictionaries         | Strongly-typed record classes                |
| **Error Detection** | During execution          | During compilation                           |
| **IDE Support**     | Basic                     | Full (IntelliSense, refactoring, navigation) |

---

## Requirements

- .NET 9.0 or later
- C# 12 or later

---

## License

MIT License - See LICENSE file for details

## Acknowledgments

- **Kedro:** Inspiration for declarative pipeline patterns
- **Microsoft.Extensions:** DI and logging infrastructure
