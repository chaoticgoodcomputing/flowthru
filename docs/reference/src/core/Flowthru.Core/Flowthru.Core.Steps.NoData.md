# <a id="Flowthru_Core_Steps_NoData"></a> Class NoData

Namespace: [Flowthru.Core.Steps](Flowthru.Core.Steps.md)  
Assembly: Flowthru.Core.dll  

Marker type representing "no meaningful data" for nodes with side-effects or data generation.
Used as input/output type when a step doesn't consume or produce meaningful data.

```csharp
public sealed class NoData
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[NoData](Flowthru.Core.Steps.NoData.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

<p>
<strong>Design rationale:</strong> NoData provides a type-safe way to represent nodes that:
</p>
<ul><li>Generate data without inputs (synthetic data, seeding)</li><li>Perform side-effects without outputs (validation, logging, diagnostics)</li></ul>
<p>
Inspired by functional programming's "Unit" type but with naming closer to typical .NET usage.
</p>
<p>
<strong>Pipeline registration:</strong> Use <xref href="Flowthru.Core.Steps.NoData.Item" data-throw-if-not-resolved="false"></xref> in either input or output position
when wiring a step that has no meaningful data on that side. Each access yields a unique catalog
entry to avoid DAG conflicts.
</p>
<pre><code class="lang-csharp">pipeline.AddStep&lt;ValidationStep&gt;(
    input: catalog.InputData,
    output: NoData.Item   // side-effect-only step
);

pipeline.AddStep&lt;GenerateDataStep&gt;(
    input: NoData.Item,   // no-input step
    output: catalog.GeneratedData
);</code></pre>
<p>
Use <xref href="Flowthru.Core.Steps.NoData.Value" data-throw-if-not-resolved="false"></xref> when returning <code>NoData</code> from a step's transform, or
<xref href="Flowthru.Core.Steps.NoData.Result" data-throw-if-not-resolved="false"></xref> for the standard <code>Task&lt;IEnumerable&lt;NoData&gt;&gt;</code> wrapper.
</p>

## Fields

### <a id="Flowthru_Core_Steps_NoData_Value"></a> Value

Singleton instance returned from step transformations that produce <code>NoData</code>.

```csharp
public static readonly NoData Value
```

#### Field Value

 [NoData](Flowthru.Core.Steps.NoData.md)

## Properties

### <a id="Flowthru_Core_Steps_NoData_Item"></a> Item

Yields a unique null catalog entry for use in either input or output position when wiring
a step. Each access returns a fresh instance with a unique key so the DAG can distinguish
independent NoData edges.

```csharp
public static IItem<NoData> Item { get; }
```

#### Property Value

 [IItem](Flowthru.Core.Data.IItem\-1.md)<[NoData](Flowthru.Core.Steps.NoData.md)\>

## Methods

### <a id="Flowthru_Core_Steps_NoData_Result"></a> Result\(\)

Returns the standard <code>NoData</code> result for side-effect-only steps. Eliminates the
verbose <code>Task.FromResult(Enumerable.Repeat(NoData.Value, 1))</code> boilerplate.

```csharp
public static Task<IEnumerable<NoData>> Result()
```

#### Returns

 [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<[IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<[NoData](Flowthru.Core.Steps.NoData.md)\>\>

Singleton collection containing <xref href="Flowthru.Core.Steps.NoData.Value" data-throw-if-not-resolved="false"></xref>.

