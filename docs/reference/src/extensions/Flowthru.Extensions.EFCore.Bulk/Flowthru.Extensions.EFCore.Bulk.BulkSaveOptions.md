# <a id="Flowthru_Extensions_EFCore_Bulk_BulkSaveOptions"></a> Class BulkSaveOptions

Namespace: [Flowthru.Extensions.EFCore.Bulk](Flowthru.Extensions.EFCore.Bulk.md)  
Assembly: Flowthru.Extensions.EFCore.Bulk.dll  

Configuration options for bulk save operations. Exposes the subset of
<code>EFCore.BulkExtensions.BulkConfig</code> properties that are relevant to
Flowthru catalog item save strategies.

```csharp
public record BulkSaveOptions : IEquatable<BulkSaveOptions>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[BulkSaveOptions](Flowthru.Extensions.EFCore.Bulk.BulkSaveOptions.md)

#### Implements

[IEquatable<BulkSaveOptions\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

Use <code>with { }</code> syntax to customize from defaults:
<pre><code class="lang-csharp">new BulkSaveOptions { BatchSize = 5000, TimeoutSeconds = 120 }</code></pre>

## Properties

### <a id="Flowthru_Extensions_EFCore_Bulk_BulkSaveOptions_BatchSize"></a> BatchSize

Number of rows per bulk operation batch. Default: 2000.

```csharp
public int BatchSize { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="Flowthru_Extensions_EFCore_Bulk_BulkSaveOptions_OnProgress"></a> OnProgress

Optional progress callback invoked with a percentage (0–100) during the
bulk operation. Useful for logging large loads.

```csharp
public Action<decimal>? OnProgress { get; init; }
```

#### Property Value

 [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<[decimal](https://learn.microsoft.com/dotnet/api/system.decimal)\>?

### <a id="Flowthru_Extensions_EFCore_Bulk_BulkSaveOptions_PreserveInsertOrder"></a> PreserveInsertOrder

Preserve the insert order of entities. Default: true.

```csharp
public bool PreserveInsertOrder { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="Flowthru_Extensions_EFCore_Bulk_BulkSaveOptions_PropertiesToExclude"></a> PropertiesToExclude

Blacklist of CLR property names to exclude from the bulk operation.
<code>null</code> excludes nothing.
Mutually exclusive with <xref href="Flowthru.Extensions.EFCore.Bulk.BulkSaveOptions.PropertiesToInclude" data-throw-if-not-resolved="false"></xref>.

```csharp
public IReadOnlyList<string>? PropertiesToExclude { get; init; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>?

### <a id="Flowthru_Extensions_EFCore_Bulk_BulkSaveOptions_PropertiesToInclude"></a> PropertiesToInclude

Whitelist of CLR property names to include in the bulk operation.
<code>null</code> includes all mapped properties.
Mutually exclusive with <xref href="Flowthru.Extensions.EFCore.Bulk.BulkSaveOptions.PropertiesToExclude" data-throw-if-not-resolved="false"></xref>.

```csharp
public IReadOnlyList<string>? PropertiesToInclude { get; init; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>?

### <a id="Flowthru_Extensions_EFCore_Bulk_BulkSaveOptions_SetOutputIdentity"></a> SetOutputIdentity

Reload database-generated identity values back into entities after insert.
Required when downstream steps depend on auto-generated PKs. Default: false.

```csharp
public bool SetOutputIdentity { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="Flowthru_Extensions_EFCore_Bulk_BulkSaveOptions_TimeoutSeconds"></a> TimeoutSeconds

Timeout in seconds for the bulk copy operation. <code>null</code> uses the
provider default (typically 30 seconds). Set to <code>0</code> for no limit.

```csharp
public int? TimeoutSeconds { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)?

### <a id="Flowthru_Extensions_EFCore_Bulk_BulkSaveOptions_UseUnlogged"></a> UseUnlogged

PostgreSQL-specific: use UNLOGGED temp tables for merge operations.
Faster but not crash-safe. Default: false.

```csharp
public bool UseUnlogged { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

