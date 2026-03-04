# Flowthru Item Templates

This directory contains item templates for scaffolding components within existing Flowthru projects.

## Pipeline Template

The pipeline template creates a new pipeline with a starter node structure.

### Usage

From within your Flowthru project's `Pipelines/` directory:

```bash
cd Pipelines
dotnet new flowthru-pipeline --name DataProcessing --ProjectName YourProjectName
```

**Parameters:**
- `--name` (required) - The name of your pipeline (e.g., "DataProcessing", "DataScience")
- `--ProjectName` (optional) - Your project's root namespace. If omitted, defaults to "ProjectName" and must be manually replaced in generated files.

### What Gets Generated

```
DataProcessing/
├── DataProcessingPipeline.cs       # Pipeline definition with dummy node
└── Nodes/
    └── DataProcessingDummyNode.cs  # Placeholder node (NoData → NoData)
```

### Next Steps After Generation

1. **Replace namespace if needed**: If you didn't provide `--ProjectName`, replace `ProjectName` with your actual project namespace in the generated files.

2. **Register the pipeline** in `Program.cs`:
   ```csharp
   flowthru.RegisterPipeline<DataProcessingPipeline>();
   ```

3. **Replace the dummy node** with actual transformation logic:
   - Update input/output types from `NoData` to your actual schemas
   - Update catalog entry references in the pipeline
   - Implement transformation logic in the node

### Example: Converting from NoData to Real Schemas

**Before (generated):**
```csharp
pipeline.AddNode(
  label: "DataProcessingDummy",
  description: "Placeholder node - replace with actual transformation logic.",
  transform: DataProcessingDummyNode.Create(),
  input: catalog.NoData,
  output: catalog.NoData
);
```

**After (with real schemas):**
```csharp
pipeline.AddNode(
  label: "PreprocessCompanies",
  description: "Clean and validate company data.",
  transform: PreprocessCompaniesNode.Create(),
  input: catalog.CompaniesRaw,
  output: catalog.CompaniesIntermediate
);
```

## Notes

- Item templates should be run from the `Pipelines/` directory of your Flowthru project
- The generated pipeline uses `NoData` input/output so it can be registered and tested immediately
- Multiple nodes can be added by creating additional node files in the `Nodes/` directory
