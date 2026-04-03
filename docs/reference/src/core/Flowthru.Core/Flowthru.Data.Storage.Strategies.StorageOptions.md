# <a id="Flowthru_Data_Storage_Strategies_StorageOptions"></a> Class StorageOptions

Namespace: [Flowthru.Data.Storage.Strategies](Flowthru.Data.Storage.Strategies.md)  
Assembly: Flowthru.Core.dll  

Options for configuring storage entry creation.

```csharp
public sealed record StorageOptions : IEquatable<StorageOptions>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[StorageOptions](Flowthru.Data.Storage.Strategies.StorageOptions.md)

#### Implements

[IEquatable<StorageOptions\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

Provides flexibility for strategy-specific configuration without
requiring changes to the factory interface.

## Properties

### <a id="Flowthru_Data_Storage_Strategies_StorageOptions_Metadata"></a> Metadata

Additional strategy-specific metadata.

```csharp
public Dictionary<string, object>? Metadata { get; init; }
```

#### Property Value

 [Dictionary](https://learn.microsoft.com/dotnet/api/system.collections.generic.dictionary\-2)<[string](https://learn.microsoft.com/dotnet/api/system.string), [object](https://learn.microsoft.com/dotnet/api/system.object)\>?

#### Remarks

Examples:
- Excel: {"SheetName": "Data"}
- Database: {"Schema": "analytics", "Timeout": 30}
- Parquet: {"CompressionCodec": "SNAPPY"}

### <a id="Flowthru_Data_Storage_Strategies_StorageOptions_Path"></a> Path

Relative path or identifier for the storage location.

```csharp
public string? Path { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

#### Remarks

Interpretation depends on the strategy:
- CSV: File path relative to base directory (e.g., "_01_Raw/data.csv")
- Database: Table name or qualified identifier (e.g., "dbo.Companies")
- Memory: Ignored (memory storage has no path)

## Methods

### <a id="Flowthru_Data_Storage_Strategies_StorageOptions_WithPath_System_String_"></a> WithPath\(string\)

Creates storage options with a path.

```csharp
public static StorageOptions WithPath(string path)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [StorageOptions](Flowthru.Data.Storage.Strategies.StorageOptions.md)

