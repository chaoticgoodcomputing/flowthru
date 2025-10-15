# Spaceflights Test Project Migration Status

## Completed Changes

### ✅ Data Catalog (Data/Catalog.cs)
- Updated all catalog entry registrations to use library implementations:
  - `CsvCatalogEntry<T>` for CSV files
  - `ExcelCatalogEntry<T>` for Excel files  
  - `ParquetCatalogEntry<T>` for Parquet files
  - `MemoryCatalogEntry<T>` for in-memory storage
- Added `using Flowthru.Data.Implementations;`
- Updated constructor signatures to match library API

### ✅ Removed Duplicate Files
- Deleted duplicate catalog entry implementations from Data folder:
  - ICatalogEntry.cs
  - CsvCatalogEntry.cs
  - MemoryCatalogEntry.cs
  - ParquetCatalogEntry.cs
  - ExcelCatalogEntry.cs
- Deleted placeholder pipeline files:
  - Pipeline.cs
  - OutputMapping.cs

## Pending Changes - Node API Mismatch

### ❌ Node Implementations Need Major Refactoring

The test project nodes use obsolete multi-arity base class syntax that doesn't exist in the library:

**Current (Obsolete):**
```csharp
// Simple node - OBSOLETE API
public class PreprocessCompaniesNode : Node<CompanyRawSchema, CompanySchema>
{
  protected override Task<IEnumerable<CompanySchema>> Transform(
    IEnumerable<CompanyRawSchema> input) { }
}

// Multi-input node - OBSOLETE API (doesn't exist in library)
public class CreateModelInputTableNode 
  : Node<ShuttleSchema, CompanySchema, ReviewRawSchema, ModelInputSchema>
{
  protected override Task<IEnumerable<ModelInputSchema>> Transform(
    IEnumerable<ShuttleSchema> shuttles,
    IEnumerable<CompanySchema> companies,
    IEnumerable<ReviewRawSchema> reviews) { }
}

// Parameterized node - OBSOLETE API (doesn't exist in library)
public class SplitDataNode : Node<ModelInputSchema, SplitDataOutputs, ModelOptions>
{
  // Inherited property: public ModelOptions Parameters { get; set; }
  protected override Task<IEnumerable<SplitDataOutputs>> Transform(
    IEnumerable<ModelInputSchema> input) { }
}
```

**Library Provides (Unified API):**
```csharp
public abstract class NodeBase<TInput, TOutput>
{
  public ILogger? Logger { get; set; }
  
  protected abstract Task<IEnumerable<TOutput>> Transform(
    IEnumerable<TInput> input);
}
```

### Refactoring Options

#### Option 1: Use Input/Output Schemas + CatalogMap (Recommended)

Multi-input nodes create input schema classes:

```csharp
// Define input schema for multi-input node
public class CreateModelInputTableInputs
{
  [Required] public IEnumerable<ShuttleSchema> Shuttles { get; set; }
  [Required] public IEnumerable<CompanySchema> Companies { get; set; }
  [Required] public IEnumerable<ReviewRawSchema> Reviews { get; set; }
}

// Refactored node
public class CreateModelInputTableNode 
  : NodeBase<CreateModelInputTableInputs, ModelInputSchema>
{
  protected override Task<IEnumerable<ModelInputSchema>> Transform(
    IEnumerable<CreateModelInputTableInputs> inputs)
  {
    var input = inputs.Single(); // CatalogMap provides singleton
    var shuttles = input.Shuttles;
    var companies = input.Companies;
    var reviews = input.Reviews;
    
    // ... existing join logic ...
  }
}
```

Pipeline configuration uses CatalogMap:
```csharp
var inputMap = new CatalogMap<CreateModelInputTableInputs>()
  .Map(x => x.Shuttles, "preprocessed_shuttles")
  .Map(x => x.Companies, "preprocessed_companies")  
  .Map(x => x.Reviews, "reviews");

pipeline.AddNode<CreateModelInputTableNode>()
  .WithInput(inputMap)
  .WithOutput("model_input_table");
```

#### Option 2: Create Wrapper Schemas (Quick Fix)

For simple 1-to-1 transforms, wrap single values in schemas:

```csharp
public class PreprocessCompaniesInput
{
  [Required] public CompanyRawSchema Company { get; set; }
}

public class PreprocessCompaniesNode 
  : NodeBase<PreprocessCompaniesInput, CompanySchema>
{
  protected override Task<IEnumerable<CompanySchema>> Transform(
    IEnumerable<PreprocessCompaniesInput> inputs)
  {
    var processed = inputs.Select(input => new CompanySchema {
      Id = input.Company.Id,
      // ... existing transform logic using input.Company ...
    });
    return Task.FromResult(processed);
  }
}
```

**This requires CatalogMap to wrap individual records** - need to verify if library supports this pattern.

### Affected Node Files

**Data Processing Nodes:**
- `Pipelines/DataProcessing/Nodes/PreprocessCompaniesNode.cs` - Simple node
- `Pipelines/DataProcessing/Nodes/PreprocessShuttlesNode.cs` - Simple node
- `Pipelines/DataProcessing/Nodes/CreateModelInputTableNode.cs` - **3-input node**

**Data Science Nodes:**
- `Pipelines/DataScience/Nodes/SplitDataNode.cs` - **Parameterized node**
- `Pipelines/DataScience/Nodes/TrainModelNode.cs` - **2-input node**
- `Pipelines/DataScience/Nodes/EvaluateModelNode.cs` - **3-input node + logger injection**

### Additional Issues

1. **Parameters Pattern**: `SplitDataNode` uses third type parameter for `ModelOptions`. Library doesn't support this - need to use property injection instead.

2. **Logger Injection**: `EvaluateModelNode` already uses property injection for logger, which is compatible with library API.

3. **Multi-Output Pattern**: `SplitDataOutputs` is designed for `OutputMapping<T>` which was deleted. Need to use library's `CatalogMap<T>` instead.

## Next Steps - Decision Required

Before continuing with node refactoring, we need to decide:

1. **API Pattern**: Use Option 1 (input schemas + CatalogMap) or create helper base classes?

2. **Scope**: Refactor all nodes now, or implement Pipeline Builder/Registry first to validate the pattern?

3. **CatalogMap Behavior**: Verify if CatalogMap can work with:
   - Individual records (not just IEnumerable<T>)
   - Pass-through mode for simple 1-to-1 transforms
   - Multi-output scenarios (SplitDataOutputs)

4. **Parameters**: How should `ModelOptions` parameters be injected into `SplitDataNode`?
   - Property injection like logger?
   - Constructor parameters (breaks parameterless requirement)?
   - Via input schema?

## Remaining Files to Update

Once node pattern is decided:
- All 6 node implementation files
- `Pipelines/PipelineRegistry.cs` - Replace OutputMapping with CatalogMap
- `Program.cs` - Update pipeline building code
- `Flowthru.Spaceflights.csproj` - Remove duplicate dependencies
