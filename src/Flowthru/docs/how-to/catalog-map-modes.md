# How to Choose Between Pass-Through and Mapped Modes

`CatalogMap<T>` has two modes. Choose based on input/output complexity.

## Pass-Through Mode (Single Input/Output)

For simple nodes with one input and one output:

```csharp
public class SimpleTransformNode : NodeBase<InputData, OutputData>
{
    protected override Task<IEnumerable<OutputData>> Transform(
        IEnumerable<InputData> input)
    {
        // Transform input → output
        return Task.FromResult(transformed);
    }
}

// Pipeline: use catalog properties directly
builder.AddNode<SimpleTransformNode>(
    input: catalog.InputData,   // Direct property reference
    output: catalog.OutputData, // Direct property reference
    name: "transform");
```

**When to Use:**
- One input dataset → one output dataset
- No parameters needed
- Simplest and most common case

## Mapped Mode (Multi-Input/Output or Parameters)

For complex nodes with multiple inputs, outputs, or parameters:

```csharp
// Multi-input
var inputMap = new CatalogMap<ComplexInputs>();
inputMap.Map(i => i.Dataset1, catalog.Dataset1);
inputMap.Map(i => i.Dataset2, catalog.Dataset2);
inputMap.MapParameter(i => i.Options, options);

// Multi-output
var outputMap = new CatalogMap<ComplexOutputs>();
outputMap.Map(o => o.Result1, catalog.Result1);
outputMap.Map(o => o.Result2, catalog.Result2);

builder.AddNode<ComplexNode>(
    inputMap: inputMap,
    outputMap: outputMap,
    name: "complex_transform");
```

**When to Use:**
- Multiple input datasets need to be joined/combined
- Multiple output datasets need to be split/separated
- Node needs configuration parameters
- Input/output structure is complex
