---
name: flowthru
description: Use when authoring or modifying Flowthru data pipelines — creating flows, schemas, catalog items, or steps in a Flowthru project. Covers project structure (the layered Data/ directories from _01_Raw to _08_Reporting), the catalog/schema/step/flow model, and the end-to-end workflow for going from input data to output data.
---

# Flowthru Flow Agent Guide

You are working inside a Flowthru data pipeline project. Flowthru is a type-safe data engineering framework for .NET. You have baseline knowledge of data pipelines, DAGs, and ETL — this guide tells you how to express those concepts in Flowthru's structure.

Exact syntax for schemas, catalog items, steps, and flows can be gleaned from the existing code in this project. Use the structure below to locate examples, and follow the workflow to complete tasks.

## Project Structure

```
Program.cs                          # Entrypoint — flows are registered here
                                    # Project runs as a Flowthru CLI service via `dotnet run`
Data/                               # Everything related to data definitions
├── _01_Raw/                        # Immutable source data — never mutated, only copied from
│   ├── Catalog.Raw.cs              # Catalog items for this layer
│   └── Schemas/                    # Schemas referenced by catalog items
│       └── *.cs
├── _02_Intermediate/               # Typed mirror of raw data — cleaned, parsed, typed formats
│   ├── Catalog.Intermediate.cs
│   └── Schemas/
│       └── *.cs
├── _03_Primary/                    # Domain data — restructured for the problem being solved
├── _04_Feature/                    # Engineered features derived from primary data
├── _05_ModelInput/                 # Joined feature sets ready for model consumption
├── _06_Models/                     # Serialized trained models
├── _07_ModelOutput/                # Results and predictions from model runs
└── _08_Reporting/                  # Descriptive and summary outputs for business consumption

Flows/                              # All flows
└── <FlowName>/
    ├── <FlowName>Flow.cs       # Steps wired to input/output catalog items
    └── Steps/
        └── *.cs                    # Individual transformation steps
```

**Key relationships:**

- Each **catalog item** references a **schema** type and declares a **storage strategy** (CSV, Parquet, EFCore, etc.).
- Each **step** declares typed inputs and outputs that match schema types.
- Each **flow** wires steps to catalog items, forming the DAG.
- `Program.cs` registers flows so the CLI runner can discover and execute them.

## Workflow

Tasks in Flowthru are ordinarily of the form: *"I have X input data, and I want to transform it to Y output data."*

Follow these steps in order:

1. **Plan transformation steps.** Determine what discrete transformations are needed to get from the input data to the output data.
2. **Plan intermediate schemas.** Identify what data shapes are needed between each transformation step.
3. **Write schemas.** Create schema classes for input, output, and all intermediary data in the appropriate `Data/<Layer>/Schemas/` directories.
4. **Create catalog items.** Add catalog items for each schema in the corresponding `Catalog.<Layer>.cs` file. Each item declares its schema type and storage strategy.
5. **Write steps.** Implement the transformation logic in `Flows/<FlowName>/Steps/`. Each step declares typed inputs and outputs matching the schemas from step 3.
6. **Wire the flow.** Connect steps to their catalog items in `Flows/<FlowName>/<FlowName>Flow.cs`.
7. **Register the flow.** Add the flow to `Program.cs` so the CLI runner can discover it.
8. **Run and confirm.** Execute the flow with `dotnet run` and verify the output.