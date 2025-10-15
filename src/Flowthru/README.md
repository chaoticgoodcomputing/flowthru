# Flowthru

A type-safe data engineering framework for .NET inspired by Kedro's declarative pipelines.

**Version:** 0.1.0 (Alpha - Foundational Architecture)  
**Status:** Active Development

---

## Table of Contents

- [Getting Started](#getting-started)
- [How-To Guides](#how-to-guides)
- [API Reference](#api-reference)
- [Architecture](#architecture)

---

## Getting Started

**Tutorial: Build Your First Data Pipeline**

In this tutorial, we'll create a simple data processing pipeline that reads CSV data, transforms it, and saves the results. This demonstrates Flowthru's core concepts: catalog entries, nodes, and pipelines.

### Prerequisites

- .NET 8.0 or later
- Basic understanding of async/await in C#
- Familiarity with data processing concepts

### Step 1: Install Flowthru

Add Flowthru to your project:

```bash
dotnet add package Flowthru
```

### Step 2: Define Your Data Schemas

Create schema classes for your data. Use `record` for immutability:

```csharp
// Raw input schema
public record CompanyRawData
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Rating { get; init; } // e.g., "85%"
}

// Processed output schema
public record CompanyProcessed
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required decimal Rating { get; init; } // Converted to 0.85
}
```

### Step 3: Create a Data Catalog

The catalog defines where your data lives:

```csharp
using Flowthru.Data;
using Flowthru.Data.Implementations;

var catalog = DataCatalogBuilder.BuildCatalog(builder =>
{
    builder.Register("raw_companies",
        new CsvCatalogEntry<CompanyRawData>("companies", "data/companies.csv"));
    
    builder.Register("processed_companies",
        new CsvCatalogEntry<CompanyProcessed>("processed", "data/processed.csv"));
});
```

You now have a catalog with two entries. Let's create a node to transform the data.

### Step 4: Create a Transformation Node

Nodes are pure transformation functions. Create a class that inherits from `NodeBase<TInput, TOutput>`:

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
        var number = percentage.TrimEnd('%');
        return decimal.Parse(number) / 100m;
    }
}
```

Your node is ready. Notice:
- ✅ Parameterless constructor (required)
- ✅ Strongly-typed input/output (compile-time safety)
- ✅ Pure transformation logic (no side effects)

### Step 5: Build and Run the Pipeline

*(Note: Pipeline builder and executor are coming in the next implementation phase. This shows the intended API.)*

```csharp
using Flowthru.Pipelines;

var pipeline = PipelineBuilder.CreatePipeline(builder =>
{
    builder.AddNode<ProcessCompaniesNode>(
        input: catalog.Get<IEnumerable<CompanyRawData>>("raw_companies"),
        output: catalog.Get<IEnumerable<CompanyProcessed>>("processed_companies"),
        name: "process_companies"
    );
});

// Execute the pipeline
var result = await pipeline.ExecuteAsync();

Console.WriteLine($"Pipeline completed in {result.Duration}");
```

### What You've Learned

You've successfully:
- Created data schemas using C# records
- Built a data catalog with CSV entries
- Implemented a transformation node
- Understood the basic pipeline structure

**Next Steps:** See [How-To Guides](#how-to-guides) for multi-input nodes, parameters, and advanced scenarios.

---

## How-To Guides

### How to Work with Multi-Input Nodes

**Problem:** Your node needs data from multiple catalog entries (e.g., joining two datasets).

**Solution:** Create an input schema and use `CatalogMap<T>` to map catalog entries to properties.

#### Step 1: Define Input Schema

```csharp
using System.ComponentModel.DataAnnotations;

public record JoinInputs
{
    [Required]
    public required IEnumerable<CompanyData> Companies { get; init; }
    
    [Required]
    public required IEnumerable<ReviewData> Reviews { get; init; }
}
```

The `[Required]` attribute enables compile-time validation that all properties are mapped.

#### Step 2: Create Node Using Input Schema

```csharp
public class JoinCompaniesAndReviewsNode 
    : NodeBase<JoinInputs, EnrichedCompanyData>
{
    protected override Task<IEnumerable<EnrichedCompanyData>> Transform(
        IEnumerable<JoinInputs> inputs)
    {
        var input = inputs.Single(); // Input schemas are always singletons
        
        var joined = from company in input.Companies
                     join review in input.Reviews 
                         on company.Id equals review.CompanyId
                     select new EnrichedCompanyData
                     {
                         Id = company.Id,
                         Name = company.Name,
                         AverageRating = review.Rating
                     };
        
        return Task.FromResult(joined);
    }
}
```

#### Step 3: Map Catalog Entries to Input Properties

```csharp
using Flowthru.Pipelines.Mapping;

var inputMap = new CatalogMap<JoinInputs>();
inputMap.Map(i => i.Companies, catalog.Get<IEnumerable<CompanyData>>("companies"));
inputMap.Map(i => i.Reviews, catalog.Get<IEnumerable<ReviewData>>("reviews"));

pipeline.AddNode<JoinCompaniesAndReviewsNode>(
    inputMap: inputMap,
    output: catalog.Get<IEnumerable<EnrichedCompanyData>>("enriched_companies"),
    name: "join_companies_reviews"
);
```

**Key Points:**
- Use `CatalogMap<T>.Map()` to connect catalog entries to input properties
- Expression-based selectors provide IntelliSense and refactoring safety
- Validation occurs at pipeline build time (fail-fast before execution)

---

### How to Use Parameters in Nodes

**Problem:** Your node needs configuration values (e.g., test split ratio, random seed).

**Solution:** Add parameters to your input schema and use `MapParameter()`.

#### Step 1: Define Input Schema with Parameters

```csharp
public record SplitDataInputs
{
    [Required]
    public required IEnumerable<FeatureData> Data { get; init; }
    
    [Required]
    public required ModelOptions Options { get; init; } // Parameter
}

public record ModelOptions
{
    public double TestSize { get; init; } = 0.2;
    public int RandomState { get; init; } = 42;
}
```

#### Step 2: Create Node Using Parameters

```csharp
public class SplitDataNode : NodeBase<SplitDataInputs, SplitDataOutputs>
{
    protected override Task<IEnumerable<SplitDataOutputs>> Transform(
        IEnumerable<SplitDataInputs> inputs)
    {
        var input = inputs.Single();
        var data = input.Data.ToList();
        var options = input.Options;
        
        // Use options.TestSize and options.RandomState
        var random = new Random(options.RandomState);
        var shuffled = data.OrderBy(_ => random.Next()).ToList();
        
        var splitIndex = (int)(shuffled.Count * options.TestSize);
        var trainData = shuffled.Take(splitIndex);
        var testData = shuffled.Skip(splitIndex);
        
        return Task.FromResult(new[]
        {
            new SplitDataOutputs
            {
                TrainData = trainData,
                TestData = testData
            }
        }.AsEnumerable());
    }
}
```

#### Step 3: Map Parameter Values

```csharp
var modelOptions = new ModelOptions
{
    TestSize = 0.2,
    RandomState = 42
};

var inputMap = new CatalogMap<SplitDataInputs>();
inputMap.Map(i => i.Data, catalog.Get<IEnumerable<FeatureData>>("features"));
inputMap.MapParameter(i => i.Options, modelOptions); // Constant value

pipeline.AddNode<SplitDataNode>(
    inputMap: inputMap,
    outputMap: splitOutputMap,
    name: "split_data"
);
```

**Key Points:**
- `Map()` connects to catalog entries (data)
- `MapParameter()` provides constant values (configuration)
- Parameters are input-only (cannot be used in output mappings)

---

### How to Create Multi-Output Nodes

**Problem:** Your node produces multiple outputs that go to different catalog entries.

**Solution:** Create an output schema and use `CatalogMap<T>` to map properties to catalog entries.

#### Step 1: Define Output Schema

```csharp
public record SplitDataOutputs
{
    [Required]
    public required IEnumerable<FeatureData> TrainData { get; init; }
    
    [Required]
    public required IEnumerable<FeatureData> TestData { get; init; }
}
```

#### Step 2: Map Output Properties to Catalog Entries

```csharp
var outputMap = new CatalogMap<SplitDataOutputs>();
outputMap.Map(o => o.TrainData, catalog.Get<IEnumerable<FeatureData>>("train_data"));
outputMap.Map(o => o.TestData, catalog.Get<IEnumerable<FeatureData>>("test_data"));

pipeline.AddNode<SplitDataNode>(
    input: catalog.Get<IEnumerable<FeatureData>>("all_data"),
    outputMap: outputMap,
    name: "split_data"
);
```

**Key Points:**
- Output schemas work symmetrically with input schemas
- Each property in the output schema maps to one catalog entry
- Multiple outputs enable downstream nodes to reference data independently

---

### How to Inject Dependencies into Nodes

**Problem:** Your node needs access to a logger, database connection, or other services.

**Solution:** Use property injection (not constructor injection).

#### Step 1: Add Properties to Your Node

```csharp
using Microsoft.Extensions.Logging;

public class ProcessDataNode : NodeBase<InputData, OutputData>
{
    // Optional dependencies via property injection
    public ILogger<ProcessDataNode>? Logger { get; set; }
    public IMyService? MyService { get; set; }
    
    protected override async Task<IEnumerable<OutputData>> Transform(
        IEnumerable<InputData> input)
    {
        Logger?.LogInformation("Starting data processing");
        
        var result = await MyService?.ProcessAsync(input) 
                     ?? throw new InvalidOperationException("MyService not injected");
        
        Logger?.LogInformation("Completed processing {Count} records", result.Count());
        return result;
    }
}
```

#### Step 2: Inject Dependencies During Pipeline Configuration

```csharp
using Microsoft.Extensions.DependencyInjection;
using Serilog;

// Set up dependency injection
var services = new ServiceCollection();
services.AddLogging(builder => builder.AddSerilog());
services.AddSingleton<IMyService, MyService>();
var serviceProvider = services.BuildServiceProvider();

// Inject dependencies when adding node
pipeline.AddNode<ProcessDataNode>(
    input: catalog.Get<IEnumerable<InputData>>("input"),
    output: catalog.Get<IEnumerable<OutputData>>("output"),
    name: "process_data",
    configure: node =>
    {
        node.Logger = serviceProvider.GetRequiredService<ILogger<ProcessDataNode>>();
        node.MyService = serviceProvider.GetRequiredService<IMyService>();
    }
);
```

**Why Property Injection?**
- Maintains parameterless constructor requirement (enables factory instantiation)
- Compatible with distributed/parallel execution scenarios
- Optional dependencies (nullable properties)

---

### How to Choose Catalog Entry Types

**Problem:** Which catalog entry type should I use for my data?

**Decision Guide:**

| Use Case | Catalog Entry Type | Notes |
|----------|-------------------|-------|
| Raw CSV input data | `CsvCatalogEntry<T>` | Best for external data sources, human-readable |
| Intermediate processed data | `ParquetCatalogEntry<T>` | Columnar format, good compression, fast reads |
| Large feature tables | `ParquetCatalogEntry<T>` | Efficient for analytics workloads |
| Excel input files | `ExcelCatalogEntry<T>` | Read-only, use for business-provided data |
| Temporary pipeline data | `MemoryCatalogEntry<T>` | No disk I/O, lost when app terminates |
| ML models | `MemoryCatalogEntry<ITransformer>` | Keep in memory during training/evaluation |
| Output reports | `CsvCatalogEntry<T>` | Human-readable output format |

**Example: Choosing Entries for a Data Science Pipeline**

```csharp
var catalog = DataCatalogBuilder.BuildCatalog(builder =>
{
    // 01_Raw: CSV for external data
    builder.Register("raw_data", 
        new CsvCatalogEntry<RawData>("raw", "data/01_raw/input.csv"));
    
    // 02_Intermediate: Parquet for processed data
    builder.Register("processed_data",
        new ParquetCatalogEntry<ProcessedData>("processed", "data/02_intermediate/processed.parquet"));
    
    // 03_Primary: Parquet for feature table
    builder.Register("features",
        new ParquetCatalogEntry<FeatureRow>("features", "data/03_primary/features.parquet"));
    
    // Training data: Memory (temporary)
    builder.Register("train_data", new MemoryCatalogEntry<FeatureRow>("train"));
    builder.Register("test_data", new MemoryCatalogEntry<FeatureRow>("test"));
    
    // Model: Memory
    builder.Register("model", new MemoryCatalogEntry<ITransformer>("model"));
    
    // Metrics: CSV for reporting
    builder.Register("metrics",
        new CsvCatalogEntry<ModelMetrics>("metrics", "data/08_reporting/metrics.csv"));
});
```

---

## API Reference

### Core Interfaces and Classes

#### `ICatalogEntry<T>`

Represents a typed storage location for data.

**Methods:**
- `Task<T> Load()` - Loads data from storage
- `Task Save(T data)` - Saves data to storage
- `Task<bool> Exists()` - Checks if data exists

**Implementations:**
- `MemoryCatalogEntry<T>` - In-memory storage
- `CsvCatalogEntry<T>` - CSV file storage
- `ParquetCatalogEntry<T>` - Parquet file storage
- `ExcelCatalogEntry<T>` - Excel file storage (read-only)

---

#### `NodeBase<TInput, TOutput>`

Abstract base class for transformation nodes.

**Generic Parameters:**
- `TInput` - Input data type or input schema type
- `TOutput` - Output data type or output schema type

**Properties:**
- `ILogger? Logger` - Optional logger for observability

**Abstract Method:**
- `protected abstract Task<IEnumerable<TOutput>> Transform(IEnumerable<TInput> input)`

**Requirements:**
- Must have parameterless constructor
- Should be pure function (minimize side effects)
- Return `Task.FromResult()` for synchronous operations

---

#### `CatalogMap<T>`

Bidirectional mapping between catalog entries and schema properties.

**Generic Constraint:**
- `T : new()` - Type must have parameterless constructor

**Methods:**
- `void Map<TProp>(Expression<Func<T, TProp>> propertySelector, ICatalogEntry<TProp> catalogEntry)` - Maps catalog entry to property
- `void MapParameter<TProp>(Expression<Func<T, TProp>> propertySelector, TProp value)` - Maps constant value to property (input only)
- `void ValidateComplete()` - Validates all `[Required]` properties are mapped
- `static CatalogMap<T> FromEntry(ICatalogEntry<T> entry)` - Creates pass-through map for single entry

**Modes:**
- **Pass-through mode:** Direct wrapper for single catalog entry (simple nodes)
- **Mapped mode:** Property-based mapping for multi-input/output nodes

---

#### `DataCatalog`

Central registry for catalog entries.

**Methods:**
- `void Register<T>(string key, ICatalogEntry<T> entry)` - Registers a catalog entry
- `ICatalogEntry<T> Get<T>(string key)` - Retrieves typed catalog entry
- `bool Contains(string key)` - Checks if entry exists

---

#### `DataCatalogBuilder`

Fluent builder for constructing catalogs.

**Static Method:**
- `static DataCatalog BuildCatalog(Action<DataCatalogBuilder> configure)` - Builds catalog with configuration action

**Instance Methods:**
- `DataCatalogBuilder Register<T>(string key, ICatalogEntry<T> entry)` - Registers entry (chainable)

---

### PipelineBuilder Overloads

*(Coming in next implementation phase)*

Four overloads for different input/output combinations:

1. **Single input, single output:**
   ```csharp
   AddNode<TNode, TInput, TOutput>(
       ICatalogEntry<TInput> input,
       ICatalogEntry<TOutput> output,
       string name,
       Action<TNode>? configure = null)
   ```

2. **Multi input, single output:**
   ```csharp
   AddNode<TNode, TInput, TOutput>(
       CatalogMap<TInput> inputMap,
       ICatalogEntry<TOutput> output,
       string name,
       Action<TNode>? configure = null)
   ```

3. **Single input, multi output:**
   ```csharp
   AddNode<TNode, TInput, TOutput>(
       ICatalogEntry<TInput> input,
       CatalogMap<TOutput> outputMap,
       string name,
       Action<TNode>? configure = null)
   ```

4. **Multi input, multi output:**
   ```csharp
   AddNode<TNode, TInput, TOutput>(
       CatalogMap<TInput> inputMap,
       CatalogMap<TOutput> outputMap,
       string name,
       Action<TNode>? configure = null)
   ```

---

## Architecture

### Design Philosophy

Flowthru is built on a single core principle: **Fail at compile-time, not runtime.**

Every design decision prioritizes compile-time safety:

- ✅ Type mismatches caught by C# compiler via generic constraints
- ✅ Property existence validated via expression trees
- ✅ Refactoring safety through strongly-typed references
- ⚠️ Mapping completeness validated at pipeline build time (Phase 1)
- 🔮 Roslyn analyzer for true compile-time completeness validation (Phase 2 - future)

---

### Architectural Spaces

Flowthru is organized into four foundational spaces:

#### 1. Data Space

**Purpose:** Abstract storage strategies and provide typed access to data.

**Design Patterns:**
- **Strategy Pattern:** Different storage implementations (CSV, Parquet, Memory, Excel)
- **Repository Pattern:** `ICatalogEntry<T>` as typed repository interface
- **Registry Pattern:** `DataCatalog` maintains centralized catalog of entries

**Key Classes:**
- `ICatalogEntry<T>` - Storage abstraction
- `DataCatalog` - Entry registry
- `MemoryCatalogEntry<T>`, `CsvCatalogEntry<T>`, etc. - Concrete strategies

---

#### 2. Nodes Space

**Purpose:** Pure transformation functions with type safety.

**Design Patterns:**
- **Template Method Pattern:** `NodeBase<TInput, TOutput>` defines skeleton, subclasses implement `Transform()`
- **Factory Pattern:** `NodeFactory` and `TypeActivator` for instantiation
- **Property Injection:** Dependencies injected via properties (maintains parameterless constructor)

**Key Design Decision:** Single abstract node class instead of multiple variants (Node<TIn,TOut>, Node<TIn1,TIn2,TOut>, etc.). Multi-input/output handled via schemas + CatalogMap.

**Key Classes:**
- `NodeBase<TInput, TOutput>` - Abstract transformation node
- `TypeActivator` - Compiled expression factory with caching
- `NodeFactory` - Domain-specific factory wrapper

---

#### 3. Pipelines Space

**Purpose:** Orchestrate nodes into directed acyclic graphs (DAGs) with compile-time type safety.

**Design Patterns:**
- **Builder Pattern:** `PipelineBuilder` for fluent API
- **Composite Pattern:** Pipeline as tree/graph of nodes
- **Strategy Pattern:** Different execution strategies (sequential, parallel)

**Key Innovation:** Unified `CatalogMap<T>` for bidirectional mapping (replaces separate InputMapping/OutputMapping).

**Key Classes:**
- `CatalogMap<T>` - Bidirectional property-to-catalog mapping
- `PipelineBuilder` - Fluent pipeline construction (coming soon)
- `Pipeline` - DAG container (coming soon)
- `SequentialExecutor` - Execution engine (coming soon)

---

#### 4. Meta Space

**Purpose:** Configuration, registration, and execution metadata.

**Design Patterns:**
- **Registry Pattern:** `PipelineRegistry` for named pipeline access
- **Builder Pattern:** Configuration builders
- **Service Locator (optional):** DI integration

**Key Classes:**
- `PipelineRegistry` - Central pipeline registry (coming soon)
- `PipelineResult`, `NodeResult` - Execution metadata (coming soon)
- `ServiceCollectionExtensions` - DI integration (coming soon)

---

### Key Architectural Decisions

#### Why Single Node Base Class?

**Problem:** Multiple node variants (Node<TIn,TOut>, Node<TIn,TOut,TParams>, Node<TIn1,TIn2,TOut>, etc.) lead to:
- Code duplication
- Complexity in pipeline builder
- Poor extensibility (need new class for 4, 5, 6+ inputs)

**Solution:** Single `NodeBase<TInput, TOutput>` with:
- Input/output schemas for multi-input/output scenarios
- `CatalogMap<T>` for property-based mapping
- Symmetry between input and output patterns

**Benefits:**
- One abstraction to understand
- Unlimited input/output arity
- Consistent pattern for all nodes

---

#### Why CatalogMap<T> Instead of Separate Input/Output Mapping?

**Problem:** Separate `InputMapping<T>` and `OutputMapping<T>` classes:
- Two concepts to learn
- Code duplication between classes
- Inconsistent APIs

**Solution:** Single `CatalogMap<T>` that works bidirectionally:
- Context (input vs output position) determines behavior
- Same API for both directions
- `Map()` for catalog entries, `MapParameter()` for constants

**Benefits:**
- One concept, consistent API
- Simpler for users
- Less code to maintain

---

#### Why Property Injection Over Constructor Injection?

**Problem:** Constructor injection prevents:
- Parameterless constructors
- Factory instantiation via `Activator.CreateInstance<T>()`
- Compiled expression factories
- Distributed/parallel execution

**Solution:** Property injection with optional dependencies:
```csharp
public ILogger? Logger { get; set; }
```

**Trade-offs:**
- ✅ Maintains parameterless constructor
- ✅ Compatible with all instantiation patterns
- ✅ Optional dependencies (nullable)
- ⚠️ Must manually inject (pipeline builder `configure` action)

---

### Phase 1 vs Phase 2 Trade-offs

#### Validation Timing

**Phase 1 (Current):**
- Type compatibility: ✅ Compile-time (generic constraints)
- Mapping completeness: ⚠️ Pipeline build-time (`ValidateComplete()`)

**Why Phase 1?**
- Pragmatic: 95% of safety with 5% of complexity
- Fail-fast: Errors before execution, not during
- Simpler: No Roslyn analyzer infrastructure needed

**Phase 2 (Future):**
- Roslyn analyzer for compile-time completeness validation
- IDE red squiggles for incomplete mappings
- Impossible to deploy incomplete pipelines

**When to Upgrade:**
- After core functionality is stable
- When team has Roslyn expertise
- If incomplete mappings become a recurring issue

---

#### Singleton Load Behavior

**Phase 1 (Current):**
- `CatalogMap<T>.LoadAsync()` always returns single instance for mapped schemas
- Bulk data flows through pass-through mode (FromEntry)

**Why Phase 1?**
- Simpler reasoning: one input schema → one execution → one output schema
- Predictable behavior (no surprises about instance count)
- Covers 99% of use cases

**Phase 2 (Future):**
- Explicit `LoadBehavior` enum (Singleton, MultipleFromFirstProperty)
- User controls when multiple instances are created

**When to Upgrade:**
- If concrete use cases emerge requiring multiple instances from mapped schemas
- Wait for user feedback before adding complexity

---

### Dependency Management

**Core Package:**
- `Microsoft.Extensions.DependencyInjection.Abstractions` - Interface only, no concrete container
- `Microsoft.Extensions.Logging.Abstractions` - Serilog-compatible logging
- `CsvHelper`, `Parquet.Net`, `ExcelDataReader` - Data format support

**User's Application:**
- User chooses DI container (typically Microsoft's)
- User chooses logging implementation (Serilog recommended)

**Design:** Thin abstraction preserves flexibility while providing sensible defaults.

---

### Testing Strategy

#### Unit Testing Nodes

Nodes are pure functions - test in isolation without pipeline infrastructure:

```csharp
[Test]
public async Task ProcessCompanies_ConvertsPercentages()
{
    var node = new ProcessCompaniesNode();
    var input = new[]
    {
        new CompanyRawData { Id = "1", Name = "Acme", Rating = "85%" }
    };
    
    var result = await node.Transform(input);
    
    Assert.That(result.Single().Rating, Is.EqualTo(0.85m));
}
```

**Benefits:**
- Fast (no I/O)
- Focused (single node logic)
- Easy setup (just instantiate and call Transform)

#### Integration Testing Pipelines

*(Coming when pipeline executor is implemented)*

Test full pipeline execution with catalog:

```csharp
[Test]
public async Task Pipeline_ProcessesDataEndToEnd()
{
    var catalog = BuildTestCatalog(); // In-memory entries
    var pipeline = BuildTestPipeline(catalog);
    
    await pipeline.ExecuteAsync();
    
    var result = await catalog.Get<OutputData>("output").Load();
    Assert.That(result, Has.Count.EqualTo(100));
}
```

---

### Project Structure

```
src/Flowthru/
├── Data/                          # Data Space
│   ├── ICatalogEntry.cs
│   ├── CatalogEntryBase.cs
│   ├── DataCatalog.cs
│   ├── DataCatalogBuilder.cs
│   └── Implementations/
│       ├── MemoryCatalogEntry.cs
│       ├── CsvCatalogEntry.cs
│       ├── ParquetCatalogEntry.cs
│       └── ExcelCatalogEntry.cs
│
├── Nodes/                         # Nodes Space
│   ├── NodeBase.cs                # Single abstract node
│   └── Factory/
│       ├── TypeActivator.cs       # Expression caching
│       └── NodeFactory.cs
│
├── Pipelines/                     # Pipelines Space
│   ├── Mapping/
│   │   ├── CatalogMap.cs          # Unified bidirectional mapping
│   │   ├── CatalogMapping.cs
│   │   ├── CatalogPropertyMapping.cs
│   │   └── ParameterMapping.cs
│   ├── Execution/                 # (Coming soon)
│   ├── Graph/                     # (Coming soon)
│   └── Validation/                # (Coming soon)
│
├── Meta/                          # Meta Space (Coming soon)
│   ├── Registry/
│   ├── Results/
│   └── Extensions/
│
├── Abstractions/                  # DI abstractions (Coming soon)
└── Logging/                       # Logging extensions (Coming soon)
```

---

### Comparison with Kedro

| Feature | Kedro (Python) | Flowthru (.NET) |
|---------|---------------|-----------------|
| **Type Safety** | Runtime (duck typing) | Compile-time (generics) |
| **Catalog** | YAML + string keys | Typed catalog entries |
| **Nodes** | Functions with decorators | Classes inheriting NodeBase |
| **Parameters** | YAML configuration | Strongly-typed parameter objects |
| **Pipeline Definition** | Python code | C# with fluent builder API |
| **Error Detection** | Runtime | Compile-time + build-time |
| **IDE Support** | Basic | Full (IntelliSense, refactoring, Go To Definition) |
| **Inspiration** | Data engineering best practices | Kedro patterns + .NET type system |

---

### Future Enhancements

**Pipeline Execution Engine** (Next Priority)
- `PipelineBuilder` with four overloads
- `SequentialExecutor` for execution
- `DependencyAnalyzer` for DAG validation
- `PipelineResult` with timing metrics

**Metadata and Registry**
- `PipelineRegistry` for centralized pipeline management
- `ExecutionContext` with IServiceProvider, ILogger
- DI integration extensions

**Phase 2 Upgrades**
- Roslyn analyzer for compile-time mapping validation
- Parallel executor for DAG-based concurrent execution
- Graph visualization (Mermaid, DOT export)
- OpenTelemetry integration for observability

**Advanced Features**
- Pipeline versioning and lineage tracking
- Caching/memoization of intermediate results
- Cloud integration (Azure, AWS)
- Configuration system (YAML → source-generated typed catalogs)

---

## Contributing

Flowthru is in active development. The foundational architecture (Data, Nodes, Mapping) is complete. Next priorities:
1. Pipeline builder and executor
2. Metadata and registry infrastructure
3. Comprehensive testing
4. Example projects

---

## License

MIT License - See LICENSE file for details

---

## Acknowledgments

- **Kedro:** Inspiration for declarative pipeline patterns and data engineering best practices
- **ChainSharp:** Reflection-based factory patterns and workflow composition ideas
- **Railway-Oriented Programming:** Error handling philosophy (deferred to Phase 2+)
