# <a id="Flowthru_Data_ICatalogEntry_1"></a> Interface ICatalogEntry<T\>

Namespace: [Flowthru.Data](Flowthru.Data.md)  
Assembly: Flowthru.Core.dll  

Unified catalog entry with cardinality encoded in the type parameter.

```csharp
public interface ICatalogEntry<T> : ICatalogEntry
```

#### Type Parameters

`T` 

The data type stored in this catalog entry.
Cardinality is determined by T itself:
- For singletons: Use T directly (e.g., LinearRegressionModel, ModelMetrics)
- For collections: Use IEnumerable&lt;T&gt; (e.g., IEnumerable&lt;FeatureRow&gt;)

#### Implements

[ICatalogEntry](Flowthru.Data.ICatalogEntry.md)

## Remarks

<p>
<strong>Unified Design:</strong> This single interface replaces the previous dual-interface
system (ICatalogObject/ICatalogDataset). Cardinality is now purely a type-level concern.
</p>
<p>
<strong>Type Alignment:</strong> Node TInput/TOutput types should directly match catalog entry
T types, eliminating the need for wrapping/unwrapping ceremony.
</p>
<p>
<strong>Effect Types:</strong> All operations return FlowIO&lt;T&gt; - an effect that represents
an async computation that can fail. This provides:
- Explicit error handling
- Cancellation support
- Functional composition
</p>

## Methods

### <a id="Flowthru_Data_ICatalogEntry_1_Load"></a> Load\(\)

Load data as an effect (can fail, is async, can be cancelled).
Returns T directly, which may itself be an IEnumerable or Seq.

```csharp
FlowIO<T> Load()
```

#### Returns

 [FlowIO](Flowthru.Effects.FlowIO\-1.md)<T\>

### <a id="Flowthru_Data_ICatalogEntry_1_Save__0_"></a> Save\(T\)

Save data as an effect.
Accepts T directly, which may itself be an IEnumerable or Seq.

```csharp
FlowIO<FlowUnit> Save(T data)
```

#### Parameters

`data` T

#### Returns

 [FlowIO](Flowthru.Effects.FlowIO\-1.md)<[FlowUnit](Flowthru.Effects.FlowUnit.md)\>

