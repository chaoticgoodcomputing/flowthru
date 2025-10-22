# How to Structure Multi-Input/Output Nodes

When nodes need multiple inputs or produce multiple outputs, use **input/output schemas** with `CatalogMap<T>`.

## Multi-Input Pattern

```csharp
// 1. Define input schema
public record JoinInputs
{
    [Required]
    public required IEnumerable<Company> Companies { get; init; }
    
    [Required]
    public required IEnumerable<Review> Reviews { get; init; }
}

// 2. Create node using schema
public class JoinNode : NodeBase<JoinInputs, EnrichedCompany>
{
    protected override Task<IEnumerable<EnrichedCompany>> Transform(
        IEnumerable<JoinInputs> inputs)
    {
        var input = inputs.Single();
        
        // Use input.Companies and input.Reviews
        // ...join logic...
        
        return Task.FromResult(enriched);
    }
}

// 3. Map catalog entries to properties
var inputMap = new CatalogMap<JoinInputs>();
inputMap.Map(i => i.Companies, catalog.Companies);
inputMap.Map(i => i.Reviews, catalog.Reviews);

// 4. Wire into pipeline
builder.AddNode<JoinNode>(
    inputMap: inputMap,
    output: catalog.EnrichedCompanies,
    name: "join_data");
```

**Key Points:**
- Input schema groups related inputs into one type
- `[Required]` attribute documents mandatory properties
- Expression-based mapping provides compile-time validation
- Schemas are always singleton (`.Single()` pattern)

## Multi-Output Pattern

```csharp
// 1. Define output schema
public record SplitOutputs
{
    [Required]
    public required IEnumerable<FeatureRow> TrainData { get; init; }
    
    [Required]
    public required IEnumerable<FeatureRow> TestData { get; init; }
}

// 2. Create node producing schema
public class SplitNode : NodeBase<FeatureRow, SplitOutputs>
{
    protected override Task<IEnumerable<SplitOutputs>> Transform(
        IEnumerable<FeatureRow> input)
    {
        // Split logic...
        
        return Task.FromResult(new[]
        {
            new SplitOutputs
            {
                TrainData = trainData,
                TestData = testData
            }
        }.AsEnumerable());
    }
}

// 3. Map properties to catalog entries
var outputMap = new CatalogMap<SplitOutputs>();
outputMap.Map(o => o.TrainData, catalog.TrainData);
outputMap.Map(o => o.TestData, catalog.TestData);

// 4. Wire into pipeline
builder.AddNode<SplitNode>(
    input: catalog.Features,
    outputMap: outputMap,
    name: "split_data");
```

**Key Points:**
- Output schema symmetrical to input schema pattern
- Each property maps to one catalog entry
- Multiple outputs enable downstream nodes to reference data independently
- Compile-time validation ensures all outputs are wired
