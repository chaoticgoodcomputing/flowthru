# Flowthru Pipeline Agent Guide

You are working inside a Flowthru data pipeline project. Flowthru is a type-safe data engineering framework for .NET. You have baseline knowledge of data pipelines, DAGs, and ETL — this guide tells you how to express those concepts in Flowthru's structure.

Exact syntax for schemas, catalog entries, nodes, and pipelines can be gleaned from the existing code in this project. Use the structure below to locate examples, and follow the workflow to complete tasks.

## Project Structure

```
Program.cs                          # Entrypoint — pipelines are registered here
                                    # Project runs as a Flowthru CLI service via `dotnet run`
Data/                               # Everything related to data definitions
├── _01_Raw/                        # Immutable source data — never mutated, only copied from
│   ├── Catalog.Raw.cs              # Catalog entries for this layer
│   └── Schemas/                    # Schemas referenced by catalog entries
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

Pipelines/                          # All pipelines
└── <PipelineName>/
    ├── <PipelineName>Pipeline.cs   # Nodes wired to input/output catalog entries
    └── Nodes/
        └── *.cs                    # Individual transformation nodes
```

**Key relationships:**

- Each **catalog entry** references a **schema** type and declares a **storage strategy** (CSV, Parquet, EFCore, etc.).
- Each **node** declares typed inputs and outputs that match schema types.
- Each **pipeline** wires nodes to catalog entries, forming the DAG.
- `Program.cs` registers pipelines so the CLI runner can discover and execute them.

## Workflow

Tasks in Flowthru are ordinarily of the form: *"I have X input data, and I want to transform it to Y output data."*

Follow these steps in order:

1. **Plan transformation nodes.** Determine what discrete transformations are needed to get from the input data to the output data.
2. **Plan intermediate schemas.** Identify what data shapes are needed between each transformation step.
3. **Write schemas.** Create schema classes for input, output, and all intermediary data in the appropriate `Data/<Layer>/Schemas/` directories.
4. **Create catalog entries.** Add catalog entries for each schema in the corresponding `Catalog.<Layer>.cs` file. Each entry declares its schema type and storage strategy.
5. **Write nodes.** Implement the transformation logic in `Pipelines/<PipelineName>/Nodes/`. Each node declares typed inputs and outputs matching the schemas from step 3.
6. **Wire the pipeline.** Connect nodes to their catalog entries in `Pipelines/<PipelineName>/<PipelineName>Pipeline.cs`.
7. **Register the pipeline.** Add the pipeline to `Program.cs` so the CLI runner can discover it.
8. **Run and confirm.** Execute the pipeline with `dotnet run` and verify the output.
