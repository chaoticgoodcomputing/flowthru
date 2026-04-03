# <a id="Flowthru_Data_IItem"></a> Interface IItem

Namespace: [Flowthru.Data](Flowthru.Data.md)  
Assembly: Flowthru.Core.dll  

Non-generic base interface for catalog entries.
Provides untyped operations for internal use by the pipeline executor and mapping layer.

```csharp
public interface IItem
```

## Remarks

This interface enables the pipeline to work with catalog entries
without knowing their specific type parameter at compile-time.

## Properties

### <a id="Flowthru_Data_IItem_DataType"></a> DataType

The runtime type of data stored in this catalog entry.
For singletons: Returns typeof(T).
For collections: Returns typeof(IEnumerable&lt;T&gt;).

```csharp
Type DataType { get; }
```

#### Property Value

 [Type](https://learn.microsoft.com/dotnet/api/system.type)

### <a id="Flowthru_Data_IItem_Label"></a> Label

Unique label identifying this catalog entry within the data catalog.

```csharp
string Label { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="Flowthru_Data_IItem_OwningCatalogLabel"></a> OwningCatalogLabel

The label of the <xref href="Flowthru.Data.DataCatalogBase" data-throw-if-not-resolved="false"></xref>-derived class that created
this entry. Set automatically by <code>GetOrCreateEntry</code>; null for entries created outside
a catalog or by custom <xref href="Flowthru.Data.IItem" data-throw-if-not-resolved="false"></xref> implementations.

```csharp
string? OwningCatalogLabel { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

#### Remarks

Used by the metadata layer to produce fully-qualified entry identifiers in the form
<code>CatalogLabel.EntryLabel</code>. First-write-wins: cross-catalog shared entries retain
the label of the catalog that originally created them.

### <a id="Flowthru_Data_IItem_PreferredInspectionLevel"></a> PreferredInspectionLevel

Gets the preferred inspection level for this catalog entry.

```csharp
InspectionLevel? PreferredInspectionLevel { get; }
```

#### Property Value

 [InspectionLevel](Flowthru.Data.Validation.InspectionLevel.md)?

## Methods

### <a id="Flowthru_Data_IItem_Exists"></a> Exists\(\)

Checks if data exists at this catalog entry location.
Returns an effect that can fail.

```csharp
FlowIO<bool> Exists()
```

#### Returns

 [FlowIO](Flowthru.Effects.FlowIO\-1.md)<[bool](https://learn.microsoft.com/dotnet/api/system.boolean)\>

### <a id="Flowthru_Data_IItem_GetCountAsync"></a> GetCountAsync\(\)

Gets the count of items in this catalog entry.
For collections (IEnumerable&lt;T&gt;), returns the enumerable count.
For singletons, returns 1 if exists, 0 otherwise.

```csharp
FlowIO<int> GetCountAsync()
```

#### Returns

 [FlowIO](Flowthru.Effects.FlowIO\-1.md)<[int](https://learn.microsoft.com/dotnet/api/system.int32)\>

### <a id="Flowthru_Data_IItem_InspectDeep"></a> InspectDeep\(\)

Performs deep validation of this catalog entry.

```csharp
FlowIO<ValidationResult> InspectDeep()
```

#### Returns

 [FlowIO](Flowthru.Effects.FlowIO\-1.md)<[ValidationResult](Flowthru.Data.Validation.ValidationResult.md)\>

Effect producing validation result

### <a id="Flowthru_Data_IItem_InspectShallow_System_Int32_"></a> InspectShallow\(int\)

Performs shallow validation of this catalog entry.

```csharp
FlowIO<ValidationResult> InspectShallow(int sampleSize = 100)
```

#### Parameters

`sampleSize` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Number of rows/records to sample for validation

#### Returns

 [FlowIO](Flowthru.Effects.FlowIO\-1.md)<[ValidationResult](Flowthru.Data.Validation.ValidationResult.md)\>

Effect producing validation result

### <a id="Flowthru_Data_IItem_LoadUntyped"></a> LoadUntyped\(\)

Loads data from the catalog entry as an untyped object.
Returns an effect that can fail.
The returned type matches the DataType property.

```csharp
FlowIO<object> LoadUntyped()
```

#### Returns

 [FlowIO](Flowthru.Effects.FlowIO\-1.md)<[object](https://learn.microsoft.com/dotnet/api/system.object)\>

### <a id="Flowthru_Data_IItem_SaveUntyped_System_Object_"></a> SaveUntyped\(object\)

Saves untyped data to the catalog entry.
Returns an effect that can fail.
The data type must be compatible with the DataType property.

```csharp
FlowIO<FlowUnit> SaveUntyped(object data)
```

#### Parameters

`data` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [FlowIO](Flowthru.Effects.FlowIO\-1.md)<[FlowUnit](Flowthru.Effects.FlowUnit.md)\>

