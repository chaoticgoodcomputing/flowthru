# <a id="Flowthru_Nodes_NoData"></a> Class NoData

Namespace: [Flowthru.Nodes](Flowthru.Nodes.md)  
Assembly: Flowthru.Core.dll  

Marker type representing "no meaningful data" for nodes with side-effects or data generation.
Used as input/output type in NodeBase when a node doesn't consume or produce meaningful data.

```csharp
public sealed class NoData
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[NoData](Flowthru.Nodes.NoData.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

<p>
<strong>Design Rationale:</strong> NoData provides a type-safe way to represent nodes that:
- Generate data without inputs (e.g., synthetic data generation, seeding)
- Perform side-effects without outputs (e.g., validation, logging, diagnostics)
</p>
<p>
This pattern is inspired by functional programming's "Unit" type but uses more intuitive
naming for .NET developers unfamiliar with functional terminology.
</p>
<p>
<strong>Usage Examples:</strong>
</p>
<pre><code class="lang-csharp">// Node with no inputs (data generation)
public class GenerateDataNode : NodeBase&lt;NoData, OutputSchema&gt;
{
    protected override Task&lt;IEnumerable&lt;OutputSchema&gt;&gt; Transform(IEnumerable&lt;NoData&gt; input)
    {
        // Generate data from scratch...
        return Task.FromResult(generatedData);
    }
}

// Node with no outputs (side-effects only)
public class ValidateNode : NodeBase&lt;InputSchema, NoData&gt;
{
    protected override Task&lt;IEnumerable&lt;NoData&gt;&gt; Transform(IEnumerable&lt;InputSchema&gt; input)
    {
        // Perform validation, logging, etc...
        return Task.FromResult(Enumerable.Repeat(NoData.Value, 1));
    }
}</code></pre>
<p>
<strong>Pipeline Registration:</strong> Use NoData type directly - it automatically converts
to a unique NullCatalogDataset instance:
</p>
<pre><code class="lang-csharp">// Simple syntax with automatic unique key generation
pipeline.AddNode&lt;ValidationNode&gt;(
    input: catalog.InputData,
    output: NoData.Output  // or just: NoData.Discard
);

pipeline.AddNode&lt;GenerateDataNode&gt;(
    input: NoData.Input,  // or just: NoData.None
    output: catalog.GeneratedData
);</code></pre>

## Fields

### <a id="Flowthru_Nodes_NoData_Value"></a> Value

Singleton instance of NoData.
Use this value when returning NoData from node transformations.

```csharp
public static readonly NoData Value
```

#### Field Value

 [NoData](Flowthru.Nodes.NoData.md)

## Properties

### <a id="Flowthru_Nodes_NoData_Discard"></a> Discard

Creates a unique null catalog entry for use as a node output (side-effect-only nodes).
Semantic alias for Output - use whichever reads better in context.

```csharp
public static ICatalogEntry<NoData> Discard { get; }
```

#### Property Value

 [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<[NoData](Flowthru.Nodes.NoData.md)\>

### <a id="Flowthru_Nodes_NoData_Input"></a> Input

Creates a unique null catalog entry for use as a node input (no-input nodes).
Each call generates a new instance with a unique key to avoid DAG conflicts.

```csharp
public static ICatalogEntry<NoData> Input { get; }
```

#### Property Value

 [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<[NoData](Flowthru.Nodes.NoData.md)\>

#### Remarks

Alias for readability in pipeline declarations where nodes don't consume external inputs.

### <a id="Flowthru_Nodes_NoData_None"></a> None

Creates a unique null catalog entry for use as a node input (no-input nodes).
Semantic alias for Input - use whichever reads better in context.

```csharp
public static ICatalogEntry<NoData> None { get; }
```

#### Property Value

 [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<[NoData](Flowthru.Nodes.NoData.md)\>

### <a id="Flowthru_Nodes_NoData_Output"></a> Output

Creates a unique null catalog entry for use as a node output (side-effect-only nodes).
Each call generates a new instance with a unique key to avoid DAG conflicts.

```csharp
public static ICatalogEntry<NoData> Output { get; }
```

#### Property Value

 [ICatalogEntry](Flowthru.Data.ICatalogEntry\-1.md)<[NoData](Flowthru.Nodes.NoData.md)\>

#### Remarks

Alias for readability in pipeline declarations where nodes produce no meaningful output.

## Methods

### <a id="Flowthru_Nodes_NoData_Result"></a> Result\(\)

Returns the standard NoData result for side-effect-only nodes.
Use this at the end of Transform() methods that return NoData.

```csharp
public static Task<IEnumerable<NoData>> Result()
```

#### Returns

 [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<[IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<[NoData](Flowthru.Nodes.NoData.md)\>\>

Singleton collection containing NoData.Value

#### Remarks

<p>
This helper eliminates the verbose <code>Task.FromResult(Enumerable.Repeat(NoData.Value, 1))</code>
boilerplate. Simply return <code>NoData.Result()</code>.
</p>
<example>
<pre><code class="lang-csharp">// Instead of:
return Task.FromResult(Enumerable.Repeat(NoData.Value, 1));

// Use:
return NoData.Result();</code></pre>
</example>

