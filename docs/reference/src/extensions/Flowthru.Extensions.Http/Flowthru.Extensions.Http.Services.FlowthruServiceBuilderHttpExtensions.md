# <a id="Flowthru_Extensions_Http_Services_FlowthruServiceBuilderHttpExtensions"></a> Class FlowthruServiceBuilderHttpExtensions

Namespace: [Flowthru.Extensions.Http.Services](Flowthru.Extensions.Http.Services.md)  
Assembly: Flowthru.Extensions.Http.dll  

Extension methods for registering HTTP storage support with <xref href="Flowthru.Core.Services.IFlowthruBuilder" data-throw-if-not-resolved="false"></xref>.

```csharp
public static class FlowthruServiceBuilderHttpExtensions
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[FlowthruServiceBuilderHttpExtensions](Flowthru.Extensions.Http.Services.FlowthruServiceBuilderHttpExtensions.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Methods

### <a id="Flowthru_Extensions_Http_Services_FlowthruServiceBuilderHttpExtensions_UseHttp_Flowthru_Core_Services_IFlowthruBuilder_"></a> UseHttp\(IFlowthruBuilder\)

Enables HTTP(S) remote file access for catalog entries that use
<code>http://</code> or <code>https://</code> URIs as file paths.

```csharp
public static IFlowthruBuilder UseHttp(this IFlowthruBuilder builder)
```

#### Parameters

`builder` IFlowthruBuilder

The Flowthru service builder.

#### Returns

 IFlowthruBuilder

The builder for method chaining.

#### Remarks

<p>
Once registered, any file-backed catalog factory method
(<code>ItemFactory.Enumerable.Csv</code>, <code>Parquet</code>, <code>Json</code>, etc.) that
receives an <xref href="Flowthru.Core.Data.Storage.IStorageMediumResolver" data-throw-if-not-resolved="false"></xref> will
automatically route <code>http://</code> and <code>https://</code> paths through
<xref href="Flowthru.Core.Data.Storage.Medium.HttpStorageMedium" data-throw-if-not-resolved="false"></xref>.
</p>
<p>
Configuration is bound from the <code>Flowthru:Http</code> section. Properties not
present in configuration retain their default values.
</p>
<p>
<strong>Example:</strong>
<pre><code class="lang-csharp">services.AddFlowthru(configuration, flowthru =&gt;
{
    flowthru.UseHttp();
    flowthru.RegisterCatalog(sp =&gt; new MyCatalog(
        dataPath,
        sp.GetRequiredService&lt;IStorageMediumResolver&gt;()
    ));
});</code></pre>
</p>
<p>
Catalog entries with local file paths are unaffected — they continue to resolve
to <xref href="Flowthru.Core.Data.Storage.Medium.FileStorageMedium" data-throw-if-not-resolved="false"></xref>.
</p>

### <a id="Flowthru_Extensions_Http_Services_FlowthruServiceBuilderHttpExtensions_UseHttp_Flowthru_Core_Services_IFlowthruBuilder_System_Action_Flowthru_Extensions_Http_HttpOptions__"></a> UseHttp\(IFlowthruBuilder, Action<HttpOptions\>\)

Enables HTTP(S) remote file access with code-first configuration overrides.

```csharp
public static IFlowthruBuilder UseHttp(this IFlowthruBuilder builder, Action<HttpOptions> configure)
```

#### Parameters

`builder` IFlowthruBuilder

The Flowthru service builder.

`configure` [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<[HttpOptions](Flowthru.Extensions.Http.HttpOptions.md)\>

Action to override HTTP options after config-file binding.

#### Returns

 IFlowthruBuilder

The builder for method chaining.

#### Remarks

<p>
The <code class="paramref">configure</code> callback runs after <code>Flowthru:Http</code> section
binding, so it can selectively override specific values.
</p>
<p>
<strong>Example (custom timeout for large remote files):</strong>
<pre><code class="lang-csharp">services.AddFlowthru(configuration, flowthru =&gt;
{
    flowthru.UseHttp(http =&gt;
    {
        http.Timeout = TimeSpan.FromMinutes(15);
        http.UserAgent = "MyOrg-DataPipeline/2.0";
    });
});</code></pre>
</p>

