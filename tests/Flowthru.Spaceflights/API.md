# FlowThru API Reference

> **Version:** 0.1.0 (Prototype)  
> **Status:** Design & Example Implementation  
> **Project:** Spaceflights Demo

This document describes the API design for FlowThru, a type-safe data engineering framework for .NET inspired by Kedro's declarative pipelines and ChainSharp's fluent syntax.

---

## 1. Data

The Data layer provides compile-time type-safe access to datasets through strongly-typed catalog entries.

### 1.1 Core Interfaces

#### `ICatalogEntry<T>`

Represents a strongly-typed catalog entry that stores data of type `T`.

```csharp
public interface ICatalogEntry<T>
{
    string Key { get; }
    Type DataType => typeof(T);
    Task<T> Load();
    Task Save(T data);
    Task<bool> Exists();
}
```

**Key Characteristics:**
- Generic type parameter `T` is known at compile-time
- Enables compiler validation of node input/output type contracts
- Replaces string-based catalog keys with typed references

**Example:**
```csharp
ICatalogEntry<IEnumerable<CompanySchema>> companies = catalog.Companies;
var data = await companies.Load();  // Typed as IEnumerable<CompanySchema>
```

---

### 1.2 Catalog Entry Implementations

#### `MemoryCatalogEntry<T>`

In-memory storage for transient data (intermediate results, models).

```csharp
public class MemoryCatalogEntry<T> : ICatalogEntry<T>
{
    public MemoryCatalogEntry(string key)
}
```

**Use Cases:**
- Intermediate pipeline datasets
- ML models in memory
- Test data that doesn't need persistence

**Example:**
```csharp
var entry = new MemoryCatalogEntry<IEnumerable<FeatureRow>>("x_train");
await entry.Save(trainingData);
var loaded = await entry.Load();
```

---

#### `CsvCatalogEntry<T>`

CSV file-based storage using CsvHelper.

```csharp
public class CsvCatalogEntry<T> : ICatalogEntry<IEnumerable<T>>
{
    public string FilePath { get; }
    public CsvConfiguration Configuration { get; }
    
    public CsvCatalogEntry(
        string key, 
        string filePath, 
        CsvConfiguration? configuration = null)
}
```

**Default Configuration:**
- `HasHeaderRecord = true`
- `CultureInfo.InvariantCulture`

**Example:**
```csharp
var entry = new CsvCatalogEntry<CompanyRawSchema>(
    "companies",
    "Data/01_Raw/companies.csv");

var companies = await entry.Load();  // IEnumerable<CompanyRawSchema>
```

---

#### `ParquetCatalogEntry<T>`

Parquet file-based storage using Parquet.NET.

```csharp
public class ParquetCatalogEntry<T> : ICatalogEntry<IEnumerable<T>> 
    where T : new()
{
    public string FilePath { get; }
    
    public ParquetCatalogEntry(string key, string filePath)
}
```

**Constraints:**
- `T` must have a parameterless constructor (`new()`)

**Use Cases:**
- Intermediate processed data
- Model input tables
- High-performance columnar storage

**Example:**
```csharp
var entry = new ParquetCatalogEntry<ModelInputSchema>(
    "model_input_table",
    "Data/03_Primary/model_input_table.parquet");

await entry.Save(processedData);
```

---

#### `ExcelCatalogEntry<T>`

Excel file-based read-only storage using ExcelDataReader.

```csharp
public class ExcelCatalogEntry<T> : ICatalogEntry<IEnumerable<T>> 
    where T : new()
{
    public string FilePath { get; }
    public string SheetName { get; }
    
    public ExcelCatalogEntry(
        string key, 
        string filePath, 
        string sheetName = "Sheet1")
}
```

**Limitations:**
- **Read-only**: `Save()` throws `NotSupportedException`
- Use CSV or Parquet for output datasets

**Example:**
```csharp
var entry = new ExcelCatalogEntry<ShuttleRawSchema>(
    "shuttles",
    "Data/01_Raw/shuttles.xlsx",
    "Sheet1");

var shuttles = await entry.Load();
```

---

### 1.3 Typed Catalog

Project-specific strongly-typed catalog class that exposes all datasets as properties.

```csharp
public class SpaceflightsCatalog
{
    // Raw data
    public ICatalogEntry<IEnumerable<CompanyRawSchema>> Companies { get; }
    public ICatalogEntry<IEnumerable<ReviewRawSchema>> Reviews { get; }
    public ICatalogEntry<IEnumerable<ShuttleRawSchema>> Shuttles { get; }
    
    // Intermediate data
    public ICatalogEntry<IEnumerable<CompanySchema>> PreprocessedCompanies { get; }
    public ICatalogEntry<IEnumerable<ShuttleSchema>> PreprocessedShuttles { get; }
    
    // Primary data
    public ICatalogEntry<IEnumerable<ModelInputSchema>> ModelInputTable { get; }
    
    // Model data (split results)
    public ICatalogEntry<IEnumerable<FeatureRow>> XTrain { get; }
    public ICatalogEntry<IEnumerable<FeatureRow>> XTest { get; }
    public ICatalogEntry<IEnumerable<decimal>> YTrain { get; }
    public ICatalogEntry<IEnumerable<decimal>> YTest { get; }
    
    // Models
    public ICatalogEntry<ITransformer> Regressor { get; }
    
    // Reporting
    public ICatalogEntry<ModelMetrics> ModelMetrics { get; }
    
    public static SpaceflightsCatalog Build(string basePath = "Data/Datasets")
}
```

**Benefits:**
- ✅ IntelliSense shows all entries with their types
- ✅ Go To Definition navigates to declaration
- ✅ Find All References shows all usages
- ✅ Rename refactoring works automatically
- ✅ Cannot reference non-existent entries (compile error)
- ✅ Cannot pass wrong type to node (compile error)

**Example:**
```csharp
var catalog = SpaceflightsCatalog.Build();

// IntelliSense shows all properties
catalog.  // <- IDE shows: Companies, Reviews, Shuttles, etc.

// Typed access
ICatalogEntry<IEnumerable<ModelInputSchema>> modelInput = catalog.ModelInputTable;
```

---

### 1.4 Schema Conventions

#### Schema Location Policy

**Rule:** `Data/Schemas/` contains only **pure catalog entry schemas** (domain models).

```
Data/
  Schemas/
    Raw/              # Raw input schemas (external data formats)
      CompanyRawSchema.cs
      ReviewRawSchema.cs
      ShuttleRawSchema.cs
    Processed/        # Cleaned/validated domain schemas
      CompanySchema.cs
      ShuttleSchema.cs
      ReviewSchema.cs
    Models/           # ML-specific domain schemas
      FeatureRow.cs
      ModelInputSchema.cs
      ModelMetrics.cs
```

**What Goes Here:**
- Domain models that represent actual data entities
- Schemas used as catalog entry types
- Reusable data structures across multiple pipelines

**What Does NOT Go Here:**
- Node-specific output schemas (e.g., `SplitDataOutputs`)
- Node parameters (e.g., `ModelOptions`)
- Pipeline coordination types

These belong in `Pipelines/{Pipeline}/Nodes/` colocated with the node (see Section 2.2).

---

#### Schema Design Guidelines

**Use `record` for immutable schemas:**
```csharp
public record CompanySchema
{
    public required string Id { get; init; }
    public required decimal CompanyRating { get; init; }
    public required string? CompanyLocation { get; init; }
    public required decimal TotalFleetCount { get; init; }
    public required bool IataApproved { get; init; }
}
```

**Use `class` for ML.NET schemas (mutable):**
```csharp
public class FeatureRow  // ML.NET requires mutable properties
{
    public float Engines { get; set; }
    public float PassengerCapacity { get; set; }
    public float Price { get; set; }
}
```

---

### 1.5 Data Layering Convention (Kedro)

FlowThru follows Kedro's data engineering layers:

| Layer               | Directory                        | Purpose                               |
| ------------------- | -------------------------------- | ------------------------------------- |
| **01_Raw**          | `Data/Datasets/01_Raw/`          | Raw input data from external sources  |
| **02_Intermediate** | `Data/Datasets/02_Intermediate/` | Preprocessed/cleaned data             |
| **03_Primary**      | `Data/Datasets/03_Primary/`      | Primary model inputs (feature tables) |
| **06_Models**       | `Data/Datasets/06_Models/`       | Trained ML models                     |
| **08_Reporting**    | `Data/Datasets/08_Reporting/`    | Reports and metrics                   |

**Benefits:**
- Clear data lineage
- Organized file structure
- Standardized naming convention
- Easy to understand data flow

---

## 2. Nodes

Nodes are pure transformation functions that process data from catalog entries.

### 2.1 Node Base Classes

#### Single-Input, Single-Output Node

```csharp
public abstract class Node<TInput, TOutput>
{
    protected abstract Task<IEnumerable<TOutput>> Transform(
        IEnumerable<TInput> input);
}
```

**Example:**
```csharp
public class PreprocessCompaniesNode : Node<CompanyRawSchema, CompanySchema>
{
    protected override Task<IEnumerable<CompanySchema>> Transform(
        IEnumerable<CompanyRawSchema> input)
    {
        var processed = input.Select(company => new CompanySchema
        {
            Id = company.Id,
            CompanyRating = ParsePercentage(company.CompanyRating),
            // ... more transformations
        });
        
        return Task.FromResult(processed);
    }
}
```

---

#### Single-Input, Single-Output Node with Parameters

```csharp
public abstract class Node<TInput, TOutput, TParameters>
{
    public TParameters Parameters { get; set; } = new();
    
    protected abstract Task<IEnumerable<TOutput>> Transform(
        IEnumerable<TInput> input);
}
```

**Example:**
```csharp
public class SplitDataNode : Node<ModelInputSchema, SplitDataOutputs, ModelOptions>
{
    // Parameters property inherited: public ModelOptions Parameters { get; set; }
    
    protected override Task<IEnumerable<SplitDataOutputs>> Transform(
        IEnumerable<ModelInputSchema> input)
    {
        var testSize = Parameters.TestSize;  // Access parameters
        var randomState = Parameters.RandomState;
        
        // ... perform split
    }
}
```

---

#### Multi-Input, Single-Output Node (2 Inputs)

```csharp
public abstract class Node<TInput1, TInput2, TOutput>
{
    protected abstract Task<IEnumerable<TOutput>> Transform(
        IEnumerable<IEnumerable<TInput1>> input1,
        IEnumerable<IEnumerable<TInput2>> input2);
}
```

**Example:**
```csharp
public class TrainModelNode : Node<IEnumerable<FeatureRow>, IEnumerable<decimal>, ITransformer>
{
    protected override Task<IEnumerable<ITransformer>> Transform(
        IEnumerable<IEnumerable<FeatureRow>> xTrain,
        IEnumerable<IEnumerable<decimal>> yTrain)
    {
        var features = xTrain.Single();
        var targets = yTrain.Single();
        
        // ... train model
        return Task.FromResult(new[] { model }.AsEnumerable());
    }
}
```

---

#### Multi-Input, Single-Output Node (3+ Inputs)

```csharp
public abstract class Node<TInput1, TInput2, TInput3, TOutput>
{
    protected abstract Task<IEnumerable<TOutput>> Transform(
        IEnumerable<TInput1> input1,
        IEnumerable<IEnumerable<TInput2>> input2,
        IEnumerable<IEnumerable<TInput3>> input3);
}
```

**Example:**
```csharp
public class EvaluateModelNode : Node<ITransformer, IEnumerable<FeatureRow>, IEnumerable<decimal>, ModelMetrics>
{
    public ILogger<EvaluateModelNode>? Logger { get; set; }  // Property injection
    
    protected override Task<IEnumerable<ModelMetrics>> Transform(
        IEnumerable<ITransformer> models,
        IEnumerable<IEnumerable<FeatureRow>> xTest,
        IEnumerable<IEnumerable<decimal>> yTest)
    {
        var model = models.Single();
        var features = xTest.Single();
        var targets = yTest.Single();
        
        // ... evaluate
        Logger?.LogInformation("R² Score: {R2}", metrics.R2Score);
        
        return Task.FromResult(new[] { metrics }.AsEnumerable());
    }
}
```

---

### 2.2 Node Artifact Colocation Pattern

**Policy:** Node-specific artifacts (parameters, output schemas) are colocated with the node class.

This follows the **React Props pattern** where component-specific types live with the component.

```csharp
// In: Pipelines/DataScience/Nodes/SplitDataNode.cs

public class SplitDataNode : Node<ModelInputSchema, SplitDataOutputs, ModelOptions>
{
    // Node implementation...
}

#region Node Artifacts (Colocated)

/// <summary>
/// Multi-output schema for train/test split operation.
/// </summary>
public record SplitDataOutputs
{
    public required IEnumerable<FeatureRow> XTrain { get; init; }
    public required IEnumerable<FeatureRow> XTest { get; init; }
    public required IEnumerable<decimal> YTrain { get; init; }
    public required IEnumerable<decimal> YTest { get; init; }
}

/// <summary>
/// Parameters for model training.
/// </summary>
public record ModelOptions
{
    public double TestSize { get; init; } = 0.2;
    public int RandomState { get; init; } = 3;
    public List<string> Features { get; init; } = new() { /* ... */ };
}

#endregion
```

**Rationale:**
- Node artifacts are only used by that specific node
- Keeps related types together (locality of behavior)
- Easy to understand node's complete contract
- Reduces cognitive overhead (no hunting across directories)

**Contrast with Domain Schemas:**
- Domain schemas in `Data/Schemas/` are reusable across nodes
- Node artifacts in `Pipelines/{Pipeline}/Nodes/` are node-specific

---

### 2.3 Node Constructor Pattern

**Rule:** All nodes MUST have parameterless constructors.

**Reason:** Enables type reference instantiation for distributed/parallel execution.

#### ✅ Correct: Property Injection

```csharp
public class EvaluateModelNode : Node<...>
{
    public ILogger<EvaluateModelNode>? Logger { get; set; }  // Property injection
    
    // Implicit parameterless constructor
}
```

#### ❌ Incorrect: Constructor Injection

```csharp
public class EvaluateModelNode : Node<...>
{
    private readonly ILogger _logger;
    
    public EvaluateModelNode(ILogger<EvaluateModelNode> logger)  // ❌ No parameterless constructor!
    {
        _logger = logger;
    }
}
```

**Instantiation:**
```csharp
// Framework can create nodes via type reference
var node = Activator.CreateInstance<SplitDataNode>();

// Or via compiled expressions for better performance
var factory = Expression.Lambda<Func<SplitDataNode>>(
    Expression.New(typeof(SplitDataNode))).Compile();
var node = factory();
```

---

### 2.4 Multi-Output Nodes

Multi-output nodes return a single schema object that groups multiple outputs. The pipeline uses `OutputMapping<T>` to unpack properties into separate catalog entries.

#### Step 1: Define Output Schema (Colocated with Node)

```csharp
public record SplitDataOutputs
{
    public required IEnumerable<FeatureRow> XTrain { get; init; }
    public required IEnumerable<FeatureRow> XTest { get; init; }
    public required IEnumerable<decimal> YTrain { get; init; }
    public required IEnumerable<decimal> YTest { get; init; }
}
```

#### Step 2: Node Returns Output Schema

```csharp
public class SplitDataNode : Node<ModelInputSchema, SplitDataOutputs, ModelOptions>
{
    protected override Task<IEnumerable<SplitDataOutputs>> Transform(...)
    {
        var outputs = new SplitDataOutputs
        {
            XTrain = trainData,
            XTest = testData,
            YTrain = trainTargets,
            YTest = testTargets
        };
        
        return Task.FromResult(new[] { outputs }.AsEnumerable());
    }
}
```

#### Step 3: Pipeline Maps Properties to Catalog (See Section 3.2)

---

### 2.5 Node Unit Testing

Nodes are pure functions that can be tested in isolation without pipeline infrastructure.

```csharp
[TestFixture]
public class SplitDataNodeTests
{
    [Test]
    public void Transform_ShouldSplitDataCorrectly()
    {
        // Arrange
        var node = new SplitDataNode
        {
            Parameters = new ModelOptions
            {
                TestSize = 0.2,
                RandomState = 42
            }
        };

        var inputData = new[]
        {
            new ModelInputSchema { /* ... */ }
        };

        // Act
        var result = await node.Transform(inputData);
        var output = result.Single();

        // Assert
        Assert.That(output.XTrain, Is.Not.Empty);
        Assert.That(output.XTest, Is.Not.Empty);
        Assert.That(output.XTrain.Count() + output.XTest.Count(), 
                    Is.EqualTo(inputData.Length));
    }
}
```

**Benefits:**
- Fast execution (no I/O, no pipeline overhead)
- Focused testing (single node logic)
- Easy to set up (just instantiate node and call Transform)
- Parameterless constructor makes instantiation trivial

---

## 3. Pipeline

Pipelines orchestrate nodes into directed acyclic graphs (DAGs) with compile-time type safety.

### 3.1 Pipeline Definition

```csharp
public static class DataSciencePipeline
{
    public static Pipeline Create(SpaceflightsCatalog catalog, ModelOptions? options = null)
    {
        return PipelineBuilder.CreatePipeline(pipeline =>
        {
            // Add nodes with type-safe catalog references
        });
    }
}
```

---

### 3.2 Adding Nodes to Pipeline

#### Single-Input, Multi-Output Node

```csharp
// Create type-safe output mapping
var splitOutputs = new OutputMapping<SplitDataOutputs>();
splitOutputs.Add(s => s.XTrain, catalog.XTrain);  // ✅ Compile-time type check
splitOutputs.Add(s => s.XTest, catalog.XTest);
splitOutputs.Add(s => s.YTrain, catalog.YTrain);
splitOutputs.Add(s => s.YTest, catalog.YTest);

pipeline.AddNode<SplitDataNode>(
    input: catalog.ModelInputTable,      // ✅ Must be ICatalogEntry<ModelInputSchema>
    outputMapping: splitOutputs,
    name: "split_data_node",
    configureNode: node => node.Parameters = options ?? new ModelOptions()
);
```

**Compile-Time Validation:**
- `catalog.ModelInputTable` type must match `SplitDataNode`'s `TInput`
- Each `OutputMapping` property type must match its catalog entry type
- Wrong types = **compilation error**

---

#### Multi-Input (2), Single-Output Node

```csharp
pipeline.AddNode<TrainModelNode>(
    inputs: (catalog.XTrain, catalog.YTrain),  // ✅ Tuple type-checked
    output: catalog.Regressor,                  // ✅ Must be ICatalogEntry<ITransformer>
    name: "train_model_node"
);
```

**Compile-Time Validation:**
- Tuple types must match `TrainModelNode`'s input types exactly
- `catalog.Regressor` type must match `TrainModelNode`'s `TOutput`

---

#### Multi-Input (3), Single-Output Node

```csharp
pipeline.AddNode<EvaluateModelNode>(
    inputs: (catalog.Regressor, catalog.XTest, catalog.YTest),
    output: catalog.ModelMetrics,
    name: "evaluate_model_node",
    configureNode: node => node.Logger = logger  // Optional dependency injection
);
```

---

### 3.3 OutputMapping<T>

Type-safe mapping from multi-output node properties to catalog entries.

```csharp
public class OutputMapping<TOutput>
{
    public void Add<TProp>(
        Expression<Func<TOutput, TProp>> propertySelector,
        ICatalogEntry<TProp> catalogEntry);
}
```

**Generic Constraint:** `TProp` (property type) MUST match catalog entry's type parameter.

**Example (Correct):**
```csharp
var mapping = new OutputMapping<SplitDataOutputs>();
mapping.Add(s => s.XTrain, catalog.XTrain);  
// ✅ Both are IEnumerable<FeatureRow>
```

**Example (Compile Error):**
```csharp
var mapping = new OutputMapping<SplitDataOutputs>();
mapping.Add(s => s.XTrain, catalog.Regressor);  
// ❌ Compile error: Cannot convert ICatalogEntry<ITransformer> 
//    to ICatalogEntry<IEnumerable<FeatureRow>>
```

**Benefits:**
- Expression-based property selectors (refactor-safe)
- IntelliSense support for properties
- Go To Definition works
- Rename refactoring automatic

---

### 3.4 Pipeline Execution

```csharp
// Build catalog
var catalog = SpaceflightsCatalog.Build();

// Create pipeline with parameters
var pipeline = DataSciencePipeline.Create(
    catalog, 
    new ModelOptions { TestSize = 0.2, RandomState = 42 });

// Execute pipeline (API TBD - placeholder)
await pipeline.Run();
```

**Status:** Execution engine not yet implemented. Design goals:
- Parallel execution where possible (DAG analysis)
- Progress reporting
- Error handling with partial rollback
- Caching/memoization of intermediate results

---

### 3.5 Complete Pipeline Example

```csharp
public static class DataSciencePipeline
{
    public static Pipeline Create(SpaceflightsCatalog catalog, ModelOptions? options = null)
    {
        return PipelineBuilder.CreatePipeline(pipeline =>
        {
            // Node 1: Split data (1 input → 4 outputs)
            var splitOutputs = new OutputMapping<SplitDataOutputs>();
            splitOutputs.Add(s => s.XTrain, catalog.XTrain);
            splitOutputs.Add(s => s.XTest, catalog.XTest);
            splitOutputs.Add(s => s.YTrain, catalog.YTrain);
            splitOutputs.Add(s => s.YTest, catalog.YTest);
            
            pipeline.AddNode<SplitDataNode>(
                input: catalog.ModelInputTable,
                outputMapping: splitOutputs,
                name: "split_data",
                configureNode: node => node.Parameters = options ?? new ModelOptions()
            );

            // Node 2: Train model (2 inputs → 1 output)
            pipeline.AddNode<TrainModelNode>(
                inputs: (catalog.XTrain, catalog.YTrain),
                output: catalog.Regressor,
                name: "train_model"
            );

            // Node 3: Evaluate model (3 inputs → 1 output)
            pipeline.AddNode<EvaluateModelNode>(
                inputs: (catalog.Regressor, catalog.XTest, catalog.YTest),
                output: catalog.ModelMetrics,
                name: "evaluate_model"
            );
        });
    }
}
```

**Compile-Time Guarantees:**
- All catalog references exist (no typos)
- All type contracts match (inputs/outputs compatible)
- All multi-output mappings are type-correct
- **If it compiles, it's correctly wired** ✅

---

## 4. Meta (Configuration & Registration)

### 4.1 Pipeline Registry

Central registry for all pipelines in a project.

```csharp
public static class PipelineRegistry
{
    public static Dictionary<string, Pipeline> RegisterPipelines(
        SpaceflightsCatalog catalog,
        ModelOptions? modelOptions = null)
    {
        return new Dictionary<string, Pipeline>
        {
            ["data_processing"] = DataProcessingPipeline.Create(catalog),
            ["data_science"] = DataSciencePipeline.Create(catalog, modelOptions),
            ["__default__"] = CombinePipelines(catalog, /* all pipelines */)
        };
    }
}
```

**Usage:**
```csharp
var catalog = SpaceflightsCatalog.Build();
var pipelines = PipelineRegistry.RegisterPipelines(catalog);

// Run specific pipeline
await pipelines["data_science"].Run();

// Run default pipeline (all pipelines in sequence)
await pipelines["__default__"].Run();
```

---

### 4.2 Project Structure

```
Flowthru.Spaceflights/
├── Data/
│   ├── Catalog.cs (legacy - deprecated)
│   ├── SpaceflightsCatalog.cs ✅ Typed catalog
│   ├── ICatalogEntry.cs
│   ├── MemoryCatalogEntry.cs
│   ├── CsvCatalogEntry.cs
│   ├── ParquetCatalogEntry.cs
│   ├── ExcelCatalogEntry.cs
│   ├── Datasets/                      # Data files
│   │   ├── 01_Raw/
│   │   ├── 02_Intermediate/
│   │   ├── 03_Primary/
│   │   ├── 06_Models/
│   │   └── 08_Reporting/
│   └── Schemas/
│       ├── Raw/                       # External data formats
│       ├── Processed/                 # Domain models
│       └── Models/                    # ML-specific schemas
├── Pipelines/
│   ├── Pipeline.cs                    # Core pipeline types
│   ├── OutputMapping.cs               # Multi-output support
│   ├── PipelineRegistry.cs            # Central registry
│   ├── DataProcessing/
│   │   ├── DataProcessingPipeline.cs
│   │   └── Nodes/
│   │       ├── PreprocessCompaniesNode.cs
│   │       ├── PreprocessShuttlesNode.cs
│   │       └── CreateModelInputNode.cs
│   ├── DataScience/
│   │   ├── DataSciencePipeline.cs
│   │   └── Nodes/
│   │       ├── SplitDataNode.cs       # + SplitDataOutputs + ModelOptions
│   │       ├── TrainModelNode.cs
│   │       └── EvaluateModelNode.cs
│   └── Reporting/
│       └── (future pipelines)
├── Tests/
│   └── Pipelines/
│       ├── DataProcessing/
│       │   └── PreprocessCompaniesNodeTests.cs
│       └── DataScience/
│           ├── SplitDataNodeTests.cs
│           ├── DataSciencePipelineTests.cs
│           └── (more tests)
└── Config/
    └── (future: parameters, logging config, etc.)
```

---

### 4.3 Configuration Philosophy

**Current State:** Configuration is code-based (C# builder pattern).

**Rationale:**
- Compile-time type safety
- IntelliSense support
- Refactoring safety
- Early failure (misconfiguration won't compile)

**Future Consideration:** Hybrid approach with source generators
- YAML/JSON for data scientists
- Generate typed catalog classes from config
- Best of both worlds: flexibility + safety

---

### 4.4 Dependency Injection

**Pattern:** Property injection for optional dependencies.

```csharp
public class EvaluateModelNode : Node<...>
{
    public ILogger<EvaluateModelNode>? Logger { get; set; }
    
    protected override Task<...> Transform(...)
    {
        Logger?.LogInformation("Evaluating model...");
    }
}
```

**Configuration:**
```csharp
pipeline.AddNode<EvaluateModelNode>(
    inputs: (...),
    output: catalog.ModelMetrics,
    name: "evaluate",
    configureNode: node =>
    {
        node.Logger = serviceProvider.GetService<ILogger<EvaluateModelNode>>();
    }
);
```

**Why Property Injection:**
- Maintains parameterless constructor requirement
- Optional dependencies (nullable)
- Compatible with distributed execution

---

### 4.5 Testing Strategy

#### Unit Tests (Nodes)
- Test node logic in isolation
- Fast, no I/O
- Located in `Tests/Pipelines/{Pipeline}/`

```csharp
[Test]
public void PreprocessCompanies_ShouldConvertPercentages()
{
    var node = new PreprocessCompaniesNode();
    var input = new[] { new CompanyRawSchema { CompanyRating = "100%" } };
    
    var result = await node.Transform(input);
    
    Assert.That(result.Single().CompanyRating, Is.EqualTo(1.0m));
}
```

#### Integration Tests (Pipelines)
- Test full pipeline execution with catalog
- Validates node wiring and data flow
- Uses in-memory catalog entries for speed

```csharp
[Test]
public async Task DataSciencePipeline_ShouldProduceModelMetrics()
{
    var catalog = SpaceflightsCatalog.Build();
    // ... populate catalog with test data
    
    var pipeline = DataSciencePipeline.Create(catalog);
    await pipeline.Run();
    
    var metrics = await catalog.ModelMetrics.Load();
    Assert.That(metrics.R2Score, Is.GreaterThan(0));
}
```

---

### 4.6 Design Principles Summary

1. **Compile-Time Type Safety**
   - Misconfigured pipelines won't compile
   - Type contracts enforced by compiler
   - Zero runtime type validation needed

2. **Fail Fast**
   - Errors at compile-time, not runtime
   - Cannot deploy broken pipelines
   - Early feedback during development

3. **Developer Experience**
   - Full IntelliSense support
   - Refactoring tools work seamlessly
   - Self-documenting types
   - Minimal ceremony

4. **Separation of Concerns**
   - Domain schemas in `Data/Schemas/`
   - Node artifacts colocated with nodes
   - Pipeline orchestration separate from node logic
   - Clear data flow (DAG structure)

5. **Testability**
   - Nodes are pure functions
   - Easy unit testing without infrastructure
   - Integration tests validate wiring
   - Fast test execution

6. **Railway-Oriented Programming**
   - Uses `Either<L,R>` from LanguageExt (future)
   - Explicit error handling
   - Composable transformations

7. **Distributed Execution Ready**
   - Parameterless constructors
   - Type reference instantiation
   - Stateless nodes
   - Parallel-safe by design

---

## Appendix A: Key Design Decisions

### Why Typed Catalogs over String Keys?

**Problem:** String keys (`"x_train"`) cause:
- Runtime errors for typos
- No IntelliSense
- No refactoring support
- No type information

**Solution:** Typed properties (`catalog.XTrain`)
- Compile-time validation
- Full IDE support
- Type information preserved
- Refactoring-safe

**Trade-off:** Less dynamic, requires typed catalog class per project.

**Verdict:** Type safety wins for enterprise data engineering.

---

### Why Colocate Node Artifacts?

**Problem:** Separate directories for parameters/output schemas:
- Cognitive overhead (hunt across directories)
- Unclear which artifacts belong to which node
- Artificial separation of related concerns

**Solution:** Colocate with node (React Props pattern)
- Locality of behavior
- Clear ownership
- Easy to understand complete node contract

**Verdict:** Developer ergonomics improved significantly.

---

### Why Parameterless Constructors?

**Problem:** Constructor injection prevents:
- Type reference instantiation (`Activator.CreateInstance<T>()`)
- Distributed/parallel execution (serialization)
- Generic factory patterns

**Solution:** Property injection for dependencies
- Maintains parameterless constructor
- Optional dependencies (nullable)
- Compatible with all instantiation patterns

**Verdict:** Flexibility for distributed execution worth the trade-off.

---

### Why OutputMapping<T> over Attributes?

**Problem:** Attributes on output schema properties (`[CatalogOutput("x_train")]`):
- Couples schema to catalog (violates separation of concerns)
- Schema is no longer "pure" data structure
- Attributes are stringly-typed

**Solution:** Expression-based `OutputMapping<T>`
- Schema remains pure
- Mapping defined at pipeline level
- Compile-time type checking
- Refactoring-safe

**Verdict:** Maintains schema purity while enabling type safety.

---

## Appendix B: Future Enhancements

1. **Pipeline Execution Engine**
   - Parallel execution (DAG analysis)
   - Progress reporting
   - Caching/memoization
   - Error recovery

2. **Roslyn Analyzers**
   - Custom compile-time checks
   - Enforce conventions (naming, structure)
   - Additional invariants

3. **Monitoring & Observability**
   - OpenTelemetry integration
   - Metrics and traces
   - Performance profiling

4. **Visualization**
   - Generate pipeline diagrams from code
   - Interactive DAG explorer
   - Data lineage tracking

5. **Distributed Execution**
   - Serialize node configurations
   - Remote execution support
   - Cloud integration (Azure, AWS)

6. **Configuration System**
   - YAML/JSON support
   - Environment-specific configs
   - Parameter validation
   - Auto-generate typed catalogs from YAML
   - Best of both worlds: config files + type safety
