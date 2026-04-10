# <a id="Flowthru_Core_Data_Item_1"></a> Class Item<T\>

Namespace: [Flowthru.Core.Data](Flowthru.Core.Data.md)  
Assembly: Flowthru.Core.dll  

Standard catalog item implementation that delegates to a storage adapter.

```csharp
public sealed class Item<T> : IItem<T>, IItem, INode<T>, INode
```

#### Type Parameters

`T` 

The data type (container with rows)

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Item<T\>](Flowthru.Core.Data.Item\-1.md)

#### Implements

[IItem<T\>](Flowthru.Core.Data.IItem\-1.md), 
[IItem](Flowthru.Core.Data.IItem.md), 
[INode<T\>](Flowthru.Core.Graph.INode\-1.md), 
[INode](Flowthru.Core.Graph.INode.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

<p>
<strong>Delegation Pattern:</strong>
</p>
<p>
This class is a thin wrapper that delegates all operations to an <xref href="Flowthru.Core.Data.Storage.IStorageAdapter%601" data-throw-if-not-resolved="false"></xref>.
The storage adapter handles the actual I/O logic, while this class provides:
- ICatalogItem interface implementation
- Identity for DAG dependency resolution (via Key)
- Type erasure for Flow heterogeneous collections
</p>
<p>
<strong>Construction:</strong>
</p>
<p>
Typically created via static factory methods in <xref href="Flowthru.Core.Data.ItemFactory" data-throw-if-not-resolved="false"></xref>:
</p>
<pre><code class="lang-csharp">var item = CatalogItemFactory.Csv&lt;CompanySchema&gt;("companies", "data.csv");
// Returns: ICatalogItem&lt;IEnumerable&lt;CompanySchema&gt;&gt;</code></pre>
<p>
<strong>Composition vs Inheritance:</strong>
</p>
<p>
Previous design: Inheritance hierarchy (CsvCatalogItem, JsonCatalogItem, etc.)
New design: Single class + composed storage adapter
</p>
<p>
Benefits:
- No class explosion for format × container combinations
- Custom storage via IStorageAdapter implementation
- Clear separation of concerns
</p>
<p>
<strong>Capability Forwarding:</strong>
</p>
<p>
The underlying storage adapter provides inspection methods, which this catalog
item automatically forwards. All storage adapters are required to implement inspection.
</p>

## Constructors

### <a id="Flowthru_Core_Data_Item_1__ctor_System_String_Flowthru_Core_Data_Storage_IStorageAdapter__0__"></a> Item\(string, IStorageAdapter<T\>\)

Creates a new catalog item with the specified key and storage adapter.

```csharp
public Item(string label, IStorageAdapter<T> storage)
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique identifier for this catalog item

`storage` [IStorageAdapter](Flowthru.Core.Data.Storage.IStorageAdapter\-1.md)<T\>

Storage adapter that handles I/O operations

## Properties

### <a id="Flowthru_Core_Data_Item_1_DataType"></a> DataType

The runtime type of the value this node produces.
For singletons: typeof(T). For collections: typeof(IEnumerable&lt;T&gt;).

```csharp
public Type DataType { get; }
```

#### Property Value

 [Type](https://learn.microsoft.com/dotnet/api/system.type)

### <a id="Flowthru_Core_Data_Item_1_Label"></a> Label

Unique label identifying this node within the DAG.
Used for dependency resolution and wiring.

```csharp
public string Label { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="Flowthru_Core_Data_Item_1_OwningCatalogLabel"></a> OwningCatalogLabel

The label of the <xref href="Flowthru.Core.Data.CatalogAbstract" data-throw-if-not-resolved="false"></xref>-derived class that created
this item. Set automatically by <code>CreateItem</code>; null for items created outside
a catalog or by custom <xref href="Flowthru.Core.Data.IItem" data-throw-if-not-resolved="false"></xref> implementations.

```csharp
public string? OwningCatalogLabel { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

#### Remarks

Used by the metadata layer to produce fully-qualified item identifiers in the form
<code>CatalogLabel.ItemLabel</code>. First-write-wins: cross-catalog shared items retain
the label of the catalog that originally created them.

### <a id="Flowthru_Core_Data_Item_1_PreferredInspectionLevel"></a> PreferredInspectionLevel

Gets the preferred inspection level for this catalog item.

```csharp
public InspectionLevel? PreferredInspectionLevel { get; }
```

#### Property Value

 [InspectionLevel](Flowthru.Core.Data.Validation.InspectionLevel.md)?

### <a id="Flowthru_Core_Data_Item_1_Traits"></a> Traits

Gets the effective storage traits for this catalog item.

```csharp
public StorageTraits Traits { get; }
```

#### Property Value

 [StorageTraits](Flowthru.Core.Data.Capabilities.StorageTraits.md)

#### Remarks

<p>
This returns the adapter's traits merged with any user-specified constraints
applied via <xref href="Flowthru.Core.Data.Item%601.Constrain(System.Func%7bFlowthru.Core.Data.Capabilities.StorageTraits%2cFlowthru.Core.Data.Capabilities.StorageTraits%7d)" data-throw-if-not-resolved="false"></xref>.
</p>
<p>
Used by Flow validation to enforce read-only constraints, network requirements, etc.
</p>

## Methods

### <a id="Flowthru_Core_Data_Item_1_Constrain_System_Func_Flowthru_Core_Data_Capabilities_StorageTraits_Flowthru_Core_Data_Capabilities_StorageTraits__"></a> Constrain\(Func<StorageTraits, StorageTraits\>\)

Applies user-specified constraints to the storage traits.

```csharp
public Item<T> Constrain(Func<StorageTraits, StorageTraits> constraintFn)
```

#### Parameters

`constraintFn` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[StorageTraits](Flowthru.Core.Data.Capabilities.StorageTraits.md), [StorageTraits](Flowthru.Core.Data.Capabilities.StorageTraits.md)\>

Function that modifies traits to apply constraints

#### Returns

 [Item](Flowthru.Core.Data.Item\-1.md)<T\>

This catalog item for method chaining

#### Remarks

<p>
<strong>One-Way Ratchet:</strong> Constraints can only tighten, never loosen.
You cannot grant capabilities the adapter doesn't support.
</p>
<p>
<strong>Valid Constraints:</strong>
</p>
<ul><li>Making a writable adapter read-only: <code>t =&gt; t with { CanWrite = false }</code></li><li>Marking a local file as non-inspectable: <code>t =&gt; t with { CanInspect = false }</code></li></ul>
<p>
<strong>Invalid Constraints (will throw):</strong>
</p>
<ul><li>Making a read-only adapter writable: <code>t =&gt; t with { CanWrite = true }</code></li><li>Removing network requirement: <code>t =&gt; t with { RequiresNetwork = false }</code></li></ul>
<p>
<strong>Example:</strong>
</p>
<pre><code class="lang-csharp">public ICatalogItem&lt;IEnumerable&lt;Company&gt;&gt; ReferenceData =&gt;
    CreateItem(() =&gt; CatalogItemFactory.Enumerable.Csv&lt;Company&gt;(
        "ref_data", $"{_basePath}/reference.csv")
        .Constrain(t =&gt; t with { CanWrite = false }));</code></pre>

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

Thrown when attempting to loosen a constraint (grant a capability the adapter doesn't support)

### <a id="Flowthru_Core_Data_Item_1_Exists"></a> Exists\(\)

Checks if data exists at this catalog item location.
Returns an effect that can fail.

```csharp
public FlowIO<bool> Exists()
```

#### Returns

 [FlowIO](Flowthru.Core.Effects.FlowIO\-1.md)<[bool](https://learn.microsoft.com/dotnet/api/system.boolean)\>

### <a id="Flowthru_Core_Data_Item_1_GetCountAsync"></a> GetCountAsync\(\)

Gets the count of items in this catalog item.
For collections (IEnumerable&lt;T&gt;), returns the enumerable count.
For singletons, returns 1 if exists, 0 otherwise.

```csharp
public FlowIO<int> GetCountAsync()
```

#### Returns

 [FlowIO](Flowthru.Core.Effects.FlowIO\-1.md)<[int](https://learn.microsoft.com/dotnet/api/system.int32)\>

### <a id="Flowthru_Core_Data_Item_1_InspectDeep"></a> InspectDeep\(\)

Performs deep validation of this catalog item.

```csharp
public FlowIO<ValidationResult> InspectDeep()
```

#### Returns

 [FlowIO](Flowthru.Core.Effects.FlowIO\-1.md)<[ValidationResult](Flowthru.Core.Data.Validation.ValidationResult.md)\>

Effect producing validation result

#### Remarks

Forwards the call directly to the underlying storage adapter.
All storage adapters must implement inspection.

### <a id="Flowthru_Core_Data_Item_1_InspectShallow_System_Int32_"></a> InspectShallow\(int\)

Performs shallow validation of this catalog item.

```csharp
public FlowIO<ValidationResult> InspectShallow(int sampleSize = 100)
```

#### Parameters

`sampleSize` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Number of rows/records to sample for validation

#### Returns

 [FlowIO](Flowthru.Core.Effects.FlowIO\-1.md)<[ValidationResult](Flowthru.Core.Data.Validation.ValidationResult.md)\>

Effect producing validation result

#### Remarks

Forwards the call directly to the underlying storage adapter.
All storage adapters must implement inspection.

### <a id="Flowthru_Core_Data_Item_1_Load"></a> Load\(\)

Load data as an effect (can fail, is async, can be cancelled).
Returns T directly, which may itself be an IEnumerable or Seq.

```csharp
public FlowIO<T> Load()
```

#### Returns

 [FlowIO](Flowthru.Core.Effects.FlowIO\-1.md)<T\>

### <a id="Flowthru_Core_Data_Item_1_LoadUntyped"></a> LoadUntyped\(\)

Loads data from the catalog item as an untyped object.
Returns an effect that can fail.
The returned type matches the DataType property.

```csharp
public FlowIO<object> LoadUntyped()
```

#### Returns

 [FlowIO](Flowthru.Core.Effects.FlowIO\-1.md)<[object](https://learn.microsoft.com/dotnet/api/system.object)\>

### <a id="Flowthru_Core_Data_Item_1_Save__0_"></a> Save\(T\)

Save data as an effect.
Accepts T directly, which may itself be an IEnumerable or Seq.

```csharp
public FlowIO<FlowUnit> Save(T data)
```

#### Parameters

`data` T

#### Returns

 [FlowIO](Flowthru.Core.Effects.FlowIO\-1.md)<[FlowUnit](Flowthru.Core.Effects.FlowUnit.md)\>

### <a id="Flowthru_Core_Data_Item_1_SaveUntyped_System_Object_"></a> SaveUntyped\(object\)

Saves untyped data to the catalog item.
Returns an effect that can fail.
The data type must be compatible with the DataType property.

```csharp
public FlowIO<FlowUnit> SaveUntyped(object data)
```

#### Parameters

`data` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [FlowIO](Flowthru.Core.Effects.FlowIO\-1.md)<[FlowUnit](Flowthru.Core.Effects.FlowUnit.md)\>

### <a id="Flowthru_Core_Data_Item_1_WithInspectionLevel_Flowthru_Core_Data_Validation_InspectionLevel_"></a> WithInspectionLevel\(InspectionLevel\)

Sets the preferred inspection level for this catalog item.

```csharp
public Item<T> WithInspectionLevel(InspectionLevel level)
```

#### Parameters

`level` [InspectionLevel](Flowthru.Core.Data.Validation.InspectionLevel.md)

The inspection level to use

#### Returns

 [Item](Flowthru.Core.Data.Item\-1.md)<T\>

This catalog item for method chaining

#### Remarks

<p>
Used to configure how this item should be validated before Flow execution.
</p>
<p>
Example:
</p>
<pre><code class="lang-csharp">var item = CatalogItemFactory.Csv&lt;Company&gt;("companies", "data.csv")
    .WithInspectionLevel(InspectionLevel.Deep);</code></pre>

