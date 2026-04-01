# <a id="Flowthru_Configuration_CatalogOptions"></a> Class CatalogOptions

Namespace: [Flowthru.Configuration](Flowthru.Configuration.md)  
Assembly: Flowthru.Core.dll  

Configuration options for data catalog construction.

```csharp
public class CatalogOptions
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[CatalogOptions](Flowthru.Configuration.CatalogOptions.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Properties

### <a id="Flowthru_Configuration_CatalogOptions_BasePath"></a> BasePath

Base path for dataset files (common constructor parameter).

```csharp
public string? BasePath { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="Flowthru_Configuration_CatalogOptions_ConnectionString"></a> ConnectionString

Connection string for database catalogs (common constructor parameter).

```csharp
public string? ConnectionString { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="Flowthru_Configuration_CatalogOptions_ConstructorArgs"></a> ConstructorArgs

Constructor arguments for the catalog (mapped to constructor parameters by name).

```csharp
public Dictionary<string, object> ConstructorArgs { get; set; }
```

#### Property Value

 [Dictionary](https://learn.microsoft.com/dotnet/api/system.collections.generic.dictionary\-2)<[string](https://learn.microsoft.com/dotnet/api/system.string), [object](https://learn.microsoft.com/dotnet/api/system.object)\>

### <a id="Flowthru_Configuration_CatalogOptions_Environment"></a> Environment

Environment-specific catalog configuration (e.g., local vs. remote).

```csharp
public string? Environment { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="Flowthru_Configuration_CatalogOptions_Type"></a> Type

The fully-qualified type name of the catalog class (e.g., "MyApp.Data.MyCatalog").

```csharp
public string? Type { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

