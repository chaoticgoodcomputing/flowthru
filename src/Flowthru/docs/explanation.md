# Explanation

## Why Compile-Time Safety Matters

Flowthru's core design philosophy is **fail at compile-time, not runtime**. This section explains the "why" behind this approach.

### The Cost of Runtime Errors

**Kedro (Python) workflow:**
1. Write pipeline in Python
2. Run pipeline
3. ❌ Error after 30 minutes: "KeyError: 'companeis'" (typo in catalog key)
4. Fix typo
5. Re-run entire pipeline from scratch
6. ❌ Error after 30 minutes: "TypeError: expected float, got str" (wrong parameter type)
7. Fix type
8. Re-run entire pipeline from scratch
9. ✅ Success after 90 minutes total

**Flowthru (.NET) workflow:**
1. Write pipeline in C#
2. ❌ Compile error: "Property 'companeis' does not exist on MyCatalog"
3. Fix typo (IntelliSense shows correct name)
4. ❌ Compile error: "Cannot convert string to double"
5. Fix type
6. ✅ Compile succeeds
7. Run pipeline
8. ✅ Success after 30 minutes

**Result:** Flowthru saves 60 minutes by catching errors before execution.

### What Can Go Wrong at Runtime vs Compile-Time

| Error Type                 | Kedro (Runtime)               | Flowthru (Compile-Time)        |
| -------------------------- | ----------------------------- | ------------------------------ |
| Typo in catalog key        | ❌ KeyError during load        | ✅ Compile error                |
| Wrong data type in node    | ❌ TypeError during transform  | ✅ Generic constraint violation |
| Missing catalog entry      | ❌ KeyError when accessed      | ✅ Property doesn't exist       |
| Typo in parameter name     | ❌ KeyError or silent fallback | ✅ Compile error                |
| Wrong parameter type       | ❌ TypeError or coercion       | ✅ Type mismatch error          |
| Input/output type mismatch | ❌ Runtime type error          | ✅ Generic constraint violation |
| Unmapped required property | ⚠️ Build-time validation*      | ⚠️ Build-time validation*       |
| Pipeline DAG cycle         | ⚠️ Build-time validation*      | ⚠️ Build-time validation*       |
| Invalid external data      | ❌ Runtime error (after work)  | ⚠️ Pre-execution validation**   |

*\*These validations occur when building the pipeline, before execution begins (fail-fast).*

*\*\*Optional eager schema validation catches data quality issues before pipeline execution.*

### Extending Fail-Fast to External Data

Flowthru's compile-time safety catches most errors before execution, but there's one category that can't be validated at compile-time: **external data quality**.

**The Problem:**
- Your C# code is correct (types match, no typos)
- Pipeline DAG is valid (no cycles, no conflicts)
- But the CSV file has a typo in a column name, or Excel file is missing rows, or Parquet schema changed

Traditional approach: discover the error after 30 minutes of computation.

**Flowthru's Solution: Eager Schema Validation**

Flowthru provides an **optional inspection system** that validates external data sources before pipeline execution begins:

```csharp
builder.RegisterPipeline<MyCatalog>("data_processing", DataProcessingPipeline.Create)
  .WithValidation(validation => {
    // Validate external data before pipeline runs
    validation.Inspect(catalog.Companies, InspectionLevel.Deep);
    validation.Inspect(catalog.Shuttles, InspectionLevel.Deep);
  });
```

**What happens:**
1. Pipeline builds and analyzes DAG
2. **Inspection phase:** Validates all Layer 0 inputs (external data sources)
   - File exists?
   - Format valid (CSV/Excel/Parquet)?
   - Headers match expected schema?
   - Sample rows (or all rows) deserialize correctly?
3. If validation fails → immediate `ValidationException` with all errors
4. If validation passes → pipeline executes normally

**Result:** Catch data quality issues in seconds, not minutes. Fail-fast extends from compile-time to pre-execution time.

**Inspection Levels:**

| Level | Validates | Performance | When to Use |
|-------|-----------|-------------|-------------|
| **None** | Nothing (skips inspection) | Instant | Known-good data, performance-critical |
| **Shallow** (default) | File exists, format valid, schema matches, sample rows | Fast (seconds) | Development, testing, most production |
| **Deep** (opt-in) | Everything + all rows validated | Slow (minutes for large datasets) | Critical production data, first-time ingestion |

See [How-to: Configure Dataset Inspection](./how-to/dataset-inspection.md) for details.

### The Type System as Documentation

In Kedro, you must consult documentation or read code to know:
- What datasets exist in the catalog
- What type of data each dataset contains
- What inputs a pipeline expects
- What parameters are available

In Flowthru, the compiler **is** the documentation:
- IntelliSense shows all catalog properties
- Hover over property to see data type
- Generic constraints show node input/output types
- Parameter classes document available options

**Example:** Finding what datasets are available:

```csharp
// Kedro: Must read YAML or code
# catalog.yml - could be anywhere in project
companies:
  type: pandas.CSVDataset
  filepath: data/companies.csv

# VS Code: no autocomplete, must memorize keys
df = catalog.load("companies")  # Hope you spelled it right!

// Flowthru: IntelliSense shows all options
var data = catalog. // ← IntelliSense dropdown shows:
                    //   - Companies
                    //   - Reviews
                    //   - EnrichedCompanies
                    //   - Features
                    //   (all properties, all types)
```

---

## Understanding the Three Compile-Time Safety Layers

Flowthru provides safety at three levels, in increasing order of complexity.

### Layer 1: Type-Safe Catalog Properties (Always Use This)

**Mechanism:** Catalog entries are strongly-typed properties, not string keys.

**What It Prevents:**
- Typos in catalog entry names
- Accessing non-existent entries
- Type mismatches when loading data

**How to Use:**
```csharp
public class MyCatalog : DataCatalogBase
{
    public CsvCatalogDataset<CompanyData> Companies { get; }
    
    public MyCatalog()
    {
        Companies = CreateCsvDataset<CompanyData>("companies", "data/companies.csv");
    }
}

// Usage: compiler validates everything
var data = catalog.Companies; // ✅ Compiler knows type is ICatalogDataset<CompanyData>
```

**Best Practices:**
- ✅ Always inherit from `DataCatalogBase`
- ✅ Use properties, never `Get<T>(string key)`
- ✅ Name properties semantically (plural nouns: Companies, Reviews, Features)
- ❌ Don't use dictionaries or string-based lookups

### Layer 2: Generic Constraint Validation (Automatic)

**Mechanism:** `NodeBase<TInput, TOutput>` uses generic constraints to enforce type compatibility.

**What It Prevents:**
- Wiring wrong catalog entry to wrong node
- Input/output type mismatches
- Incompatible pipeline connections

**How It Works:**
```csharp
// Node declares exact types it expects
public class ProcessNode : NodeBase<CompanyRawData, CompanyProcessed> { }

// Pipeline builder uses generics to enforce compatibility
builder.AddNode<ProcessNode>(
    input: catalog.Companies,      // ✅ Must be ICatalogDataset<CompanyRawData>
    output: catalog.Processed,     // ✅ Must be ICatalogDataset<CompanyProcessed>
    name: "process");

// Compiler verifies:
// - catalog.Companies provides CompanyRawData (node's TInput)
// - catalog.Processed accepts CompanyProcessed (node's TOutput)
// - Any mismatch = compilation error
```

**Best Practices:**
- ✅ Always specify exact types in node signatures
- ✅ Let the compiler infer generic parameters in `AddNode<T>()`
- ❌ Never use `object` or `dynamic` as node types
- ❌ Don't cast away type information

### Layer 2.5: Read/Write Capability Enforcement (New in v0.3.0)

**Mechanism:** Catalog entries declare read-only or read-write capabilities through interface inheritance.

**What It Prevents:**
- Using read-only data sources (Excel files, HTTP APIs) as pipeline outputs
- Runtime "write not supported" errors
- Attempting to save to immutable data sources

**How It Works:**
```csharp
// Read-only dataset (e.g., Excel file - no write support)
public IReadableCatalogDataset<ShuttleRawSchema> Shuttles =>
    GetOrCreateReadOnlyDataset(() => 
        new ExcelCatalogDataset<ShuttleRawSchema>("shuttles", "data/shuttles.xlsx"));

// Read-write dataset (e.g., CSV or Parquet - full access)
public ICatalogDataset<ShuttleSchema> CleanedShuttles =>
    GetOrCreateDataset(() => 
        new ParquetCatalogDataset<ShuttleSchema>("cleaned_shuttles", "data/cleaned.parquet"));

// Pipeline usage - compiler enforces correctness
pipeline.AddNode<PreprocessShuttlesNode>(
    input: catalog.Shuttles,           // ✅ IReadableCatalogDataset<T> - read-only OK for input
    output: catalog.CleanedShuttles,   // ✅ ICatalogDataset<T> - read-write OK for output
    name: "PreprocessShuttles"
);

// This would cause a compile error (if we had stricter generic constraints):
pipeline.AddNode<SomeNode>(
    input: catalog.SomeInput,
    output: catalog.Shuttles,          // ❌ Cannot use IReadableCatalogDataset<T> as output!
    name: "WontCompile"
);
// Error: Cannot convert IReadableCatalogDataset<T> to writable output type
```

**Interface Hierarchy:**
```csharp
// Base capability interfaces
IReadableCatalogDataset<T>   // Can Load() only
IWritableCatalogDataset<T>   // Can Save() only

// Full read-write interface inherits both
ICatalogDataset<T> : IReadableCatalogDataset<T>, IWritableCatalogDataset<T>

// Same pattern for singleton objects
IReadableCatalogObject<T>    // Can Load() only
IWritableCatalogObject<T>    // Can Save() only
ICatalogObject<T> : IReadableCatalogObject<T>, IWritableCatalogObject<T>
```

**When to Use Each:**
- **`IReadableCatalogDataset<T>`**: Excel files, database views, HTTP APIs, immutable reference data
- **`IWritableCatalogDataset<T>`**: Log sinks, metrics collectors (rare - usually use read-write)
- **`ICatalogDataset<T>`**: CSV, Parquet, in-memory datasets, any read-write source
- **`IReadableCatalogObject<T>`**: Immutable config files, pre-trained models from external sources
- **`ICatalogObject<T>`**: Trained models, mutable configuration, aggregated metrics

**Best Practices:**
- ✅ Use `IReadableCatalogDataset<T>` for Excel files and other read-only sources
- ✅ Use `GetOrCreateReadOnlyDataset()` helper in catalog for read-only datasets
- ✅ Use `ICatalogDataset<T>` (full read-write) for most processing pipelines
- ✅ Declare capabilities explicitly in catalog property types
- ❌ Don't try to use read-only datasets as pipeline outputs
- ❌ Don't implement Save() methods that throw exceptions (use read-only base classes instead)

### Layer 3: Expression-Based Mapping (Use for Multi-Input/Output)

**Mechanism:** `CatalogMap<T>` uses expression trees to validate property mappings at compile-time.

**What It Prevents:**
- Typos in property names
- Mapping to non-existent properties
- Type mismatches between properties and catalog entries

**How It Works:**
```csharp
// Input schema defines required properties
public record JoinInputs
{
    [Required]
    public required IEnumerable<Company> Companies { get; init; }
    
    [Required]
    public required IEnumerable<Review> Reviews { get; init; }
}

// Expression-based mapping
var inputMap = new CatalogMap<JoinInputs>();
inputMap.Map(i => i.Companies, catalog.Companies);
//           ^^^^^^^^^^^^^^
//           Expression tree: compiler validates
//           - Property 'Companies' exists on JoinInputs
//           - Type matches catalog.Companies type

inputMap.Map(i => i.Companeis, catalog.Companies);
//           ^^^^^^^^^^^^^^
//           ❌ Compile error: 'JoinInputs' does not contain 'Companeis'
```

**Best Practices:**
- ✅ Use expression selectors: `i => i.PropertyName`
- ✅ Mark properties with `[Required]` to document intent
- ✅ Let IntelliSense guide property selection
- ❌ Never use string property names
- ❌ Don't use reflection for mapping

---

## Common Pitfalls and How to Avoid Them

Even with compile-time safety, certain patterns can introduce runtime errors. Here's how to avoid them.

### Pitfall 1: Using String Keys Instead of Properties

**❌ Don't:**
```csharp
// Bypasses all compile-time safety!
public Pipeline Create(DataCatalogBase catalog)
{
    var companies = catalog.CreateEntry<CompanyData>("companies"); // String key!
    // ...
}
```

**✅ Do:**
```csharp
// Strongly-typed catalog
public Pipeline Create(MyCatalog catalog)
{
    var companies = catalog.Companies; // Property!
    // ...
}
```

**Why:** String keys reintroduce all the runtime errors Flowthru was designed to eliminate.

### Pitfall 2: Over-Using `object` or `dynamic`

**❌ Don't:**
```csharp
// Loses all type information
public class GenericProcessNode : NodeBase<object, object>
{
    protected override Task<IEnumerable<object>> Transform(IEnumerable<object> input)
    {
        // Must cast, loses compile-time safety
        var companies = input.Cast<CompanyData>();
        // ...
    }
}
```

**✅ Do:**
```csharp
// Explicit types
public class ProcessCompaniesNode : NodeBase<CompanyData, ProcessedCompany>
{
    protected override Task<IEnumerable<ProcessedCompany>> Transform(
        IEnumerable<CompanyData> input)
    {
        // No casting needed, full type safety
        var processed = input.Select(/* ... */);
        // ...
    }
}
```

**Why:** Explicit types enable compiler validation and IntelliSense at every usage site.

### Pitfall 3: Ignoring `[Required]` Validation

**❌ Don't:**
```csharp
// Properties not marked as required
public record JoinInputs
{
    public IEnumerable<Company>? Companies { get; init; }
    public IEnumerable<Review>? Reviews { get; init; }
}

// Forgot to map Reviews!
var inputMap = new CatalogMap<JoinInputs>();
inputMap.Map(i => i.Companies, catalog.Companies);
// Missing: inputMap.Map(i => i.Reviews, catalog.Reviews);

// Runtime: Reviews will be null, causing NullReferenceException
```

**✅ Do:**
```csharp
// Properties marked as required
public record JoinInputs
{
    [Required]
    public required IEnumerable<Company> Companies { get; init; }
    
    [Required]
    public required IEnumerable<Review> Reviews { get; init; }
}

// ValidateComplete() catches missing mapping at pipeline build-time
inputMap.ValidateComplete(); // ❌ Throws: Property 'Reviews' is required but not mapped
```

**Why:** `[Required]` + `ValidateComplete()` provides fail-fast validation before execution.

### Pitfall 4: Mutating Shared State in Nodes

**❌ Don't:**
```csharp
// Shared mutable state
public class StatefulNode : NodeBase<InputData, OutputData>
{
    private int _counter = 0; // ❌ Mutable state!
    
    protected override Task<IEnumerable<OutputData>> Transform(
        IEnumerable<InputData> input)
    {
        _counter++; // ❌ Not thread-safe!
        // ...
    }
}
```

**✅ Do:**
```csharp
// Stateless, pure function
public class StatelessNode : NodeBase<InputData, OutputData>
{
    protected override Task<IEnumerable<OutputData>> Transform(
        IEnumerable<InputData> input)
    {
        // All data flows through parameters and return value
        var processed = input.Select(/* ... */);
        return Task.FromResult(processed);
    }
}
```

**Why:** Stateless nodes are thread-safe, testable, and compatible with parallel execution.

### Pitfall 5: Constructor Injection Instead of Property Injection

**❌ Don't:**
```csharp
// Constructor injection prevents parameterless instantiation
public class MyNode : NodeBase<InputData, OutputData>
{
    private readonly ILogger _logger;
    
    public MyNode(ILogger<MyNode> logger) // ❌ No parameterless constructor!
    {
        _logger = logger;
    }
}
// Error: Cannot instantiate via Activator.CreateInstance<T>()
```

**✅ Do:**
```csharp
// Property injection maintains parameterless constructor
public class MyNode : NodeBase<InputData, OutputData>
{
    public ILogger<MyNode>? Logger { get; set; } // ✅ Optional property
    
    protected override Task<IEnumerable<OutputData>> Transform(
        IEnumerable<InputData> input)
    {
        Logger?.LogInformation("Processing...");
        // ...
    }
}
```

**Why:** Parameterless constructors enable factory instantiation, expression compilation, and distributed execution.

---

## Design Rationale: Why These Choices?

Understanding the "why" helps you make better decisions when extending Flowthru.

### Why DataCatalogBase Instead of Interface?

**Decision:** Catalog is an abstract base class, not an interface.

**Rationale:**
- ✅ Provides factory methods (`CreateCsvDataset`, `CreateParquetDataset`, etc.)
- ✅ Enforces initialization pattern (constructor assigns properties)
- ✅ Allows future additions without breaking existing code
- ✅ Maintains `Services` property for DI integration

**Trade-offs:**
- ⚠️ Single inheritance (but catalogs rarely need other base classes)
- ✅ Simpler user code (no boilerplate implementation)

### Why Expression Trees for Mapping Instead of Strings?

**Decision:** `inputMap.Map(i => i.PropertyName, catalog.Entry)` instead of `inputMap.Map("PropertyName", catalog.Entry)`.

**Rationale:**
- ✅ Compiler validates property exists
- ✅ Refactoring updates all references automatically
- ✅ IntelliSense provides autocomplete
- ✅ Type inference ensures property and catalog types match
- ✅ Go To Definition navigation works

**Trade-offs:**
- ⚠️ Slightly more verbose syntax
- ✅ Eliminates entire class of runtime errors

### Why Single NodeBase Class Instead of Multiple Variants?

**Decision:** One `NodeBase<TInput, TOutput>` instead of `Node1Input1Output<TIn,TOut>`, `Node2Inputs1Output<TIn1,TIn2,TOut>`, etc.

**Rationale:**
- ✅ Single concept to learn (one abstract class)
- ✅ Scales to unlimited inputs/outputs (via schemas)
- ✅ Consistent pattern for all nodes
- ✅ Less code to maintain in framework
- ✅ Symmetry between input and output patterns

**Trade-offs:**
- ⚠️ Requires defining schema classes for multi-input/output nodes
- ✅ Schemas are self-documenting and reusable

### Why Property Injection Instead of Constructor Injection?

**Decision:** Dependencies via nullable properties instead of constructor parameters.

**Rationale:**
- ✅ Maintains parameterless constructor (required for factory instantiation)
- ✅ Compatible with compiled expression factories (`Expression.New()`)
- ✅ Works with `Activator.CreateInstance<T>()`
- ✅ Supports optional dependencies (nullable types)
- ✅ Enables future distributed execution (nodes serialized without dependencies)

**Trade-offs:**
- ⚠️ Dependencies must be explicitly injected (not automatic)
- ✅ More explicit about optional vs required dependencies

### Why FlowthruApplication Pattern Instead of Manual Setup?

**Decision:** `FlowthruApplication.Create()` builder pattern instead of manual DI/catalog/registry setup.

**Rationale:**
- ✅ Reduces boilerplate from ~100 lines to ~15 lines
- ✅ Enforces best practices (logging, DI, proper exit codes)
- ✅ Matches Kedro user expectations (single entry point)
- ✅ Easier to extend (add configuration via builder methods)
- ✅ Consistent across all projects

**Trade-offs:**
- ⚠️ Less control over low-level details (but escape hatches exist)
- ✅ 95% of projects don't need custom setup

---

## Comparing with Kedro: Key Differences

Understanding how Flowthru differs from Kedro helps teams migrating from Python.

| Aspect                  | Kedro                              | Flowthru                                     |
| ----------------------- | ---------------------------------- | -------------------------------------------- |
| **Catalog Definition**  | YAML file with string keys         | C# class with typed properties               |
| **Catalog Access**      | `catalog.load("key")`              | `catalog.PropertyName`                       |
| **Node Definition**     | Python function with decorator     | C# class inheriting `NodeBase<TIn,TOut>`     |
| **Pipeline Definition** | `pipeline([node(...), node(...)])` | `PipelineBuilder().AddNode<T>().Build()`     |
| **Parameters**          | YAML file, dictionary access       | C# record classes, strongly-typed            |
| **Type Safety**         | Runtime (duck typing)              | Compile-time (generic constraints)           |
| **Multi-Input Nodes**   | Multiple function parameters       | Input schema + `CatalogMap<T>`               |
| **Error Detection**     | During execution                   | During compilation                           |
| **IDE Support**         | Basic (Pylance)                    | Full (IntelliSense, refactoring, navigation) |
| **Parallel Execution**  | Automatic (DAG-based)              | Future (currently sequential)                |
| **Configuration**       | YAML-driven                        | Code-driven with builder pattern             |

**Key Insight:** Flowthru trades Python's dynamic flexibility for C#'s compile-time guarantees. This is intentional—we prevent errors before they happen.

---

## When to Use Flowthru vs Kedro

Flowthru is not a replacement for Kedro in all scenarios. Choose based on your team and project needs.

**Use Flowthru when:**
- ✅ Team is comfortable with C#/.NET ecosystem
- ✅ Type safety and refactoring support are priorities
- ✅ Long-term maintainability matters more than rapid prototyping
- ✅ Need to integrate with existing .NET infrastructure
- ✅ Performance-critical workloads (compiled vs interpreted)
- ✅ Enterprise environments with strict type safety requirements

**Use Kedro when:**
- ✅ Team is primarily Python-focused
- ✅ Rapid prototyping and experimentation are priorities
- ✅ Python-specific ML libraries are essential (scikit-learn, TensorFlow)
- ✅ Dynamic typing flexibility is more valuable than compile-time checks
- ✅ Rich ecosystem of Kedro plugins needed
- ✅ Research environments where iteration speed matters most

**Hybrid Approach:**
- Use Kedro for research/experimentation
- Use Flowthru for production/enterprise deployment
- Common pattern: prototype in Python, productionize in C#

---

## Future Enhancements

Flowthru is in active development. Upcoming features maintain compile-time safety while adding capabilities:

**Phase 1 (Current):**
- ✅ Application builder pattern (`FlowthruApplication`)
- ✅ Inline pipeline registration (`builder.RegisterPipeline<TCatalog>()`)
- ✅ Generic pipeline registry (`PipelineRegistry<TCatalog>` for complex scenarios)
- ✅ Type-safe parameter injection
- ✅ Property-based dependency injection
- ✅ Formatted console output
- ✅ Proper exit codes

**Phase 2 (Next):**
- ⏳ Parallel executor (DAG-based concurrent execution)
- ⏳ Graph visualization (Mermaid, DOT export)
- ⏳ Pipeline composition (combine multiple pipelines)
- ⏳ Advanced caching (memoization of intermediate results)

**Phase 3 (Future):**
- 🔮 Roslyn analyzer (compile-time mapping validation)
- 🔮 Source generators (YAML → typed catalog classes)
- 🔮 OpenTelemetry integration (distributed tracing)
- 🔮 Cloud integration (Azure, AWS batch execution)
- 🔮 Pipeline versioning and lineage tracking

**Design Principle:** Every feature must maintain or enhance compile-time safety. No feature will reintroduce runtime errors that could be caught at compile-time.
