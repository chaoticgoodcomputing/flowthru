# <a id="Flowthru_Extensions_GQL_Data_PageInfo"></a> Class PageInfo

Namespace: [Flowthru.Extensions.GQL.Data](Flowthru.Extensions.GQL.Data.md)  
Assembly: Flowthru.Extensions.GQL.dll  

Pagination metadata returned by a Relay-style GraphQL connection.

```csharp
public record PageInfo : IEquatable<PageInfo>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[PageInfo](Flowthru.Extensions.GQL.Data.PageInfo.md)

#### Implements

[IEquatable<PageInfo\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Constructors

### <a id="Flowthru_Extensions_GQL_Data_PageInfo__ctor_System_Boolean_System_String_"></a> PageInfo\(bool, string?\)

Pagination metadata returned by a Relay-style GraphQL connection.

```csharp
public PageInfo(bool HasNextPage, string? EndCursor)
```

#### Parameters

`HasNextPage` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether a subsequent page exists.

`EndCursor` [string](https://learn.microsoft.com/dotnet/api/system.string)?

The opaque cursor identifying the end of the current page.

## Properties

### <a id="Flowthru_Extensions_GQL_Data_PageInfo_EndCursor"></a> EndCursor

The opaque cursor identifying the end of the current page.

```csharp
public string? EndCursor { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="Flowthru_Extensions_GQL_Data_PageInfo_HasNextPage"></a> HasNextPage

Whether a subsequent page exists.

```csharp
public bool HasNextPage { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

