# Working with Read-Only Data Sources

## Problem

You need to read data from sources that don't support writing (Excel files, database views, HTTP APIs), but want to prevent runtime errors when accidentally trying to use them as pipeline outputs.

## Solution

Use `IReadableCatalogDataset<T>` or `IReadableCatalogObject<T>` instead of the full read-write interfaces. This provides **compile-time safety** by preventing read-only sources from being used as outputs.

---

## Step 1: Identify Read-Only Sources

Common read-only data sources:
- **Excel files** - ExcelDataReader library doesn't support writing
- **Database views** - Read-only by design
- **HTTP APIs** - GET endpoints that return data
- **Immutable reference data** - Configuration files, lookup tables
- **External data feeds** - Third-party data sources

---

## Step 2: Declare Read-Only Catalog Properties

Use `IReadableCatalogDataset<T>` for collections and `IReadableCatalogObject<T>` for singletons:

```csharp
using Flowthru.Data;
using Flowthru.Data.Implementations;

public class MyCatalog : DataCatalogBase
{
    // Read-only Excel dataset
    public IReadableCatalogDataset<ShuttleRawSchema> Shuttles =>
        GetOrCreateReadOnlyDataset(() => 
            new ExcelCatalogDataset<ShuttleRawSchema>(
                "shuttles", 
                $"{BasePath}/shuttles.xlsx", 
                "Sheet1"));

    // Read-write Parquet dataset (for outputs)
    public ICatalogDataset<ShuttleSchema> CleanedShuttles =>
        GetOrCreateDataset(() => 
            new ParquetCatalogDataset<ShuttleSchema>(
                "cleaned_shuttles", 
                $"{BasePath}/cleaned_shuttles.parquet"));

    // Read-only configuration object
    public IReadableCatalogObject<ModelConfig> Config =>
        GetOrCreateReadOnlyObject(() => 
            new JsonReadOnlyCatalogObject<ModelConfig>("config"));

    protected string BasePath { get; }

    public MyCatalog(string basePath)
    {
        BasePath = basePath;
    }
}
```

**Key Points:**
- Use `IReadableCatalogDataset<T>` instead of `ICatalogDataset<T>` for read-only datasets
- Use `GetOrCreateReadOnlyDataset()` helper method
- Use `IReadableCatalogObject<T>` instead of `ICatalogObject<T>` for read-only singletons

---

## Step 3: Use Read-Only Sources in Pipelines

Read-only sources can **only be used as inputs**, never as outputs:

```csharp
public static class DataProcessingPipeline
{
    public static Pipeline Create(MyCatalog catalog)
    {
        return PipelineBuilder.CreatePipeline(pipeline =>
        {
            // ✅ CORRECT: Read-only source as input
            pipeline.AddNode<PreprocessShuttlesNode>(
                input: catalog.Shuttles,           // IReadableCatalogDataset<T> - OK
                output: catalog.CleanedShuttles,   // ICatalogDataset<T> - OK
                name: "PreprocessShuttles"
            );

            // ❌ COMPILE ERROR: Cannot use read-only source as output
            // pipeline.AddNode<SomeNode>(
            //     input: catalog.SomeInput,
            //     output: catalog.Shuttles,        // ❌ IReadableCatalogDataset<T> - ERROR!
            //     name: "WontCompile"
            // );
        });
    }
}
```

**Benefits:**
- Compile-time error if you try to write to a read-only source
- No runtime exceptions for "write not supported"
- Clear intent in catalog declarations
- IntelliSense shows correct available datasets

---

## Step 4: Implement Custom Read-Only Datasets

If you need to create a custom read-only dataset, extend `ReadOnlyCatalogDatasetBase<T>`:

```csharp
using Flowthru.Data;

public class HttpApiCatalogDataset<T> : ReadOnlyCatalogDatasetBase<T>
    where T : new()
{
    private readonly string _apiUrl;
    private readonly HttpClient _httpClient;

    public HttpApiCatalogDataset(string key, string apiUrl, HttpClient httpClient)
        : base(key)
    {
        _apiUrl = apiUrl;
        _httpClient = httpClient;
    }

    public override async Task<IEnumerable<T>> Load()
    {
        var response = await _httpClient.GetStringAsync(_apiUrl);
        var data = JsonSerializer.Deserialize<List<T>>(response);
        return data ?? new List<T>();
    }

    public override Task<bool> Exists()
    {
        // For HTTP APIs, we can check if the endpoint is reachable
        return Task.FromResult(true);
    }
}
```

**Key Points:**
- Extend `ReadOnlyCatalogDatasetBase<T>` (not `CatalogDatasetBase<T>`)
- Implement `Load()` and `Exists()` only
- **No `Save()` method** - completely omitted for compile-time safety
- The base class throws `NotSupportedException` if `SaveUntyped()` is called (edge case)

---

## Step 5: Implement Custom Read-Only Objects

For singleton objects, extend `ReadOnlyCatalogObjectBase<T>`:

```csharp
using Flowthru.Data;

public class JsonReadOnlyCatalogObject<T> : ReadOnlyCatalogObjectBase<T>
{
    private readonly string _filePath;

    public JsonReadOnlyCatalogObject(string key, string filePath)
        : base(key)
    {
        _filePath = filePath;
    }

    public override async Task<T> Load()
    {
        var json = await File.ReadAllTextAsync(_filePath);
        var data = JsonSerializer.Deserialize<T>(json);
        return data ?? throw new InvalidOperationException($"Failed to deserialize {_filePath}");
    }

    public override Task<bool> Exists()
    {
        return Task.FromResult(File.Exists(_filePath));
    }
}
```

---

## Common Patterns

### Pattern 1: Excel Input → Parquet Output

```csharp
// Catalog
public IReadableCatalogDataset<RawData> RawExcel =>
    GetOrCreateReadOnlyDataset(() => 
        new ExcelCatalogDataset<RawData>("raw", "data/raw.xlsx"));

public ICatalogDataset<ProcessedData> ProcessedParquet =>
    GetOrCreateDataset(() => 
        new ParquetCatalogDataset<ProcessedData>("processed", "data/processed.parquet"));

// Pipeline
pipeline.AddNode<ProcessNode>(
    input: catalog.RawExcel,           // ✅ Read-only input
    output: catalog.ProcessedParquet,  // ✅ Read-write output
    name: "Process"
);
```

### Pattern 2: HTTP API Input → CSV Output

```csharp
// Catalog
public IReadableCatalogDataset<ApiResponse> ApiData =>
    GetOrCreateReadOnlyDataset(() => 
        new HttpApiCatalogDataset<ApiResponse>("api", "https://api.example.com/data"));

public ICatalogDataset<ProcessedData> ProcessedCsv =>
    GetOrCreateDataset(() => 
        new CsvCatalogDataset<ProcessedData>("processed", "data/processed.csv"));

// Pipeline
pipeline.AddNode<TransformApiDataNode>(
    input: catalog.ApiData,           // ✅ Read-only input
    output: catalog.ProcessedCsv,     // ✅ Read-write output
    name: "TransformApiData"
);
```

### Pattern 3: Read-Only Configuration Object

```csharp
// Catalog
public IReadableCatalogObject<ModelConfig> Config =>
    GetOrCreateReadOnlyObject(() => 
        new JsonReadOnlyCatalogObject<ModelConfig>("config", "config/model.json"));

public ICatalogDataset<TrainingData> TrainingData =>
    GetOrCreateDataset(() => 
        new ParquetCatalogDataset<TrainingData>("training", "data/training.parquet"));

// Pipeline - multi-input with read-only config
pipeline.AddNode<TrainModelNode>(
    name: "TrainModel",
    input: new CatalogMap<TrainInputs>()
        .Map(x => x.Config, catalog.Config)        // ✅ Read-only config
        .Map(x => x.Training, catalog.TrainingData), // ✅ Read-write data
    output: catalog.Model
);
```

---

## Benefits of Read-Only Interfaces

1. **Compile-Time Safety**: Prevents using read-only sources as outputs
2. **Clear Intent**: Catalog declarations document capabilities
3. **No Runtime Exceptions**: Errors caught during compilation
4. **Better IntelliSense**: Type system guides correct usage
5. **Maintainability**: Future developers understand constraints immediately

---

## Migration from Full Read-Write

If you have existing code using `ICatalogDataset<T>` for read-only sources:

**Before:**
```csharp
public ICatalogDataset<ShuttleData> Shuttles =>
    GetOrCreateDataset(() => 
        new ExcelCatalogDataset<ShuttleData>("shuttles", "shuttles.xlsx"));
```

**After:**
```csharp
public IReadableCatalogDataset<ShuttleData> Shuttles =>
    GetOrCreateReadOnlyDataset(() => 
        new ExcelCatalogDataset<ShuttleData>("shuttles", "shuttles.xlsx"));
```

**Changes Required:**
1. Change property type from `ICatalogDataset<T>` to `IReadableCatalogDataset<T>`
2. Change helper method from `GetOrCreateDataset()` to `GetOrCreateReadOnlyDataset()`
3. Verify the dataset is never used as a pipeline output
4. If it is used as output, keep it as `ICatalogDataset<T>` and use a writable implementation

---

## Summary

- Use `IReadableCatalogDataset<T>` for read-only collections
- Use `IReadableCatalogObject<T>` for read-only singletons
- Use `GetOrCreateReadOnlyDataset()` and `GetOrCreateReadOnlyObject()` helpers
- Extend `ReadOnlyCatalogDatasetBase<T>` or `ReadOnlyCatalogObjectBase<T>` for custom implementations
- Enjoy compile-time safety instead of runtime errors!
