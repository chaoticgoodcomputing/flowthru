# Slicing Pipelines

Execute subsets of your pipeline using slicing strategies. This is useful for testing specific nodes, debugging data flow, or running specific portions of large pipelines.

## CLI Usage

Flowthru supports five slicing strategies via command-line flags. All strategies can be combined — they compose via intersection.

### Pipeline Selection

**Run a specific pipeline from a multi-pipeline project:**
```bash
dotnet run --pipelines DataScience
```

**Run multiple pipelines:**
```bash
dotnet run --pipelines DataScience,Reporting
```

Flowthru always merges all registered pipelines into a unified DAG. The `--pipelines` flag filters to nodes belonging to specific pipelines by name. Without this flag, all pipelines execute.

### Basic Slicing

**Run specific nodes and their dependencies:**
```bash
dotnet run --from-nodes PreprocessCompanies,PreprocessShuttles
```

**Run nodes up to a specific output:**
```bash
dotnet run --to-nodes CreateModelInput
```

**Run only explicitly named nodes (dependencies auto-included):**
```bash
dotnet run --only-nodes FeatureEngineering
```

### Data-Based Slicing

**Run producers of specific catalog entries and their upstream dependencies:**
```bash
dotnet run --to-data companies,shuttles
```

**Run consumers of specific catalog entries and their downstream dependents:**
```bash
dotnet run --from-data model_input
```

These strategies resolve catalog entry names to their producer or consumer nodes, then include transitive dependencies. `--to-data` finds producers (upstream), `--from-data` finds consumers (downstream).

### Combining Strategies

Multiple flags narrow the result set via intersection:

```bash
dotnet run --pipelines DataScience --to-data model_input
```

This runs nodes in the DataScience pipeline that are required to produce `model_input` data.

**Common patterns:**

```bash
# Run a single pipeline
dotnet run --pipelines DataScience

# Test specific nodes without downstream work
dotnet run --only-nodes PreprocessCompanies,PreprocessShuttles --to-nodes CreateModelInput

# Run preprocessing steps with dependencies
dotnet run --from-nodes ValidateRawData --to-nodes CreateModelInput

# Find producer of a catalog entry across all pipelines
dotnet run --to-data final_report

# Run specific pipeline up to a data output
dotnet run --pipelines Reporting --to-data ShuttleCapacityChart

# Filter multiple pipelines and run to specific output
dotnet run --pipelines DataScience,DataEvaluation --to-data validation_metrics
```

### Comma-Separated Values

All slicing flags accept comma-separated lists:

```bash
dotnet run --pipelines DataScience,Reporting --only-nodes TrainModel,EvaluateModel
```

## Programmatic Usage

Use `ExecutionOptions` with `IFlowthruService`:

```csharp
using Flowthru.Services.Models;

var options = new ExecutionOptions
{
    SliceStrategy = new PipelineSliceStrategy
    {
        Pipelines = new HashSet<string> { "DataScience" },
        FromNodes = new HashSet<string> { "PreprocessCompanies", "PreprocessShuttles" },
        ToNodes = new HashSet<string> { "CreateModelInput" }
    }
};

var result = await service.ExecutePipelineAsync(options, exportMetadata: true, metadataOutputDirectory: null, cancellationToken);
```

### All Strategy Properties

```csharp
var strategy = new PipelineSliceStrategy
{
    Pipelines = new HashSet<string> { "DataScience", "Reporting" },  // filter by pipeline name
    FromNodes = new HashSet<string> { "NodeA", "NodeB" },            // + upstream
    ToNodes = new HashSet<string> { "NodeC" },                       // + downstream
    FromData = new HashSet<string> { "model_input" },                // consumers + downstream
    ToData = new HashSet<string> { "raw_data" },                     // producers + upstream
    OnlyNodes = new HashSet<string> { "NodeD", "NodeE" }             // explicit allowlist
};
```

All properties are optional. Omit them to run all pipelines:

```csharp
var options = new ExecutionOptions(); // No slicing, all pipelines execute
```

## How Slicing Works

Slicing guarantees runnability — the result is always a valid sub-DAG:

1. **Pipelines** filters nodes by pipeline name prefix (e.g., "DataScience.NodeName")
2. **FromNodes** includes all upstream dependencies (transitive closure)
3. **ToNodes** includes all downstream dependents (transitive closure)
4. **FromData** resolves to consumer nodes, then includes downstream (transitive closure)
5. **ToData** resolves to producer nodes, then includes upstream (transitive closure)
6. **OnlyNodes** automatically includes required dependencies
7. Multiple strategies intersect — each narrows the result set

**Slicing is additive only.** There is no `--except` flag because subtractive operations break the runnability guarantee.

## Cross-Pipeline Queries

The unified execution model enables powerful cross-pipeline queries:

```bash
# Find which pipeline produces a catalog entry
dotnet run --to-data intermediate_report

# Find all consumers of a shared data asset
dotnet run --from-data validated_companies
```

These queries search the merged DAG of all pipelines, making cross-pipeline dependencies transparent.

## Errors

Slicing validates during pre-flight:

```
✗ Pipelines filter matched no nodes for pipeline: 'Typo'
Available pipelines: DataScience, DataProcessing, Reporting
```

```
✗ FromNodes references non-existent node: 'InvalidNode'
Available nodes: PreprocessCompanies, PreprocessShuttles, ...
```

```
✗ ToData references catalog entry 'unknown_data' which has no producer node
```

Invalid slices fail before execution begins, consistent with Flowthru's fail-fast philosophy.
