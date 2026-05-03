# <a id="Flowthru_Core_Data_Storage_PropertyMappingStrategy"></a> Enum PropertyMappingStrategy

Namespace: [Flowthru.Core.Data.Storage](Flowthru.Core.Data.Storage.md)  
Assembly: Flowthru.Core.dll  

Property mapping strategy used by a format serializer.

```csharp
public enum PropertyMappingStrategy
```

## Fields

`LibraryControlled = 1` 

Underlying library controls mapping with no programmatic access.



`SerializedLabel = 0` 

Serializer respects [SerializedLabel] attributes using PropertyMappingPlanner.



