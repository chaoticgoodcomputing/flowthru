# <a id="Flowthru_Core_Data_Storage_PropertyMappingStrategy"></a> Enum PropertyMappingStrategy

Namespace: [Flowthru.Core.Data.Storage](Flowthru.Core.Data.Storage.md)  
Assembly: Flowthru.Core.dll  

Property mapping strategy used by a format serializer.

```csharp
public enum PropertyMappingStrategy
```

## Fields

`Adapter = 3` 

Serializer uses an adapter to translate SerializedLabel to native attributes.



`LibraryControlled = 2` 

Underlying library controls mapping with no programmatic access.



`NativeAttributes = 1` 

Serializer uses format-specific attributes (e.g., ML.NET [LoadColumn], CsvHelper [Name]).



`SerializedLabel = 0` 

Serializer respects [SerializedLabel] attributes using PropertyMappingHelper.



