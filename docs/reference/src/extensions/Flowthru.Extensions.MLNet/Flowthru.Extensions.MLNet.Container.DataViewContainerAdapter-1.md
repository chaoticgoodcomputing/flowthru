# <a id="Flowthru_Extensions_MLNet_Container_DataViewContainerAdapter_1"></a> Class DataViewContainerAdapter<T\>

Namespace: [Flowthru.Extensions.MLNet.Container](Flowthru.Extensions.MLNet.Container.md)  
Assembly: Flowthru.Extensions.MLNet.dll  

Container adapter for ML.NET IDataView - columnar data representation.

```csharp
public sealed class DataViewContainerAdapter<T> : IContainerAdapter<IDataView, T> where T : class, new()
```

#### Type Parameters

`T` 

The row schema type

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[DataViewContainerAdapter<T\>](Flowthru.Extensions.MLNet.Container.DataViewContainerAdapter\-1.md)

#### Implements

IContainerAdapter<IDataView, T\>

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Examples

<pre><code class="lang-csharp">public record FeatureRow(
    float Feature1,
    float Feature2,
    int Label
) : IFlatSchema, ITextSerializable;

var mlContext = new MLContext();
var adapter = new DataViewContainerAdapter&lt;FeatureRow&gt;(mlContext);

// From rows to IDataView
var dataView = await adapter.FromRows(rowStream);

// Use with ML.NET
var pipeline = mlContext.Transforms
    .NormalizeMinMax("Feature1")
    .Append(mlContext.Transforms.NormalizeMinMax("Feature2"));

var model = pipeline.Fit(dataView);
var transformedData = model.Transform(dataView);

// Back to rows
var transformedRows = adapter.ToRows(transformedData);</code></pre>

## Remarks

<p>
<strong>NEW CAPABILITY:</strong> This adapter enables native ML.NET integration with Flowthru!
</p>
<p>
<strong>Characteristics:</strong>
</p>
<ul><li><strong>Columnar storage:</strong> Optimized for ML.NET operations</li><li><strong>Lazy evaluation:</strong> Data loaded on-demand during iteration</li><li><strong>Type safety:</strong> Strongly-typed row schema</li><li><strong>ML.NET native:</strong> Direct integration with ML.NET pipelines</li></ul>
<p>
<strong>Use Cases:</strong>
</p>
<ul><li>Machine learning pipelines using ML.NET</li><li>Data transformations (normalization, encoding, etc.)</li><li>Feature engineering workflows</li><li>Model training and evaluation</li></ul>
<p>
<strong>Integration with ML.Next:</strong>
</p>
<p>
This adapter bridges Flowthru catalogs with ML.Next's type-safe wrappers,
enabling end-to-end compile-time safety for ML pipelines.
</p>

## Constructors

### <a id="Flowthru_Extensions_MLNet_Container_DataViewContainerAdapter_1__ctor_Microsoft_ML_MLContext_"></a> DataViewContainerAdapter\(MLContext\)

Creates a new IDataView container adapter.

```csharp
public DataViewContainerAdapter(MLContext mlContext)
```

#### Parameters

`mlContext` [MLContext](https://learn.microsoft.com/dotnet/api/microsoft.ml.mlcontext)

The ML.NET context for data operations

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

Thrown if mlContext is null

## Properties

### <a id="Flowthru_Extensions_MLNet_Container_DataViewContainerAdapter_1_MLContext"></a> MLContext

Gets the ML.NET context used by this adapter.

```csharp
public MLContext MLContext { get; }
```

#### Property Value

 [MLContext](https://learn.microsoft.com/dotnet/api/microsoft.ml.mlcontext)

## Methods

### <a id="Flowthru_Extensions_MLNet_Container_DataViewContainerAdapter_1_FromRows_System_Collections_Generic_IAsyncEnumerable__0__"></a> FromRows\(IAsyncEnumerable<T\>\)

Materializes an async stream of rows into an in-memory container.

```csharp
public Task<IDataView> FromRows(IAsyncEnumerable<T> rows)
```

#### Parameters

`rows` [IAsyncEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.iasyncenumerable\-1)<T\>

Async stream of rows from format deserialization

#### Returns

 [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<[IDataView](https://learn.microsoft.com/dotnet/api/microsoft.ml.idataview)\>

Task producing the populated container

#### Remarks

<p>
<strong>Materialization Strategy:</strong>
</p>
<p>
Different containers have different materialization approaches:
</p>
<ul><li><strong>IEnumerable:</strong> Eager - load all rows into List</li><li><strong>Seq:</strong> Lazy - wrap async enumerable</li><li><strong>IDataView:</strong> Columnar - convert to columnar format</li></ul>
<p>
<strong>Memory Considerations:</strong>
</p>
<p>
Be aware that eager containers (IEnumerable) will load all data into memory.
For large datasets, prefer streaming containers (Seq, IDataView with lazy loading).
</p>

### <a id="Flowthru_Extensions_MLNet_Container_DataViewContainerAdapter_1_ToRows_Microsoft_ML_IDataView_"></a> ToRows\(IDataView\)

Converts an in-memory container back to an async stream of rows.

```csharp
public IAsyncEnumerable<T> ToRows(IDataView container)
```

#### Parameters

`container` [IDataView](https://learn.microsoft.com/dotnet/api/microsoft.ml.idataview)

The container to stream from

#### Returns

 [IAsyncEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.iasyncenumerable\-1)<T\>

Async enumerable of rows for format serialization

#### Remarks

<p>
<strong>Streaming Strategy:</strong>
</p>
<p>
Rows should be yielded lazily if the container supports it:
</p>
<ul><li><strong>IEnumerable:</strong> Enumerate and yield</li><li><strong>Seq:</strong> Already lazy - expose as async enumerable</li><li><strong>IDataView:</strong> Enumerate rows from columnar format</li></ul>

