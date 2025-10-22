# Tutorials

## Setting Up Your First Pipeline Application

Flowthru provides a minimal-boilerplate application pattern inspired by Kedro. In this tutorial, you'll create a complete pipeline application in under 20 lines of code.

### What You'll Build

- A data catalog with typed entries
- A pipeline registry with multiple pipelines
- A console application with proper logging and error handling

### Step 1: Install Flowthru

```bash
dotnet add package Flowthru
# Flowthru automatically includes Microsoft.Extensions.* packages
```

### Step 2: Create Your Data Catalog

The catalog is a strongly-typed registry of all data in your pipeline. Inherit from `DataCatalogBase`:

```csharp
using Flowthru.Data;
using Flowthru.Data.Implementations;

public class MyCatalog : DataCatalogBase
{
    // Raw CSV inputs
    public CsvCatalogDataset<RawData> RawData { get; }
    
    // Processed parquet outputs
    public ParquetCatalogDataset<ProcessedData> ProcessedData { get; }
    
    public MyCatalog()
    {
        RawData = CreateCsvDataset<RawData>("raw_data", "data/raw.csv");
        ProcessedData = CreateParquetDataset<ProcessedData>("processed", "data/processed.parquet");
    }
}
```

**Key Design Points:**
- Catalog entries are **properties**, not method calls or string lookups
- Compiler enforces correct types at every usage site
- IntelliSense shows all available datasets

### Step 3: Create Pipeline Factory Methods

Pipelines are built using factory methods that accept your catalog. Create static classes to organize them:

```csharp
using Flowthru.Pipelines;

public static class DataProcessingPipeline
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

### Step 4: Create Pipeline Registry

The registry connects named pipelines to their factory methods. Inherit from `PipelineRegistry<TCatalog>`:

```csharp
using Flowthru.Registry;

public class MyPipelineRegistry : PipelineRegistry<MyCatalog>
{
    protected override void RegisterPipelines(IPipelineRegistrar<MyCatalog> registrar)
    {
        // Pipeline without parameters
        registrar
            .Register("data_processing", DataProcessingPipeline.Create)
            .WithDescription("Extract, transform, and load raw data")
            .WithTags("etl", "preprocessing");
        
        // Pipeline with parameters
        registrar
            .Register("data_science", DataSciencePipeline.Create, new ModelOptions
            {
                TestSize = 0.2,
                RandomState = 42,
                Features = new List<string> { /* ... */ }
            })
            .WithDescription("Train and evaluate ML model")
            .WithTags("ml", "training");
    }
}
```

**Key Design Points:**
- `Register()` captures the catalog type in the factory signature
- Compiler verifies `DataProcessingPipeline.Create` accepts `MyCatalog`
- Parameters are strongly-typed (no string-based configuration)
- Fluent API allows chaining metadata

### Step 5: Create Application Entry Point

The `FlowthruApplication` handles DI, logging, catalog initialization, and pipeline execution:

```csharp
public class Program
{
    public static Task<int> Main(string[] args)
    {
        return FlowthruApplication.Create(args, builder =>
        {
            builder
                .UseCatalog(new MyCatalog())
                .RegisterPipelines<MyPipelineRegistry>()
                .ConfigureLogging(logging =>
                {
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Information);
                });
        });
    }
}
```

**That's it!** Your entire application fits in 15 lines.

### Step 6: Run Your Pipeline

```bash
# Run specific pipeline
dotnet run data_processing

# Run different pipeline
dotnet run data_science

# See available pipelines
dotnet run
# Output: Available pipelines: data_processing, data_science
```

The application automatically:
- ✅ Parses command-line arguments
- ✅ Selects and builds the requested pipeline
- ✅ Executes with detailed logging
- ✅ Formats results with success/failure status
- ✅ Returns proper exit codes (0 = success, 1 = failure)

---

## Working with Data

Data in Flowthru flows through strongly-typed catalog entries. All type mismatches are caught at compile-time.

### Define Data Schemas

Use C# records for immutability and structural equality:

```csharp
// Raw schema
public record CompanyRawData
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Rating { get; init; } // "85%"
}

// Processed schema
public record CompanyProcessed
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required decimal Rating { get; init; } // 0.85
}
```

### Add Entries to Catalog

```csharp
public class MyCatalog : DataCatalogBase
{
    public CsvCatalogDataset<CompanyRawData> RawCompanies { get; }
    public ParquetCatalogDataset<CompanyProcessed> ProcessedCompanies { get; }
    
    public MyCatalog()
    {
        RawCompanies = CreateCsvDataset<CompanyRawData>("raw_companies", "data/raw_companies.csv");
        ProcessedCompanies = CreateParquetDataset<CompanyProcessed>("processed_companies", "data/processed_companies.parquet");
    }
}
```

### Choose the Right Storage Format

| Storage Type               | Best For                           | Pros                       | Cons                                       |
| -------------------------- | ---------------------------------- | -------------------------- | ------------------------------------------ |
| `CsvCatalogDataset<T>`     | Raw inputs, human-readable outputs | Human-readable, universal  | Slow for large data, no schema enforcement |
| `ParquetCatalogDataset<T>` | Intermediate data, feature tables  | Fast, columnar, compressed | Binary format, requires tools to inspect   |
| `MemoryCatalogDataset<T>`  | Temporary pipeline data            | No I/O overhead            | Lost when app terminates                   |
| `ExcelCatalogDataset<T>`   | Business-provided data             | Familiar to analysts       | Read-only, limited scale                   |

---

## Building Pipelines

Pipelines are directed acyclic graphs (DAGs) of transformation nodes.

### Create Transformation Nodes

Nodes inherit from `NodeBase<TInput, TOutput>` and implement pure transformation logic:

```csharp
using Flowthru.Nodes;

public class ProcessCompaniesNode : NodeBase<CompanyRawData, CompanyProcessed>
{
    protected override Task<IEnumerable<CompanyProcessed>> Transform(
        IEnumerable<CompanyRawData> input)
    {
        var processed = input.Select(company => new CompanyProcessed
        {
            Id = company.Id,
            Name = company.Name,
            Rating = ParsePercentage(company.Rating)
        });
        
        return Task.FromResult(processed);
    }
    
    private decimal ParsePercentage(string percentage)
    {
        // "85%" → 0.85
        var number = percentage.TrimEnd('%');
        return decimal.Parse(number) / 100m;
    }
}
```

**Requirements:**
- ✅ Must have parameterless constructor
- ✅ Must be stateless (thread-safe)
- ✅ Should be pure (no side effects)
- ✅ Use `Task.FromResult()` for synchronous logic

### Wire Nodes into Pipelines

```csharp
public static class ProcessingPipeline
{
    public static Pipeline Create(MyCatalog catalog)
    {
        return new PipelineBuilder()
            .AddNode<ProcessCompaniesNode>(
                input: catalog.RawCompanies,
                output: catalog.ProcessedCompanies,
                name: "process_companies")
            .Build();
    }
}
```

**Compile-Time Safety:**
- ✅ `catalog.RawCompanies` is `ICatalogDataset<CompanyRawData>`
- ✅ Node expects `NodeBase<CompanyRawData, CompanyProcessed>`
- ✅ Compiler verifies input type matches
- ✅ Compiler verifies output type matches
- ❌ Type mismatch = **compilation error**

---

## Working with Nodes

Nodes are the core transformation units. Flowthru enforces best practices through the type system.

### Single-Input, Single-Output Nodes

Most nodes transform one dataset into another:

```csharp
public class FilterValidRecordsNode : NodeBase<RawRecord, ValidRecord>
{
    protected override Task<IEnumerable<ValidRecord>> Transform(
        IEnumerable<RawRecord> input)
    {
        var valid = input.Where(r => r.IsValid);
        // Transform logic...
        return Task.FromResult(valid);
    }
}
```

### Multi-Input Nodes (Using Input Schemas)

For nodes that need multiple datasets, create an input schema:

```csharp
// Input schema groups multiple datasets
public record JoinInputs
{
    [Required]
    public required IEnumerable<Company> Companies { get; init; }
    
    [Required]
    public required IEnumerable<Review> Reviews { get; init; }
}

public class JoinNode : NodeBase<JoinInputs, EnrichedCompany>
{
    protected override Task<IEnumerable<EnrichedCompany>> Transform(
        IEnumerable<JoinInputs> inputs)
    {
        var input = inputs.Single(); // Schemas are always singleton
        
        // Join logic using input.Companies and input.Reviews...
        
        return Task.FromResult(enriched);
    }
}
```

Wire it up with `CatalogMap<T>`:

```csharp
var inputMap = new CatalogMap<JoinInputs>();
inputMap.Map(i => i.Companies, catalog.Companies);
inputMap.Map(i => i.Reviews, catalog.Reviews);

builder.AddNode<JoinNode>(
    inputMap: inputMap,
    output: catalog.EnrichedCompanies,
    name: "join_data");
```

**Compile-Time Safety:**
- ✅ Expression `i => i.Companies` is validated by compiler
- ✅ Type of `catalog.Companies` must match property type
- ✅ Refactoring `Companies` property auto-updates all references
- ❌ Typo in property name = **compilation error**

### Nodes with Parameters

Configuration values are strongly-typed and injected via input schemas:

```csharp
public record SplitInputs
{
    [Required]
    public required IEnumerable<FeatureRow> Data { get; init; }
    
    [Required]
    public required ModelOptions Options { get; init; } // Parameter
}

public record ModelOptions
{
    public double TestSize { get; init; } = 0.2;
    public int RandomState { get; init; } = 42;
    public List<string> Features { get; init; } = new();
}
```

Map parameters using `MapParameter()`:

```csharp
var options = new ModelOptions { TestSize = 0.3, RandomState = 42 };

var inputMap = new CatalogMap<SplitInputs>();
inputMap.Map(i => i.Data, catalog.FeatureData);
inputMap.MapParameter(i => i.Options, options);

builder.AddNode<SplitDataNode>(inputMap: inputMap, /* ... */);
```

**Compile-Time Safety:**
- ✅ `ModelOptions` is a real class, not a dictionary
- ✅ IntelliSense shows all available options
- ✅ Typo in option name = **compilation error**
- ✅ Wrong type for option value = **compilation error**
