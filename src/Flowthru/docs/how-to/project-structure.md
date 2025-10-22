# How to Structure Your Project

Organize your Flowthru project for maintainability and compile-time safety.

## Recommended Structure

```
MyPipeline/
├── Program.cs                           # Application entry point
├── Data/
│   ├── MyCatalog.cs                     # Catalog class
│   └── Schemas/
│       ├── Raw/
│       │   └── CompanyRawData.cs        # Raw data schemas
│       ├── Processed/
│       │   └── CompanyProcessed.cs      # Processed schemas
│       └── Parameters/
│           └── ModelOptions.cs          # Parameter schemas
├── Pipelines/
│   ├── PipelineRegistry.cs              # Central registry
│   ├── DataProcessing/
│   │   ├── DataProcessingPipeline.cs    # Pipeline factory
│   │   └── Nodes/
│   │       ├── ExtractNode.cs
│   │       ├── TransformNode.cs
│   │       └── LoadNode.cs
│   └── DataScience/
│       ├── DataSciencePipeline.cs
│       └── Nodes/
│           ├── SplitDataNode.cs
│           ├── TrainModelNode.cs
│           └── EvaluateModelNode.cs
└── Data/
    └── Datasets/                        # Actual data files
        ├── 01_Raw/
        ├── 02_Intermediate/
        └── 03_Primary/
```

## Key Principles

- **Schemas grouped by processing stage:** Raw, Processed, Parameters
- **Pipelines grouped by domain:** DataProcessing, DataScience
- **Nodes colocated with their pipeline:** Makes dependencies obvious
- **One catalog class per project:** Use inheritance for variations
