# <a id="Flowthru_Core_Data_Storage_Medium_CachedHttpStorageMedium"></a> Class CachedHttpStorageMedium

Namespace: [Flowthru.Core.Data.Storage.Medium](Flowthru.Core.Data.Storage.Medium.md)  
Assembly: Flowthru.Extensions.Http.dll  

HTTP(S) storage medium with local disk caching using conditional-GET semantics.

```csharp
public sealed class CachedHttpStorageMedium : IStorageMedium
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[CachedHttpStorageMedium](Flowthru.Core.Data.Storage.Medium.CachedHttpStorageMedium.md)

#### Implements

IStorageMedium

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

<p>
On first access, downloads the resource and writes two files to the cache directory:
</p>
<ul><li><code>{sha256(url)}.dat</code> — response body</li><li><code>{sha256(url)}.meta.json</code> — ETag, Last-Modified, and original URL</li></ul>
<p>
On subsequent accesses, issues a conditional <code>GET</code> with
<code>If-None-Match</code> / <code>If-Modified-Since</code> headers. A <code>304 Not Modified</code>
response streams from the cached <code>.dat</code> file without downloading again.
</p>
<p>
When the server provides no caching headers, the
<xref href="Flowthru.Extensions.Http.HttpCacheOptions.MaxAge" data-throw-if-not-resolved="false"></xref> TTL is used as a
fallback: once the cache entry is older than <code>MaxAge</code>, a fresh request is made.
</p>
<p>
<strong>Pre-flight:</strong> <xref href="Flowthru.Core.Data.Storage.Medium.CachedHttpStorageMedium.Exists" data-throw-if-not-resolved="false"></xref> returns <code>true</code> immediately
if a cached <code>.dat</code> file is present, sparing the network entirely.
</p>

## Constructors

### <a id="Flowthru_Core_Data_Storage_Medium_CachedHttpStorageMedium__ctor_System_Uri_System_Net_Http_HttpClient_System_String_System_TimeSpan_"></a> CachedHttpStorageMedium\(Uri, HttpClient, string, TimeSpan\)

```csharp
public CachedHttpStorageMedium(Uri uri, HttpClient httpClient, string cacheDirectory, TimeSpan maxAge)
```

#### Parameters

`uri` [Uri](https://learn.microsoft.com/dotnet/api/system.uri)

Remote resource URI.

`httpClient` [HttpClient](https://learn.microsoft.com/dotnet/api/system.net.http.httpclient)

HTTP client to use for requests.

`cacheDirectory` [string](https://learn.microsoft.com/dotnet/api/system.string)

Directory where cache files are stored.

`maxAge` [TimeSpan](https://learn.microsoft.com/dotnet/api/system.timespan)

TTL used when the server provides no cache headers.

## Properties

### <a id="Flowthru_Core_Data_Storage_Medium_CachedHttpStorageMedium_Traits"></a> Traits

Structural constraints and capabilities of this storage medium.

```csharp
public StorageTraits Traits { get; }
```

#### Property Value

 StorageTraits

#### Remarks

Medium traits focus on WHERE data is stored and the access patterns it supports.
For composed adapters, these traits are merged with format and container traits.

## Methods

### <a id="Flowthru_Core_Data_Storage_Medium_CachedHttpStorageMedium_Exists"></a> Exists\(\)

Checks if data exists at this storage location.

```csharp
public FlowIO<bool> Exists()
```

#### Returns

 FlowIO<[bool](https://learn.microsoft.com/dotnet/api/system.boolean)\>

Effect that produces true if data exists, false otherwise

#### Remarks

<p>
This is used to determine if a catalog entry is a "seed" (Layer 0 input)
or if it's produced by a step in the pipeline.
</p>

### <a id="Flowthru_Core_Data_Storage_Medium_CachedHttpStorageMedium_ReadStream"></a> ReadStream\(\)

Reads raw bytes from storage as a stream.

```csharp
public FlowIO<Stream> ReadStream()
```

#### Returns

 FlowIO<[Stream](https://learn.microsoft.com/dotnet/api/system.io.stream)\>

Effect that produces a readable stream on success

#### Remarks

<p>
The returned stream should be positioned at the beginning and ready to read.
The caller is responsible for disposing the stream.
</p>
<p>
<strong>Error Conditions:</strong>
</p>
<ul><li>Storage location does not exist</li><li>Access denied (permissions)</li><li>Network failure (for remote storage)</li><li>I/O error</li></ul>

### <a id="Flowthru_Core_Data_Storage_Medium_CachedHttpStorageMedium_WriteStream_System_IO_Stream_"></a> WriteStream\(Stream\)

Writes raw bytes to storage from a stream.

```csharp
public FlowIO<FlowUnit> WriteStream(Stream stream)
```

#### Parameters

`stream` [Stream](https://learn.microsoft.com/dotnet/api/system.io.stream)

Stream containing data to write

#### Returns

 FlowIO<FlowUnit\>

Effect that completes on successful write

#### Remarks

<p>
The stream will be read from its current position to the end.
The implementation should handle creating parent directories if needed.
</p>
<p>
<strong>Atomicity:</strong>
</p>
<p>
Implementations should strive for atomic writes (write to temp, then rename)
to avoid partial writes on failure.
</p>
<p>
<strong>Error Conditions:</strong>
</p>
<ul><li>Insufficient disk space</li><li>Access denied (permissions)</li><li>Network failure (for remote storage)</li><li>I/O error</li></ul>

#### Exceptions

 [NotSupportedException](https://learn.microsoft.com/dotnet/api/system.notsupportedexception)

Always thrown — HTTP sources are read-only.

