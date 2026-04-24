# <a id="Flowthru_Core_Data_Storage_IStorageMediumResolver"></a> Interface IStorageMediumResolver

Namespace: [Flowthru.Core.Data.Storage](Flowthru.Core.Data.Storage.md)  
Assembly: Flowthru.Core.dll  

Resolves the appropriate <xref href="Flowthru.Core.Data.Storage.IStorageMedium" data-throw-if-not-resolved="false"></xref> for a given file path or URI string.

```csharp
public interface IStorageMediumResolver
```

## Remarks

<p>
Falls back to <xref href="Flowthru.Core.Data.Storage.Medium.FileStorageMedium" data-throw-if-not-resolved="false"></xref> for bare paths and <code>file://</code>
URIs. For all other URI schemes, registered <xref href="Flowthru.Core.Data.Storage.IStorageMediumProvider" data-throw-if-not-resolved="false"></xref>
implementations are consulted in registration order.
</p>
<p>
<strong>DI-based usage (recommended):</strong>
</p>
<pre><code class="lang-csharp">services.AddFlowthru(flowthru =&gt;
{
    flowthru.UseHttp();          // registers HttpStorageMediumProvider
    flowthru.RegisterCatalog(sp =&gt; new MyCatalog(
        dataPath,
        sp.GetRequiredService&lt;IStorageMediumResolver&gt;()
    ));
});</code></pre>
<p>
<strong>Direct-construction usage (standalone, tests):</strong>
</p>
<pre><code class="lang-csharp">var resolver = new StorageMediumResolver()
    .Register(new HttpStorageMediumProvider());

var catalog = new MyCatalog(dataPath, resolver);</code></pre>

## Methods

### <a id="Flowthru_Core_Data_Storage_IStorageMediumResolver_Resolve_System_String_"></a> Resolve\(string\)

Returns the appropriate <xref href="Flowthru.Core.Data.Storage.IStorageMedium" data-throw-if-not-resolved="false"></xref> for the given path or URI string.

```csharp
IStorageMedium Resolve(string pathOrUri)
```

#### Parameters

`pathOrUri` [string](https://learn.microsoft.com/dotnet/api/system.string)

A local file path (absolute or relative), a <code>file://</code> URI, or any other URI
whose scheme is handled by a registered <xref href="Flowthru.Core.Data.Storage.IStorageMediumProvider" data-throw-if-not-resolved="false"></xref>.

#### Returns

 [IStorageMedium](Flowthru.Core.Data.Storage.IStorageMedium.md)

