# <a id="Flowthru_Core_Data_Storage_Medium_HttpStorageMedium"></a> Class HttpStorageMedium

Namespace: [Flowthru.Core.Data.Storage.Medium](Flowthru.Core.Data.Storage.Medium.md)  
Assembly: Flowthru.Extensions.Http.dll  

Storage medium for reading files over HTTP or HTTPS.

```csharp
public sealed class HttpStorageMedium : IStorageMedium
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[HttpStorageMedium](Flowthru.Core.Data.Storage.Medium.HttpStorageMedium.md)

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
<strong>Responsibility:</strong> Read raw byte streams from remote HTTP(S) endpoints.
</p>
<p>
<strong>Characteristics:</strong>
</p>
<ul><li>Read-only — <xref href="Flowthru.Core.Data.Storage.Medium.HttpStorageMedium.WriteStream(System.IO.Stream)" data-throw-if-not-resolved="false"></xref> is not supported</li><li>RequiresNetwork: true</li><li>CanStream: true — uses <code>ResponseHeadersRead</code> to avoid buffering the entire response</li><li>Pre-flight inspection uses an HTTP <code>HEAD</code> request</li></ul>
<p>
<strong>Usage via resolver (typical):</strong>
</p>
<pre><code class="lang-csharp">// Register the extension once in Program.cs
services.AddFlowthru(flowthru =&gt; flowthru.UseHttp());

// Then any catalog entry with an http:// or https:// path is resolved automatically
public IItem&lt;IEnumerable&lt;RetailSchema&gt;&gt; RetailData =&gt;
    CreateItem(() =&gt; ItemFactory.Enumerable.Csv&lt;RetailSchema&gt;(
        "RetailData",
        "https://example.com/data/retail.csv",
        _resolver));</code></pre>
<p>
<strong>Direct construction (tests, advanced):</strong>
</p>
<pre><code class="lang-csharp">var medium = new HttpStorageMedium(
    new Uri("https://example.com/data.csv"),
    httpClient);</code></pre>

## Constructors

### <a id="Flowthru_Core_Data_Storage_Medium_HttpStorageMedium__ctor_System_Uri_System_Net_Http_HttpClient_"></a> HttpStorageMedium\(Uri, HttpClient\)

Creates a new HTTP storage medium.

```csharp
public HttpStorageMedium(Uri uri, HttpClient httpClient)
```

#### Parameters

`uri` [Uri](https://learn.microsoft.com/dotnet/api/system.uri)

The URI of the remote resource.

`httpClient` [HttpClient](https://learn.microsoft.com/dotnet/api/system.net.http.httpclient)

The <xref href="System.Net.Http.HttpClient" data-throw-if-not-resolved="false"></xref> to use for requests.

## Properties

### <a id="Flowthru_Core_Data_Storage_Medium_HttpStorageMedium_Traits"></a> Traits

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

### <a id="Flowthru_Core_Data_Storage_Medium_HttpStorageMedium_Exists"></a> Exists\(\)

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

### <a id="Flowthru_Core_Data_Storage_Medium_HttpStorageMedium_ReadStream"></a> ReadStream\(\)

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

### <a id="Flowthru_Core_Data_Storage_Medium_HttpStorageMedium_WriteStream_System_IO_Stream_"></a> WriteStream\(Stream\)

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

