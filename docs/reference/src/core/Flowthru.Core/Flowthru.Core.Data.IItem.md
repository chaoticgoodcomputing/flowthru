# <a id="Flowthru_Core_Data_IItem"></a> Interface IItem

Namespace: [Flowthru.Core.Data](Flowthru.Core.Data.md)  
Assembly: Flowthru.Core.dll  

Non-generic base interface for catalog items — a specialization of <xref href="Flowthru.Core.Graph.INode" data-throw-if-not-resolved="false"></xref>
for data I/O nodes backed by storage adapters.

```csharp
public interface IItem : INode
```

#### Implements

[INode](Flowthru.Core.Graph.INode.md)

## Remarks

<p>
Extends <xref href="Flowthru.Core.Graph.INode" data-throw-if-not-resolved="false"></xref> with data-specific operations: existence checks,
row counting, and two-level inspection (shallow/deep). The engine-level
<xref href="Flowthru.Core.Graph.INode.ProduceUntyped" data-throw-if-not-resolved="false"></xref> and <xref href="Flowthru.Core.Graph.INode.ConsumeUntyped(System.Object)" data-throw-if-not-resolved="false"></xref> are
bridged to <xref href="Flowthru.Core.Data.IItem.LoadUntyped" data-throw-if-not-resolved="false"></xref> and <xref href="Flowthru.Core.Data.IItem.SaveUntyped(System.Object)" data-throw-if-not-resolved="false"></xref> via default
interface implementations.
</p>

## Properties

### <a id="Flowthru_Core_Data_IItem_OwningCatalogLabel"></a> OwningCatalogLabel

The label of the <xref href="Flowthru.Core.Data.CatalogAbstract" data-throw-if-not-resolved="false"></xref>-derived class that created
this item. Set automatically by <code>CreateItem</code>; null for items created outside
a catalog or by custom <xref href="Flowthru.Core.Data.IItem" data-throw-if-not-resolved="false"></xref> implementations.

```csharp
string? OwningCatalogLabel { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

#### Remarks

Used by the metadata layer to produce fully-qualified item identifiers in the form
<code>CatalogLabel.ItemLabel</code>. First-write-wins: cross-catalog shared items retain
the label of the catalog that originally created them.

### <a id="Flowthru_Core_Data_IItem_PreferredInspectionLevel"></a> PreferredInspectionLevel

Gets the preferred inspection level for this catalog item.

```csharp
InspectionLevel? PreferredInspectionLevel { get; }
```

#### Property Value

 [InspectionLevel](Flowthru.Core.Data.Validation.InspectionLevel.md)?

## Methods

### <a id="Flowthru_Core_Data_IItem_Exists"></a> Exists\(\)

Checks if data exists at this catalog item location.
Returns an effect that can fail.

```csharp
FlowIO<bool> Exists()
```

#### Returns

 [FlowIO](Flowthru.Core.Effects.FlowIO\-1.md)<[bool](https://learn.microsoft.com/dotnet/api/system.boolean)\>

### <a id="Flowthru_Core_Data_IItem_GetCountAsync"></a> GetCountAsync\(\)

Gets the count of items in this catalog item.
For collections (IEnumerable&lt;T&gt;), returns the enumerable count.
For singletons, returns 1 if exists, 0 otherwise.

```csharp
FlowIO<int> GetCountAsync()
```

#### Returns

 [FlowIO](Flowthru.Core.Effects.FlowIO\-1.md)<[int](https://learn.microsoft.com/dotnet/api/system.int32)\>

### <a id="Flowthru_Core_Data_IItem_InspectDeep"></a> InspectDeep\(\)

Performs deep validation of this catalog item.

```csharp
FlowIO<ValidationResult> InspectDeep()
```

#### Returns

 [FlowIO](Flowthru.Core.Effects.FlowIO\-1.md)<[ValidationResult](Flowthru.Core.Data.Validation.ValidationResult.md)\>

Effect producing validation result

### <a id="Flowthru_Core_Data_IItem_InspectShallow_System_Int32_"></a> InspectShallow\(int\)

Performs shallow validation of this catalog item.

```csharp
FlowIO<ValidationResult> InspectShallow(int sampleSize = 100)
```

#### Parameters

`sampleSize` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Number of rows/records to sample for validation

#### Returns

 [FlowIO](Flowthru.Core.Effects.FlowIO\-1.md)<[ValidationResult](Flowthru.Core.Data.Validation.ValidationResult.md)\>

Effect producing validation result

### <a id="Flowthru_Core_Data_IItem_InspectTarget"></a> InspectTarget\(\)

Validates that this catalog item is accessible as a write destination.

```csharp
FlowIO<ValidationResult> InspectTarget()
```

#### Returns

 [FlowIO](Flowthru.Core.Effects.FlowIO\-1.md)<[ValidationResult](Flowthru.Core.Data.Validation.ValidationResult.md)\>

Effect producing validation result

### <a id="Flowthru_Core_Data_IItem_LoadUntyped"></a> LoadUntyped\(\)

Loads data from the catalog item as an untyped object.
Returns an effect that can fail.
The returned type matches the DataType property.

```csharp
FlowIO<object> LoadUntyped()
```

#### Returns

 [FlowIO](Flowthru.Core.Effects.FlowIO\-1.md)<[object](https://learn.microsoft.com/dotnet/api/system.object)\>

### <a id="Flowthru_Core_Data_IItem_SaveUntyped_System_Object_"></a> SaveUntyped\(object\)

Saves untyped data to the catalog item.
Returns an effect that can fail.
The data type must be compatible with the DataType property.

```csharp
FlowIO<FlowUnit> SaveUntyped(object data)
```

#### Parameters

`data` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [FlowIO](Flowthru.Core.Effects.FlowIO\-1.md)<[FlowUnit](Flowthru.Core.Effects.FlowUnit.md)\>

