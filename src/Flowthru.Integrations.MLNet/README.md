# Flowthru.Integrations.MLNet

ML.NET integration for Flowthru - provides IDataView container adapters for machine learning pipelines.

## Purpose

This integration library enables seamless integration between Flowthru's data catalog system and ML.NET's machine learning framework. It provides container adapters that bridge Flowthru's streaming row abstraction with ML.NET's columnar IDataView representation.

## Key Components

### DataViewContainerAdapter<T>

Container adapter that converts between `IAsyncEnumerable<T>` rows and ML.NET's `IDataView` columnar format.

**Features:**
- Native ML.NET integration
- Type-safe row schema
- Lazy evaluation support
- Optimized for ML.NET pipelines

**Usage:**

```csharp
using Flowthru.Integrations.MLNet.Container;
using Microsoft.ML;

public record FeatureRow(
    float Feature1,
    float Feature2,
    int Label
) : IFlatSchema, ITextSerializable;

var mlContext = new MLContext();
var adapter = new DataViewContainerAdapter<FeatureRow>(mlContext);

// Load data from Flowthru catalog
var dataView = await adapter.FromRows(rowStream);

// Use with ML.NET pipelines
var pipeline = mlContext.Transforms
    .NormalizeMinMax("Feature1")
    .Append(mlContext.Transforms.NormalizeMinMax("Feature2"));

var model = pipeline.Fit(dataView);
```

## Dependencies

- **Flowthru** - Core data catalog and storage abstractions
- **Microsoft.ML** - ML.NET framework
- **LanguageExt.Core** - Functional programming primitives

## Integration with ML.Next

This adapter bridges Flowthru catalogs with ML.Next's type-safe wrappers, enabling end-to-end compile-time safety for ML pipelines.

## Architecture

This is an **optional integration** - the core Flowthru library does not depend on ML.NET. Users who need ML.NET support can add this package as a separate dependency.

```
Flowthru (core)
    ↑
    │ depends on
    │
Flowthru.Integrations.MLNet (optional)
    ↑
    │ depends on
    │
Microsoft.ML
```

## Version

0.1.0 (Pre-alpha)
