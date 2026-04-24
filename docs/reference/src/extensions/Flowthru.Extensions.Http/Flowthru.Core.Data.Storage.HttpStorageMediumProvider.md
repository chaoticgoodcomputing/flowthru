# <a id="Flowthru_Core_Data_Storage_HttpStorageMediumProvider"></a> Class HttpStorageMediumProvider

Namespace: [Flowthru.Core.Data.Storage](Flowthru.Core.Data.Storage.md)  
Assembly: Flowthru.Extensions.Http.dll  

Storage medium provider for <code>http://</code> and <code>https://</code> URIs.

```csharp
public sealed class HttpStorageMediumProvider : IStorageMediumProvider
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[HttpStorageMediumProvider](Flowthru.Core.Data.Storage.HttpStorageMediumProvider.md)

#### Implements

IStorageMediumProvider

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

Registered by <xref href="Flowthru.Extensions.Http.Services.FlowthruServiceBuilderHttpExtensions" data-throw-if-not-resolved="false"></xref>
as an <xref href="Flowthru.Core.Data.Storage.IStorageMediumProvider" data-throw-if-not-resolved="false"></xref> singleton. The
<xref href="Flowthru.Core.Data.Storage.StorageMediumResolver" data-throw-if-not-resolved="false"></xref> picks it up automatically when an HTTP(S) path
is used in a catalog entry.
<p>
When <xref href="Flowthru.Extensions.Http.HttpOptions.Cache" data-throw-if-not-resolved="false"></xref> is set, returns a
<xref href="Flowthru.Core.Data.Storage.Medium.CachedHttpStorageMedium" data-throw-if-not-resolved="false"></xref> that persists response bodies to disk and
uses conditional-GET semantics to avoid redundant downloads.
</p>

## Constructors

### <a id="Flowthru_Core_Data_Storage_HttpStorageMediumProvider__ctor_Microsoft_Extensions_Options_IOptions_Flowthru_Extensions_Http_HttpOptions__"></a> HttpStorageMediumProvider\(IOptions<HttpOptions\>\)

Creates a new HTTP provider using options resolved from DI.

```csharp
public HttpStorageMediumProvider(IOptions<HttpOptions> options)
```

#### Parameters

`options` [IOptions](https://learn.microsoft.com/dotnet/api/microsoft.extensions.options.ioptions\-1)<[HttpOptions](Flowthru.Extensions.Http.HttpOptions.md)\>

### <a id="Flowthru_Core_Data_Storage_HttpStorageMediumProvider__ctor"></a> HttpStorageMediumProvider\(\)

Creates a new HTTP provider with a default <xref href="System.Net.Http.HttpClient" data-throw-if-not-resolved="false"></xref> and no caching.
Use this for direct construction outside the DI container.

```csharp
public HttpStorageMediumProvider()
```

## Methods

### <a id="Flowthru_Core_Data_Storage_HttpStorageMediumProvider_CanHandle_System_Uri_"></a> CanHandle\(Uri\)

Returns <code>true</code> if this provider can handle the given URI.

```csharp
public bool CanHandle(Uri uri)
```

#### Parameters

`uri` [Uri](https://learn.microsoft.com/dotnet/api/system.uri)

The parsed URI from a catalog entry's path string.

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="Flowthru_Core_Data_Storage_HttpStorageMediumProvider_Create_System_Uri_"></a> Create\(Uri\)

Creates a storage medium for the given URI.

```csharp
public IStorageMedium Create(Uri uri)
```

#### Parameters

`uri` [Uri](https://learn.microsoft.com/dotnet/api/system.uri)

The parsed URI from a catalog entry's path string.

#### Returns

 IStorageMedium

