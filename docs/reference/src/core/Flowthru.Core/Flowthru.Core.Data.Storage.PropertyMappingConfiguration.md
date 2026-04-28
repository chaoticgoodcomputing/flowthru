# <a id="Flowthru_Core_Data_Storage_PropertyMappingConfiguration"></a> Class PropertyMappingConfiguration

Namespace: [Flowthru.Core.Data.Storage](Flowthru.Core.Data.Storage.md)  
Assembly: Flowthru.Core.dll  

Describes how a format serializer handles property-to-field name mapping.

```csharp
public sealed class PropertyMappingConfiguration
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[PropertyMappingConfiguration](Flowthru.Core.Data.Storage.PropertyMappingConfiguration.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

<p>
<strong>Purpose:</strong> Makes property mapping strategy explicit and discoverable for each serializer.
</p>

## Properties

### <a id="Flowthru_Core_Data_Storage_PropertyMappingConfiguration_Description"></a> Description

Optional description of the mapping behavior.

```csharp
public string? Description { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="Flowthru_Core_Data_Storage_PropertyMappingConfiguration_MetadataType"></a> MetadataType

For NativeAttributes strategy: the name of the attribute type(s) used.
For Adapter strategy: the adapter type.

```csharp
public Type? MetadataType { get; }
```

#### Property Value

 [Type](https://learn.microsoft.com/dotnet/api/system.type)?

### <a id="Flowthru_Core_Data_Storage_PropertyMappingConfiguration_Strategy"></a> Strategy

The strategy used for property mapping.

```csharp
public PropertyMappingStrategy Strategy { get; }
```

#### Property Value

 [PropertyMappingStrategy](Flowthru.Core.Data.Storage.PropertyMappingStrategy.md)

### <a id="Flowthru_Core_Data_Storage_PropertyMappingConfiguration_SupportsSerializedLabel"></a> SupportsSerializedLabel

Checks if the serializer supports SerializedLabel attributes (directly or via adapter).

```csharp
public bool SupportsSerializedLabel { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="Flowthru_Core_Data_Storage_PropertyMappingConfiguration_FromAdapter__1"></a> FromAdapter<TAdapter\>\(\)

Serializer uses an adapter to bridge SerializedLabel with native attributes.

```csharp
public static PropertyMappingConfiguration FromAdapter<TAdapter>()
```

#### Returns

 [PropertyMappingConfiguration](Flowthru.Core.Data.Storage.PropertyMappingConfiguration.md)

Configuration for adapter-based mapping

#### Type Parameters

`TAdapter` 

The adapter type that performs the bridging

### <a id="Flowthru_Core_Data_Storage_PropertyMappingConfiguration_FromNativeAttributes_System_String_"></a> FromNativeAttributes\(string\)

Serializer uses format-specific attributes (e.g., ML.NET's [LoadColumn], [ColumnName]).

```csharp
public static PropertyMappingConfiguration FromNativeAttributes(string attributeDescription)
```

#### Parameters

`attributeDescription` [string](https://learn.microsoft.com/dotnet/api/system.string)

Description of native attributes used

#### Returns

 [PropertyMappingConfiguration](Flowthru.Core.Data.Storage.PropertyMappingConfiguration.md)

Configuration for native attribute mapping

### <a id="Flowthru_Core_Data_Storage_PropertyMappingConfiguration_FromSerializedLabel__1"></a> FromSerializedLabel<TRow\>\(\)

Serializer uses SerializedLabel attributes via PropertyMappingHelper.

```csharp
public static PropertyMappingConfiguration FromSerializedLabel<TRow>()
```

#### Returns

 [PropertyMappingConfiguration](Flowthru.Core.Data.Storage.PropertyMappingConfiguration.md)

Configuration for SerializedLabel-based mapping

#### Type Parameters

`TRow` 

The schema type

### <a id="Flowthru_Core_Data_Storage_PropertyMappingConfiguration_LibraryControlled_System_String_"></a> LibraryControlled\(string?\)

Underlying library controls mapping with no programmatic API.
Property names must match storage field names exactly.

```csharp
public static PropertyMappingConfiguration LibraryControlled(string? limitation = null)
```

#### Parameters

`limitation` [string](https://learn.microsoft.com/dotnet/api/system.string)?

Optional description of the limitation

#### Returns

 [PropertyMappingConfiguration](Flowthru.Core.Data.Storage.PropertyMappingConfiguration.md)

Configuration for library-controlled mapping

