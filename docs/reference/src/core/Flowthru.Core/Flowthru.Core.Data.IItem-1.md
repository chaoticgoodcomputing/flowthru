# <a id="Flowthru_Core_Data_IItem_1"></a> Interface IItem<T\>

Namespace: [Flowthru.Core.Data](Flowthru.Core.Data.md)  
Assembly: Flowthru.Core.dll  

Typed catalog item — a specialization of <xref href="Flowthru.Core.Graph.INode%601" data-throw-if-not-resolved="false"></xref> for data I/O.

```csharp
public interface IItem<T> : IItem, INode<T>, INode
```

#### Type Parameters

`T` 

The data type stored in this catalog item.
Cardinality is determined by T itself:
- For singletons: Use T directly (e.g., LinearRegressionModel, ModelMetrics)
- For collections: Use IEnumerable&lt;T&gt; (e.g., IEnumerable&lt;FeatureRow&gt;)

#### Implements

[IItem](Flowthru.Core.Data.IItem.md), 
[INode<T\>](Flowthru.Core.Graph.INode\-1.md), 
[INode](Flowthru.Core.Graph.INode.md)

## Remarks

<p>
<xref href="Flowthru.Core.Data.IItem%601.Load" data-throw-if-not-resolved="false"></xref> and <xref href="Flowthru.Core.Data.IItem%601.Save(%600)" data-throw-if-not-resolved="false"></xref> are the data-specific aliases for
<xref href="Flowthru.Core.Graph.INode%601.Produce" data-throw-if-not-resolved="false"></xref> and <xref href="Flowthru.Core.Graph.INode%601.Consume(%600)" data-throw-if-not-resolved="false"></xref>.
Default interface implementations bridge the two: the engine calls
<code>Produce()</code>/<code>Consume()</code>, which delegate to <code>Load()</code>/<code>Save()</code>.
</p>

## Methods

### <a id="Flowthru_Core_Data_IItem_1_Load"></a> Load\(\)

Load data as an effect (can fail, is async, can be cancelled).
Returns T directly, which may itself be an IEnumerable or Seq.

```csharp
FlowIO<T> Load()
```

#### Returns

 [FlowIO](Flowthru.Core.Effects.FlowIO\-1.md)<T\>

### <a id="Flowthru_Core_Data_IItem_1_Save__0_"></a> Save\(T\)

Save data as an effect.
Accepts T directly, which may itself be an IEnumerable or Seq.

```csharp
FlowIO<FlowUnit> Save(T data)
```

#### Parameters

`data` T

#### Returns

 [FlowIO](Flowthru.Core.Effects.FlowIO\-1.md)<[FlowUnit](Flowthru.Core.Effects.FlowUnit.md)\>

