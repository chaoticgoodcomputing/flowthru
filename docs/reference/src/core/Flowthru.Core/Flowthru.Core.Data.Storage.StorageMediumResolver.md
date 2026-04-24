# <a id="Flowthru_Core_Data_Storage_StorageMediumResolver"></a> Class StorageMediumResolver

Namespace: [Flowthru.Core.Data.Storage](Flowthru.Core.Data.Storage.md)  
Assembly: Flowthru.Core.dll  

Default implementation of <xref href="Flowthru.Core.Data.Storage.IStorageMediumResolver" data-throw-if-not-resolved="false"></xref>.

```csharp
public sealed class StorageMediumResolver : IStorageMediumResolver
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[StorageMediumResolver](Flowthru.Core.Data.Storage.StorageMediumResolver.md)

#### Implements

[IStorageMediumResolver](Flowthru.Core.Data.Storage.IStorageMediumResolver.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

<p>
Consults registered <xref href="Flowthru.Core.Data.Storage.IStorageMediumProvider" data-throw-if-not-resolved="false"></xref> instances in order, falling
back to <xref href="Flowthru.Core.Data.Storage.Medium.FileStorageMedium" data-throw-if-not-resolved="false"></xref> for bare file paths and <code>file://</code> URIs.
</p>
<p>
<strong>Two construction modes:</strong>
</p>
<ul><li>
<strong>DI-injected:</strong> The DI container passes all registered
<code>IStorageMediumProvider</code> singletons via
<xref href="Flowthru.Core.Data.Storage.StorageMediumResolver.%23ctor(System.Collections.Generic.IEnumerable%7bFlowthru.Core.Data.Storage.IStorageMediumProvider%7d)" data-throw-if-not-resolved="false"></xref>.
Used automatically when <code>services.AddFlowthru(...)</code> registers this type.
</li><li>
<strong>Direct construction:</strong> Use the parameterless constructor and chain
<xref href="Flowthru.Core.Data.Storage.StorageMediumResolver.Register(Flowthru.Core.Data.Storage.IStorageMediumProvider)" data-throw-if-not-resolved="false"></xref> calls. Useful in tests or standalone programs that don't
use the DI service layer.
</li></ul>

## Constructors

### <a id="Flowthru_Core_Data_Storage_StorageMediumResolver__ctor_System_Collections_Generic_IEnumerable_Flowthru_Core_Data_Storage_IStorageMediumProvider__"></a> StorageMediumResolver\(IEnumerable<IStorageMediumProvider\>\)

DI constructor — providers are collected from all registered
<xref href="Flowthru.Core.Data.Storage.IStorageMediumProvider" data-throw-if-not-resolved="false"></xref> singletons.

```csharp
public StorageMediumResolver(IEnumerable<IStorageMediumProvider> providers)
```

#### Parameters

`providers` [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<[IStorageMediumProvider](Flowthru.Core.Data.Storage.IStorageMediumProvider.md)\>

### <a id="Flowthru_Core_Data_Storage_StorageMediumResolver__ctor"></a> StorageMediumResolver\(\)

Parameterless constructor for direct construction outside the DI container.
Chain <xref href="Flowthru.Core.Data.Storage.StorageMediumResolver.Register(Flowthru.Core.Data.Storage.IStorageMediumProvider)" data-throw-if-not-resolved="false"></xref> to add providers.

```csharp
public StorageMediumResolver()
```

## Methods

### <a id="Flowthru_Core_Data_Storage_StorageMediumResolver_Register_Flowthru_Core_Data_Storage_IStorageMediumProvider_"></a> Register\(IStorageMediumProvider\)

Adds a provider to the resolver's dispatch chain.

```csharp
public StorageMediumResolver Register(IStorageMediumProvider provider)
```

#### Parameters

`provider` [IStorageMediumProvider](Flowthru.Core.Data.Storage.IStorageMediumProvider.md)

#### Returns

 [StorageMediumResolver](Flowthru.Core.Data.Storage.StorageMediumResolver.md)

<code>this</code> for fluent chaining.

### <a id="Flowthru_Core_Data_Storage_StorageMediumResolver_Resolve_System_String_"></a> Resolve\(string\)

Returns the appropriate <xref href="Flowthru.Core.Data.Storage.IStorageMedium" data-throw-if-not-resolved="false"></xref> for the given path or URI string.

```csharp
public IStorageMedium Resolve(string pathOrUri)
```

#### Parameters

`pathOrUri` [string](https://learn.microsoft.com/dotnet/api/system.string)

A local file path (absolute or relative), a <code>file://</code> URI, or any other URI
whose scheme is handled by a registered <xref href="Flowthru.Core.Data.Storage.IStorageMediumProvider" data-throw-if-not-resolved="false"></xref>.

#### Returns

 [IStorageMedium](Flowthru.Core.Data.Storage.IStorageMedium.md)

