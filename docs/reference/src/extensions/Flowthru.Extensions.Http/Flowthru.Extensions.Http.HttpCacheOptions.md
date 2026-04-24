# <a id="Flowthru_Extensions_Http_HttpCacheOptions"></a> Class HttpCacheOptions

Namespace: [Flowthru.Extensions.Http](Flowthru.Extensions.Http.md)  
Assembly: Flowthru.Extensions.Http.dll  

Configuration for local disk caching of HTTP responses.

```csharp
public sealed class HttpCacheOptions
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[HttpCacheOptions](Flowthru.Extensions.Http.HttpCacheOptions.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

<p>
When set on <xref href="Flowthru.Extensions.Http.HttpOptions.Cache" data-throw-if-not-resolved="false"></xref>, <xref href="Flowthru.Core.Data.Storage.HttpStorageMediumProvider" data-throw-if-not-resolved="false"></xref>
returns a caching medium that persists response bodies to disk and uses HTTP
conditional-GET semantics (<code>ETag</code> / <code>If-None-Match</code>,
<code>Last-Modified</code> / <code>If-Modified-Since</code>) to avoid re-downloading
unchanged resources.
</p>
<p>
Cache files are stored under <xref href="Flowthru.Extensions.Http.HttpCacheOptions.Directory" data-throw-if-not-resolved="false"></xref> as two files per URL:
</p>
<ul><li><code>{sha256(url)}.dat</code> — the response body</li><li><code>{sha256(url)}.meta.json</code> — URL, ETag, and Last-Modified metadata</li></ul>

## Properties

### <a id="Flowthru_Extensions_Http_HttpCacheOptions_Directory"></a> Directory

Directory where cached response bodies and metadata are stored.
The directory is created if it does not exist.

```csharp
public required string Directory { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="Flowthru_Extensions_Http_HttpCacheOptions_MaxAge"></a> MaxAge

Maximum age of a cached response when the server provides no caching headers.
Once this TTL expires, a conditional GET is issued on the next access.
Defaults to 24 hours.

```csharp
public TimeSpan MaxAge { get; init; }
```

#### Property Value

 [TimeSpan](https://learn.microsoft.com/dotnet/api/system.timespan)

