# How to Leverage Compile-Time Safety

Flowthru's primary advantage over Kedro is **compile-time type safety**. Here's how to maximize it.

## Use Catalog Properties, Never String Keys

**❌ Avoid (runtime errors):**
```csharp
var data = catalog.Get<CompanyData>("compnies"); // Typo! Runtime KeyNotFoundException
```

**✅ Prefer (compile-time safety):**
```csharp
public class MyCatalog : DataCatalogBase
{
    public CsvCatalogDataset<CompanyData> Companies { get; }
    
    public MyCatalog()
    {
        Companies = CreateCsvDataset<CompanyData>("companies", "data/companies.csv");
    }
}

// Usage: typos caught by compiler
var data = catalog.Compnies; // ❌ Compile error: 'MyCatalog' does not contain 'Compnies'
var data = catalog.Companies; // ✅ Correct
```

**Benefits:**
- IntelliSense autocomplete shows all datasets
- Rename refactoring updates all usages
- Unused datasets show as gray (dead code detection)
- Cannot reference non-existent datasets

## Use Expression-Based Mapping, Never String Property Names

**❌ Avoid (runtime errors):**
```csharp
inputMap.Map("Companeis", catalog.Companies); // Typo! Runtime exception
```

**✅ Prefer (compile-time safety):**
```csharp
inputMap.Map(i => i.Companies, catalog.Companies); // ✅ Compiler validates 'Companies' exists
```

**Benefits:**
- Compiler validates property exists on input schema
- Refactoring updates property references automatically
- Type mismatch between property and catalog entry caught at compile-time

## Use Strongly-Typed Parameters, Never Dictionaries

**❌ Avoid (runtime errors):**
```csharp
// Kedro-style: string keys, any values
var parameters = new Dictionary<string, object>
{
    { "test_size", 0.2 },
    { "random_stat", 42 } // Typo! Wrong key, discovered at runtime
};
```

**✅ Prefer (compile-time safety):**
```csharp
public record ModelOptions
{
    public double TestSize { get; init; } = 0.2;
    public int RandomState { get; init; } = 42;
}

var options = new ModelOptions
{
    TestSize = 0.2,
    RandomStat = 42 // ❌ Compile error: 'ModelOptions' does not contain 'RandomStat'
};
```

**Benefits:**
- IntelliSense shows all available parameters
- Default values defined once in record
- Type safety: cannot assign string to int parameter
- Refactoring renames propagate automatically

## Validate Schemas at Compile-Time with Generic Constraints

**❌ Avoid (runtime errors):**
```csharp
// Node can accept any type, errors discovered during execution
public class ProcessNode : NodeBase<object, object> { }
```

**✅ Prefer (compile-time safety):**
```csharp
// Node signature enforces exact types
public class ProcessNode : NodeBase<CompanyRawData, CompanyProcessed> { }

// Pipeline builder verifies compatibility
builder.AddNode<ProcessNode>(
    input: catalog.Companies,     // ✅ Must be ICatalogDataset<CompanyRawData>
    output: catalog.Processed,    // ✅ Must be ICatalogDataset<CompanyProcessed>
    name: "process");
```

**Benefits:**
- Compiler verifies input/output types match node expectations
- Impossible to wire wrong dataset to wrong node
- Type changes in node signature immediately flag incompatible usages
