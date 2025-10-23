# How-to: Configure Dataset Inspection

## Overview

Flowthru provides an **eager schema validation** system that inspects external data sources (Layer 0 inputs) before pipeline execution begins. This fail-fast approach catches data quality issues early, saving computation time and providing clear error messages.

## When to Use Inspection

### Use Cases for Shallow Inspection (Default)

Shallow inspection validates:
- File existence
- File format correctness (e.g., valid CSV, Excel, or Parquet structure)
- Schema compatibility (columns match expected types)
- Sample row deserialization (default: 10 rows)

**Best for:**
- Development and testing
- Fast CI/CD pipelines
- Known-good data sources
- Quick validation before expensive computations

### Use Cases for Deep Inspection (Opt-in)

Deep inspection validates:
- Everything in shallow inspection
- **All rows** in the dataset for type compatibility

**Best for:**
- Production runs with external/untrusted data
- Critical pipelines where data quality is paramount
- First-time data ingestion
- Auditing and compliance requirements

### When to Disable Inspection

Set inspection level to `None` when:
- Data source is known to be valid
- Performance is critical
- The catalog entry doesn't support inspection
- You're handling validation elsewhere in the pipeline

## Quick Start

### Enable Deep Inspection for Critical Inputs

```csharp
builder.RegisterPipeline<MyCatalog>("data_processing", DataProcessingPipeline.Create)
  .WithValidation(validation => {
    // Opt into deep inspection for critical external data sources
    validation.Inspect(catalog.Companies, InspectionLevel.Deep);
    validation.Inspect(catalog.Shuttles, InspectionLevel.Deep);
    validation.Inspect(catalog.Reviews, InspectionLevel.Deep);
  });
```

### Use Default Shallow Inspection

If you don't call `WithValidation()`, all Layer 0 inputs that implement `IShallowInspectable<T>` will be inspected at the `Shallow` level by default.

```csharp
// No WithValidation() call - uses smart defaults
builder.RegisterPipeline<MyCatalog>("data_processing", DataProcessingPipeline.Create)
  .WithDescription("Preprocesses raw data");
// Layer 0 inputs will be inspected with Shallow level automatically
```

### Explicitly Disable Inspection

```csharp
builder.RegisterPipeline<MyCatalog>("data_processing", DataProcessingPipeline.Create)
  .WithValidation(validation => {
    // Explicitly disable inspection for known-good source
    validation.Inspect(catalog.CachedResults, InspectionLevel.None);
  });
```

## Inspection Levels Explained

| Level       | Validates                                                          | Performance                       | When to Use                                          |
| ----------- | ------------------------------------------------------------------ | --------------------------------- | ---------------------------------------------------- |
| **None**    | Nothing (skips inspection)                                         | Instant                           | Known-good data, performance-critical paths          |
| **Shallow** | File exists, format valid, schema matches, sample rows deserialize | Fast (seconds)                    | Development, testing, CI/CD, most production cases   |
| **Deep**    | Everything in Shallow + all rows deserialize correctly             | Slow (minutes for large datasets) | Production with untrusted data, first-time ingestion |

## How Inspection Works

### 1. Pipeline Registration

When you register a pipeline and call `WithValidation()`, you configure inspection levels for specific catalog entries:

```csharp
builder.RegisterPipeline<MyCatalog>("my_pipeline", MyPipeline.Create)
  .WithValidation(validation => {
    validation.Inspect(catalog.InputData, InspectionLevel.Deep);
  });
```

### 2. DAG Analysis (Build Phase)

Flowthru analyzes the pipeline DAG and identifies **Layer 0 nodes** — nodes with no dependencies that read external data:

```plaintext
Layer 0: [PreprocessCompanies, PreprocessShuttles, PreprocessReviews]
Layer 1: [CreateModelInputTable]
Layer 2: [TrainModel]
```

### 3. External Input Extraction

Flowthru extracts all **unique catalog entries** that Layer 0 nodes read from:

```plaintext
External Inputs: [companies, shuttles, reviews]
```

**Important:** Intermediate outputs (e.g., `preprocessed_companies`) are **never inspected** because they don't exist yet.

### 4. Inspection Execution

Before pipeline execution, each external input is inspected based on its configured or default level:

```plaintext
Inspecting 'companies' with Deep inspection...
  ✓ File exists
  ✓ Valid CSV format
  ✓ Headers match schema
  ✓ All 10,019 rows validated
```

### 5. Fail-Fast on Errors

If any validation fails, Flowthru throws a `ValidationException` with all errors aggregated:

```plaintext
ValidationException: Pipeline validation failed with 2 error(s):
  - companies: SchemaMismatch: Column 'revenue' expected type 'Double' but found 'String' in row 123
  - shuttles: NotFound: File 'Data/Datasets/shuttles.xlsx' does not exist
```

The pipeline **never executes** if validation fails, saving computation time and providing clear error messages upfront.

## Format-Specific Inspection Behavior

### CSV Datasets

**Shallow Inspection:**
- Validates file exists
- Checks CSV is parseable
- Validates headers match `[Name]` attributes on model properties
- Checks all expected columns are present
- Deserializes first 10 rows (default sample size)

**Deep Inspection:**
- Everything in shallow
- Deserializes **all rows** to validate type compatibility

**Example:**

```csharp
// Model with [Name] attributes
public class Company {
  [Name("id")]
  public string Id { get; set; }
  
  [Name("company_rating")]
  public double Rating { get; set; }
  
  [Name("iata_approved")]
  public bool IataApproved { get; set; }
}

// CSV file must have these exact column names:
// id,company_rating,iata_approved
```

### Excel Datasets

**Shallow Inspection:**
- Validates file exists
- Checks Excel file is parseable
- Validates worksheet exists (uses first sheet by default)
- Checks all expected columns are present
- Deserializes first 10 rows (default sample size)

**Deep Inspection:**
- Everything in shallow
- Deserializes **all rows** to validate type compatibility

### Parquet Datasets

**Shallow Inspection:**
- Validates file exists
- Checks magic bytes "PAR1" are present
- Deserializes first 10 rows (default sample size)

**Deep Inspection:**
- Everything in shallow
- Deserializes **all rows** to validate type compatibility

**Note:** Parquet.NET loads the entire file into memory, so shallow vs deep inspection has similar performance characteristics. The distinction is semantic: shallow validates a sample, deep validates everything.

## Default Behavior Reference

### Resolution Logic

For each Layer 0 input, Flowthru determines the inspection level using this priority:

1. **Explicitly configured** via `WithValidation()` → use that level
2. **Entry implements** `IShallowInspectable<T>` → use `Shallow`
3. **Otherwise** → use `None` (skip inspection)

### Built-in Support

All Flowthru catalog dataset types support shallow and deep inspection by default:

- ✅ `CsvCatalogDataset<T>` - Implements `IShallowInspectable<T>`, `IDeepInspectable<T>`
- ✅ `ExcelCatalogDataset<T>` - Implements `IShallowInspectable<T>`, `IDeepInspectable<T>`
- ✅ `ParquetCatalogDataset<T>` - Implements `IShallowInspectable<T>`, `IDeepInspectable<T>`
- ❌ `MemoryCatalogDataset<T>` - Does not implement inspection interfaces (in-memory data doesn't need validation)

### Custom Catalog Entries

If you create custom catalog entries, they'll use `None` (skip) by default unless you implement the inspection interfaces.

See: [Implementing Custom Inspection Logic](./custom-inspection.md) *(placeholder for future guide)*

## Performance Implications

### Shallow Inspection Cost

Typical overhead for shallow inspection:

- **CSV (10K rows):** +50-200ms (reads 10 rows)
- **Excel (10K rows):** +100-300ms (reads 10 rows)
- **Parquet (10K rows):** +50-100ms (reads 10 rows)

**Total for 3 inputs:** ~500ms additional startup time

This is negligible compared to pipeline execution time and provides high confidence in data quality.

### Deep Inspection Cost

Typical overhead for deep inspection:

- **CSV (10K rows):** +500-2000ms (reads all rows)
- **Excel (10K rows):** +1000-3000ms (reads all rows)
- **Parquet (10K rows):** +300-800ms (reads all rows)

**Total for 3 inputs (30K rows):** ~5-10 seconds additional startup time

Deep inspection can be expensive for large datasets (100K+ rows). Use strategically for critical production inputs.

### Optimization Tips

1. **Use Shallow for Development/CI**
   ```csharp
   // Fast feedback loop during development
   // Deep inspection only in production
   #if DEBUG
   validation.Inspect(catalog.BigDataset, InspectionLevel.Shallow);
   #else
   validation.Inspect(catalog.BigDataset, InspectionLevel.Deep);
   #endif
   ```

2. **Selective Deep Inspection**
   ```csharp
   // Deep inspect only critical/small inputs
   validation.Inspect(catalog.ConfigData, InspectionLevel.Deep);  // Small, critical
   validation.Inspect(catalog.LargeDataset, InspectionLevel.Shallow);  // Large, less critical
   ```

3. **Skip Inspection for Trusted Sources**
   ```csharp
   // Skip inspection for known-good cached data
   validation.Inspect(catalog.PreprocessedCache, InspectionLevel.None);
   ```

## Example: Real-World Configuration

```csharp
// Production pipeline with strategic inspection levels
builder.RegisterPipeline<SpaceflightsCatalog>("DataProcessing", DataProcessingPipeline.Create)
  .WithDescription("Preprocesses raw data and creates model input table")
  .WithValidation(validation => {
    // Critical external data - deep inspect
    validation.Inspect(catalog.Companies, InspectionLevel.Deep);
    validation.Inspect(catalog.Shuttles, InspectionLevel.Deep);
    
    // Large dataset, less critical - shallow inspect
    validation.Inspect(catalog.Reviews, InspectionLevel.Shallow);
  });

builder.RegisterPipeline<SpaceflightsCatalog>("DataScience", DataSciencePipeline.Create, params)
  .WithDescription("Trains ML model")
  .WithValidation(validation => {
    // This pipeline reads model_input_table (Layer 1 output from DataProcessing)
    // No inspection needed - it doesn't exist yet!
    // Flowthru automatically skips intermediate outputs
  });
```

## Troubleshooting

### "Pipeline validation failed with X error(s)"

The `ValidationException` message includes all errors grouped by catalog entry. Example:

```plaintext
ValidationException: Pipeline validation failed with 3 error(s):

[companies]
  - SchemaMismatch: Expected column 'company_rating' of type 'Double' but found 'String'
  - TypeMismatch: Failed to deserialize row 123: Cannot convert 'N/A' to Double

[shuttles]
  - NotFound: File 'Data/Datasets/shuttles.xlsx' does not exist
```

**Fix:**
1. Check file paths and existence
2. Verify CSV/Excel column names match `[Name]` attributes
3. Ensure data types in files match C# model types
4. Fix data quality issues (nulls, invalid values, etc.)

### "Entry does not implement IShallowInspectable&lt;T&gt;"

You configured deep or shallow inspection for a catalog entry that doesn't support it.

**Fix:**
1. Check if the entry type supports inspection (see "Built-in Support" above)
2. For custom entries, implement `IShallowInspectable<T>` and/or `IDeepInspectable<T>`
3. Or set inspection level to `None` to skip validation

### Inspection is Too Slow

If deep inspection takes too long:

**Fix:**
1. Use `Shallow` inspection for large datasets during development
2. Reserve `Deep` inspection for production or critical data
3. Cache validated data to avoid re-inspection
4. Consider data quality improvements at the source

### Inspection Passes But Pipeline Still Fails

Inspection validates:
- File existence and format
- Schema compatibility
- Type deserialization

Inspection does NOT validate:
- Business logic (e.g., "revenue must be positive")
- Data completeness (e.g., "no missing values allowed")
- Cross-dataset consistency (e.g., "all company IDs in shuttles must exist in companies")

**For these cases:**
- Implement validation nodes in your pipeline
- Add custom validation logic to your catalog entry's `InspectDeep()` override
- Use data quality libraries (e.g., Great Expectations, Pandera)

## Next Steps

- **[Implementing Custom Inspection Logic](./custom-inspection.md)** - Add inspection to custom catalog entries *(placeholder)*
- **[Pipeline Parameters](./pipeline-parameters.md)** - Pass parameters to pipelines
- **[Logging and Dependencies](./logging-dependencies.md)** - Configure logging and dependency injection
