# <a id="Flowthru_Extensions_Http_HttpOptions"></a> Class HttpOptions

Namespace: [Flowthru.Extensions.Http](Flowthru.Extensions.Http.md)  
Assembly: Flowthru.Extensions.Http.dll  

Configuration options for the HTTP storage medium extension.

```csharp
public sealed class HttpOptions
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[HttpOptions](Flowthru.Extensions.Http.HttpOptions.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Properties

### <a id="Flowthru_Extensions_Http_HttpOptions_Cache"></a> Cache

Optional local disk cache configuration. When <code>null</code> (default), every
<xref href="Flowthru.Core.Data.Storage.Medium.HttpStorageMedium.ReadStream" data-throw-if-not-resolved="false"></xref> call
issues a fresh HTTP request. Set this to enable conditional-GET caching.

```csharp
public HttpCacheOptions? Cache { get; set; }
```

#### Property Value

 [HttpCacheOptions](Flowthru.Extensions.Http.HttpCacheOptions.md)?

### <a id="Flowthru_Extensions_Http_HttpOptions_Timeout"></a> Timeout

Timeout for HTTP requests. Defaults to 5 minutes to accommodate large remote files.

```csharp
public TimeSpan Timeout { get; set; }
```

#### Property Value

 [TimeSpan](https://learn.microsoft.com/dotnet/api/system.timespan)

### <a id="Flowthru_Extensions_Http_HttpOptions_UserAgent"></a> UserAgent

Optional <code>User-Agent</code> header value sent with every request.
Defaults to <code>Flowthru-Http/1.0</code>.

```csharp
public string UserAgent { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

