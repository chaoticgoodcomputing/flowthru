# ML.Next

**Type-safe, functional wrapper for Microsoft.ML focused on compile-time safety and ETL pipeline construction**

ML.Next provides a thin, principled facade over ML.NET's data transformation capabilities using LanguageExt v5's type system and monadic error handling. The library emphasizes:

- **Compile-time schema tracking** via phantom types
- **Functional composition** with type-safe transformations
- **Explicit error handling** using the `Fin<T>` monad
- **Accumulated validation** using the `Validation` monad
- **Zero-cost abstractions** with record structs and traits

## Quick Start

```csharp
using ML.Next;
using ML.Next.Core.Schema;
using ML.Next.Extract;
using ML.Next.Transform;
using Microsoft.ML;

// Define phantom types for compile-time schema tracking
public struct RawDataSchema : ISchemaDefinition { }
public struct ProcessedSchema : ISchemaDefinition { }
public struct ModelSchema : ISchemaDefinition { }

var context = new MLContext();

// Load data with schema tracking
var dataResult = DataLoader.LoadFromTextFile<RawDataSchema>(
    context,
    path: "data.csv",
    hasHeader: true,
    separatorChar: ',');

// dataResult is Fin<DataView<RawDataSchema>>
dataResult.Match(
    Succ: data =>
    {
        // Build type-safe transformation pipeline
        var normalizeEstimator = ColumnTransforms.NormalizeMinMax<RawDataSchema, ProcessedSchema>(
            context, "SepalLength");
        
        var transformerResult = normalizeEstimator.Fit(data);
        
        transformerResult.Match(
            Succ: transformer =>
            {
                // Transformer<RawDataSchema, ProcessedSchema>
                var transformedResult = transformer.Transform(data);
                // Returns Fin<DataView<ProcessedSchema>>
            },
            Fail: err => Console.WriteLine($"Fit failed: {err}")
        );
    },
    Fail: err => Console.WriteLine($"Load failed: {err}")
);
```

## Architecture

The library is organized around the **Extract-Transform-Load (ETL)** pattern:

### Extract Phase (`Extract/`)

Data loading with schema validation:

- **`DataLoader`**: Load data from enumerables, text files, or wrap existing `IDataView`
- Returns `Fin<DataView<TSchema>>` for explicit error handling

```csharp
var dataResult = DataLoader.LoadFromEnumerable<MySchema, MyDataClass>(
    context, 
    data,
    schemaDefinition);

var fileResult = DataLoader.LoadFromTextFile<MySchema>(
    context,
    path: "file.csv",
    hasHeader: true);
```

### Transform Phase (`Transform/`)

Type-safe transformations with compile-time schema tracking:

- **`Transformer<TSchemaIn, TSchemaOut>`**: Fitted, immutable transformation
- **`Estimator<TSchemaIn, TSchemaOut>`**: Learnable transformation requiring fitting
- **`PipelineBuilder`**: Fluent API for composing transformations
- **`ColumnTransforms`**: Common operations (concatenate, normalize, encode)

```csharp
// Estimator composition (type-safe!)
var estimator1 = ColumnTransforms.NormalizeMinMax<Schema1, Schema2>(context, "col1");
var estimator2 = ColumnTransforms.MapValueToKey<Schema2, Schema3>(context, "col2");

var combined = estimator1.Append(estimator2);
// Type: Estimator<Schema1, Schema3>

// Transformer composition
var transformer = combined.Fit(trainingData).ThrowIfFail();
var composed = transformer1.Append(transformer2);
// Type: Transformer<Schema1, Schema3>

// Fluent pipeline
var builder = PipelineBuilder<RawSchema>
    .Create()
    .Then(transformer1)
    .Then(transformer2)
    .Build();
```

### Load Phase (`Load/`)

Model persistence and prediction:

- **`ModelPersistence`**: Save/load models with schema metadata
- **`PredictionEngine<TInput, TOutput>`**: Type-safe prediction with error handling

```csharp
// Save model
ModelPersistence.SaveModel(
    context,
    transformer,
    trainingData,
    "model.zip");

// Load model
var loadedModel = ModelPersistence.LoadModel<InputSchema, OutputSchema>(
    context,
    "model.zip");

// Create prediction engine
var engineResult = PredictionEngine<InputClass, OutputClass>.Create(
    context,
    transformer);

engineResult.Match(
    Succ: engine =>
    {
        var prediction = engine.Predict(input);
        // Returns Fin<OutputClass>
    },
    Fail: err => Console.WriteLine($"Engine creation failed: {err}")
);
```

### Validation Phase (`Validation/`)

Accumulated error reporting:

- **`SchemaValidator`**: Validate schema requirements, column types, compatibility
- **`PipelineValidator`**: End-to-end pipeline validation

```csharp
// Schema validation with accumulated errors
var validation = SchemaValidator.ValidateSchema(
    dataView.Underlying,
    new ColumnRequirement("SepalLength", typeof(float), IsRequired: true),
    new ColumnRequirement("Species", typeof(string), IsRequired: true));

validation.Match(
    Succ: _ => Console.WriteLine("Schema valid"),
    Fail: errors => 
    {
        // All errors accumulated
        foreach (var err in errors)
        {
            Console.WriteLine($"- {err.Message}");
        }
    });

// Pipeline validation
var pipelineValidation = PipelineValidator.ValidatePipeline(
    dataLoader: () => DataLoader.LoadFromTextFile<RawSchema>(context, "data.csv"),
    transformer: myTransformer);
```

## Core Types (`Core/`)

### Schema Tracking

Phantom types enable compile-time schema validation:

```csharp
// Define schema markers
public struct IrisRawSchema : ISchemaDefinition { }
public struct IrisNormalizedSchema : ISchemaDefinition { }

// DataView<TSchema> wraps IDataView with compile-time tracking
DataView<IrisRawSchema> raw = ...;
Transformer<IrisRawSchema, IrisNormalizedSchema> normalizer = ...;

var normalized = normalizer.Transform(raw);
// Type: Fin<DataView<IrisNormalizedSchema>>

// Compile error if schemas don't match!
Transformer<OtherSchema, OutputSchema> wrong = ...;
wrong.Transform(raw); // ❌ Type error!
```

### Columns

Strongly-typed column names with LanguageExt v5 traits:

```csharp
// ColumnName<TType> implements Identifier<T> trait
var sepalLength = ColumnName<float>.Create("SepalLength");
var species = ColumnName<string>.Create("Species");

// Type-safe column transformations
var asInt = sepalLength.As<int>();
```

### Annotations

Type-safe metadata tracking:

```csharp
public readonly record struct MyAnnotations : IAnnotations
{
    public Option<NormalizedAnnotation> Normalized { get; init; }
    public Option<SlotNamesAnnotation> SlotNames { get; init; }
}
```

## Error Handling

All fallible operations return `Fin<T>` from LanguageExt:

```csharp
// Pattern match on result
result.Match(
    Succ: value => UseValue(value),
    Fail: err => LogError(err)
);

// Throw if failed (for prototyping)
var value = result.ThrowIfFail();

// Bind operations (monadic composition)
var final = result
    .Bind(value => Transform(value))
    .Bind(transformed => Save(transformed));
```

Validation uses `Validation<Error, T>` to accumulate all errors:

```csharp
var v1 = ValidateField1();
var v2 = ValidateField2();
var v3 = ValidateField3();

// Applicative combination accumulates ALL errors
var combined = (v1, v2, v3).Apply((f1, f2, f3) => CreateObject(f1, f2, f3));

combined.Match(
    Succ: obj => Use(obj),
    Fail: errors => 
    {
        // Seq<Error> contains all validation failures
        foreach (var err in errors)
        {
            Log(err);
        }
    });
```

## Design Principles

### 1. Phantom Types for Schema Safety

Schema types are markers with no runtime representation:

```csharp
public struct MySchema : ISchemaDefinition { }
```

The type system tracks schemas through transformations:

```csharp
Transformer<Schema1, Schema2> t1;
Transformer<Schema2, Schema3> t2;

var composed = t1.Append(t2);
// Type: Transformer<Schema1, Schema3>
// Compiler verifies Schema2 compatibility!
```

### 2. Monadic Error Handling

`Fin<T>` makes errors explicit in the type system:

- **No exceptions** for expected failures
- **Explicit handling** required via pattern matching
- **Composable** via `Bind`, `Map`, `Match`

### 3. LanguageExt v5 Traits

Use traits for semantic type identity:

- **`Identifier<T>`** for column names (structural equality, implicit conversions)
- **`DomainType<SELF, REPR>`** for value wrappers
- **Higher-kinded types** for generic abstractions

### 4. Zero-Cost Abstractions

- `record struct` for value semantics without heap allocation
- Phantom types erased at runtime
- Thin wrappers around ML.NET types

## API Reference

### Extract Phase

| Type                | Purpose                        | Key Methods                                      |
| ------------------- | ------------------------------ | ------------------------------------------------ |
| `DataLoader`        | Load data with schema tracking | `LoadFromEnumerable`, `LoadFromTextFile`, `Wrap` |
| `DataView<TSchema>` | Schema-tracked data view       | `ToOption`, `Underlying`                         |

### Transform Phase

| Type                       | Purpose                      | Key Methods                                       |
| -------------------------- | ---------------------------- | ------------------------------------------------- |
| `Transformer<TIn, TOut>`   | Fitted transformation        | `From`, `Transform`, `Append`                     |
| `Estimator<TIn, TOut>`     | Learnable transformation     | `From`, `Fit`, `Append`                           |
| `PipelineBuilder<TSchema>` | Fluent pipeline construction | `Create`, `Then`, `Build`                         |
| `ColumnTransforms`         | Common transformations       | `Concatenate`, `NormalizeMinMax`, `MapValueToKey` |

### Load Phase

| Type                          | Purpose              | Key Methods              |
| ----------------------------- | -------------------- | ------------------------ |
| `ModelPersistence`            | Save/load models     | `SaveModel`, `LoadModel` |
| `PredictionEngine<TIn, TOut>` | Type-safe prediction | `Create`, `Predict`      |

### Validation Phase

| Type                | Purpose                     | Key Methods                                     |
| ------------------- | --------------------------- | ----------------------------------------------- |
| `SchemaValidator`   | Schema validation           | `ValidateSchema`, `ValidateSchemaCompatibility` |
| `PipelineValidator` | Pipeline validation         | `ValidatePipeline`, `ValidateEstimatorFit`      |
| `ColumnRequirement` | Runtime schema requirements | Constructor                                     |

## Integration with Flowthru

This library provides the ML.NET integration layer for Flowthru's data pipeline system:

1. **Schema definitions** map to Flowthru's catalog schemas
2. **Transformers** become Flowthru pipeline nodes
3. **Validation** integrates with Flowthru's validation system
4. **Error handling** aligns with Flowthru's error model

## Dependencies

- **.NET 9.0** - Target framework
- **Microsoft.ML 4.0.3** - Core ML.NET library
- **LanguageExt.Core 5.0.0-beta-54** - Functional programming utilities

## Examples

See `examples/FlowthruIris/` for a complete end-to-end example using the Iris dataset.

## Testing

Run tests with:

```bash
nx test ML.Next
```

## License

See LICENSE file in repository root.
