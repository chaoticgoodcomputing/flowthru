# <a id="Flowthru_Extensions_EFCore_Data_DbScope"></a> Class DbScope

Namespace: [Flowthru.Extensions.EFCore.Data](Flowthru.Extensions.EFCore.Data.md)  
Assembly: Flowthru.Extensions.EFCore.dll  

Identifies which database instance a <xref href="Flowthru.Extensions.EFCore.Data.DbQuery%601" data-throw-if-not-resolved="false"></xref> or
<xref href="Flowthru.Core.Data.Storage.DbQueryStorageAdapter%601" data-throw-if-not-resolved="false"></xref> is associated with,
enabling the fused INSERT-FROM-SELECT save path when source and destination share the same DB.

```csharp
public abstract class DbScope
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[DbScope](Flowthru.Extensions.EFCore.Data.DbScope.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

<p>
Two scopes are considered the same database when they are equal under their equality rule:
</p>
<ul><li>
  <xref href="Flowthru.Extensions.EFCore.Data.DbScope.Inferred(System.Object)" data-throw-if-not-resolved="false"></xref> — reference equality on the factory object.
  The default for catalog entries created via <code>EFCoreItemFactory.Query</code>.
</li><li>
  <xref href="Flowthru.Extensions.EFCore.Data.DbScope.Explicit(System.String)" data-throw-if-not-resolved="false"></xref> — case-sensitive string equality on the scope name.
  Use this when two catalog entries point to the same logical database but use
  different factory instances (e.g., two separate DI-injected factory objects).
</li></ul>

## Methods

### <a id="Flowthru_Extensions_EFCore_Data_DbScope_Explicit_System_String_"></a> Explicit\(string\)

Creates a named scope.
Two entries with the same <code class="paramref">name</code> are considered the same database
regardless of factory instance identity.

```csharp
public static DbScope Explicit(string name)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

Case-sensitive scope name.

#### Returns

 [DbScope](Flowthru.Extensions.EFCore.Data.DbScope.md)

### <a id="Flowthru_Extensions_EFCore_Data_DbScope_Inferred_System_Object_"></a> Inferred\(object\)

Creates a scope inferred from factory object identity.
Two entries sharing the exact same factory reference are considered the same database.

```csharp
public static DbScope Inferred(object factory)
```

#### Parameters

`factory` [object](https://learn.microsoft.com/dotnet/api/system.object)

The factory object whose reference identity keys this scope.

#### Returns

 [DbScope](Flowthru.Extensions.EFCore.Data.DbScope.md)

