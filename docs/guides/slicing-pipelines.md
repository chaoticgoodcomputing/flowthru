---
title: Slicing Flows
description: Execute subsets of a flow with --from, --to, --only, and related slicing flags — useful for testing specific steps, debugging data flow, or running portions of a large flow.
---

Execute subsets of your flow using slicing strategies. This is useful for testing specific steps, debugging data flow, or running specific portions of large flows.

## Label Resolution

A Flowthru flow is a **bipartite graph** of steps and catalog items. Every edge is an input or output relationship between a step and an item. All slicing flags accept either kind of label:

- **Step label** — resolved directly to that step.
- **Catalog item label** — resolved to the step(s) that produce or consume that item.

This means `--from PreprocessShuttles` and `--from raw_shuttles` are equivalent when `PreprocessShuttles` is the only consumer of `raw_shuttles`. You do not need to know which kind of label you are providing — Flowthru resolves uniformly, step-first, item fallback.

## CLI Usage

Flowthru supports four slicing flags. All flags compose via intersection.

### Flow Selection

**Run a specific flow from a multi-flow project:**
```bash
dotnet run --flows DataScience
```

**Run multiple flows:**
```bash
dotnet run --flows DataScience,Reporting
```

Flowthru always merges all registered flows into a unified DAG before execution. `--flows` filters to steps belonging to specific flows by name prefix. Without this flag, all flows execute.

### From

**Run from a label and everything downstream:**
```bash
dotnet run --from PreprocessCompanies
dotnet run --from raw_companies
```

If the label matches a step, that step and all its downstream dependents are included. If it matches a catalog item, all steps that consume that item (and their downstream dependents) are included.

**Multiple labels:**
```bash
dotnet run --from PreprocessCompanies,PreprocessShuttles
```

### To

**Run everything required to produce a label:**
```bash
dotnet run --to CreateModelInput
dotnet run --to model_input
```

If the label matches a step, that step and all its upstream dependencies are included. If it matches a catalog item, the step that produces it (and all its upstream dependencies) are included.

### Only

**Run explicitly named labels plus their minimal upstream dependencies:**
```bash
dotnet run --only FeatureEngineering
dotnet run --only model_input
```

`--only` is an explicit allowlist. Upstream dependencies are automatically included to keep the sub-DAG executable, but nothing downstream is added.

### Combining Flags

Multiple flags narrow the result set via intersection:

```bash
dotnet run --flows DataScience --to model_input
```

This runs steps in the DataScience flow that are required to produce `model_input`.

**Common patterns:**

```bash
# Run a single flow
dotnet run --flows DataScience

# Run a segment between two points
dotnet run --from ValidateRawData --to CreateModelInput

# Explicit allowlist with automatic dependency resolution
dotnet run --only TrainModel,EvaluateModel

# Find the producer of a catalog item across all flows
dotnet run --to final_report

# Run a specific flow up to a catalog item output
dotnet run --flows Reporting --to ShuttleCapacityChart

# Filter multiple flows and run to a specific output
dotnet run --flows DataScience,DataEvaluation --to validation_metrics
```

### Comma-Separated Values

All flags accept comma-separated lists:

```bash
dotnet run --flows DataScience,Reporting --only TrainModel,EvaluateModel
```

## Programmatic Usage

Use `FlowSliceStrategy` with `IFlowthruService`:

```csharp
using Flowthru.Flows;

var strategy = new FlowSliceStrategy
{
    Flows = new HashSet<string> { "DataScience" },
    From  = new HashSet<string> { "PreprocessCompanies", "PreprocessShuttles" },
    To    = new HashSet<string> { "CreateModelInput" },
};

var result = await service.ExecuteFlowAsync(options, exportMetadata: true, metadataOutputDirectory: null, cancellationToken);
```

### All Strategy Properties

```csharp
var strategy = new FlowSliceStrategy
{
    Flows = new HashSet<string> { "DataScience", "Reporting" }, // filter by flow name prefix
    From  = new HashSet<string> { "StepA", "raw_data" },        // step/item + downstream
    To    = new HashSet<string> { "StepC", "model_input" },     // step/item + upstream
    Only  = new HashSet<string> { "StepD", "final_output" },    // explicit allowlist + upstream deps
};
```

All properties are optional. Omit them to run all flows without slicing:

```csharp
var options = new ExecutionOptions(); // No slicing — all flows execute
```

## How Slicing Works

Slicing guarantees runnability — the result is always a valid sub-DAG:

1. **Flows** restricts to steps whose label begins with the specified flow name prefix (e.g., `"DataScience.TrainModel"`).
2. **Only** resolves each label to a step (or its producing step), then expands upstream to include all transitive dependencies.
3. **From** resolves each label to a step (or all steps consuming that catalog item), then expands downstream.
4. **To** resolves each label to a step (or its producing step), then expands upstream.
5. Multiple flags intersect — each narrows the result set.

**Slicing is additive only.** There is no `--except` flag because subtractive operations break the runnability guarantee.

## Cross-Flow Queries

The unified execution model enables cross-flow data lineage queries:

```bash
# Find which flow produces a catalog item
dotnet run --to intermediate_report

# Find all consumers of a shared data asset
dotnet run --from validated_companies
```

These queries search the merged DAG of all flows, making cross-flow dependencies transparent.

## Errors

Slicing validates during pre-flight:

```
✗ Flows filter matched no steps. Specified: Typo
```

```
✗ From references 'InvalidStep' which does not match any step label
  or catalog item consumed by any step in the flow.
```

```
✗ To references 'input_a' which does not match any step label
  or catalog item produced by any step in the flow.
```

Invalid slices fail before execution begins, consistent with Flowthru's fail-fast philosophy.
