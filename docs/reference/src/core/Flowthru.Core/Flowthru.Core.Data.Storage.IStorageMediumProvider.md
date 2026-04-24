# <a id="Flowthru_Core_Data_Storage_IStorageMediumProvider"></a> Interface IStorageMediumProvider

Namespace: [Flowthru.Core.Data.Storage](Flowthru.Core.Data.Storage.md)  
Assembly: Flowthru.Core.dll  

Factory for creating <xref href="Flowthru.Core.Data.Storage.IStorageMedium" data-throw-if-not-resolved="false"></xref> instances for a specific URI scheme.

```csharp
public interface IStorageMediumProvider
```

## Remarks

<p>
Implement this interface to add support for a new remote storage scheme (e.g., "sftp", "s3").
Providers are selected by scheme when <xref href="Flowthru.Core.Data.Storage.StorageMediumResolver" data-throw-if-not-resolved="false"></xref> dispatches a URI.
</p>
<p>
<strong>Registration:</strong>
</p>
<ul><li>
<strong>DI-based (recommended):</strong> Register as <code>IStorageMediumProvider</code>
singleton via an extension's <code>Use*()</code> builder method. The
<xref href="Flowthru.Core.Data.Storage.StorageMediumResolver" data-throw-if-not-resolved="false"></xref> collects all registered providers automatically.
</li><li>
<strong>Direct construction:</strong> Pass to
<xref href="Flowthru.Core.Data.Storage.StorageMediumResolver.Register(Flowthru.Core.Data.Storage.IStorageMediumProvider)" data-throw-if-not-resolved="false"></xref> when building a resolver manually
outside the DI container.
</li></ul>
<p>
<strong>Example (SFTP provider):</strong>
</p>
<pre><code class="lang-csharp">public sealed class SftpStorageMediumProvider : IStorageMediumProvider
{
    private readonly SftpOptions _options;

    public SftpStorageMediumProvider(SftpOptions options) =&gt; _options = options;

    public bool CanHandle(Uri uri) =&gt; uri.Scheme == "sftp";

    public IStorageMedium Create(Uri uri) =&gt;
        new SftpStorageMedium(uri, _options);
}</code></pre>

## Methods

### <a id="Flowthru_Core_Data_Storage_IStorageMediumProvider_CanHandle_System_Uri_"></a> CanHandle\(Uri\)

Returns <code>true</code> if this provider can handle the given URI.

```csharp
bool CanHandle(Uri uri)
```

#### Parameters

`uri` [Uri](https://learn.microsoft.com/dotnet/api/system.uri)

The parsed URI from a catalog entry's path string.

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="Flowthru_Core_Data_Storage_IStorageMediumProvider_Create_System_Uri_"></a> Create\(Uri\)

Creates a storage medium for the given URI.

```csharp
IStorageMedium Create(Uri uri)
```

#### Parameters

`uri` [Uri](https://learn.microsoft.com/dotnet/api/system.uri)

The parsed URI from a catalog entry's path string.

#### Returns

 [IStorageMedium](Flowthru.Core.Data.Storage.IStorageMedium.md)

