# <a id="Flowthru_Meta_FlowthruMetadataBuilder"></a> Class FlowthruMetadataBuilder

Namespace: [Flowthru.Meta](Flowthru.Meta.md)  
Assembly: Flowthru.Core.dll  

Fluent builder for configuring metadata providers and export settings.

```csharp
public class FlowthruMetadataBuilder
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[FlowthruMetadataBuilder](Flowthru.Meta.FlowthruMetadataBuilder.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

<p>
Use this builder to register metadata providers with custom configuration.
Providers are executed in registration order during metadata export.
</p>
<p>
<strong>Example usage:</strong>
</p>
<pre><code class="lang-csharp">builder.ConfigureMetadata(meta =&gt; meta
    .AddProvider&lt;JsonMetadataProvider, JsonMetadataProviderBuilder&gt;(json =&gt; json
        .WithOutputDirectory("metadata")
        .WithTimestamp("yyyy-MM-dd_HH-mm-ss")
        .UseCompactFormat())
    .AddProvider&lt;MermaidMetadataProvider, MermaidMetadataProviderBuilder&gt;(mermaid =&gt; mermaid
        .WithOutputDirectory("metadata")
        .WithDirection(MermaidMetadataProvider.MermaidFlowchartDirection.LeftToRight))
);</code></pre>

## Methods

### <a id="Flowthru_Meta_FlowthruMetadataBuilder_AddProvider__2_System_Action___1__"></a> AddProvider<TProvider, TBuilder\>\(Action<TBuilder\>?\)

Adds a metadata provider with optional configuration.

```csharp
public FlowthruMetadataBuilder AddProvider<TProvider, TBuilder>(Action<TBuilder>? configure = null) where TProvider : IMetadataProvider where TBuilder : new()
```

#### Parameters

`configure` [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<TBuilder\>?

Optional configuration action for the provider's builder

#### Returns

 [FlowthruMetadataBuilder](Flowthru.Meta.FlowthruMetadataBuilder.md)

This builder for fluent chaining

#### Type Parameters

`TProvider` 

The metadata provider type (must implement <xref href="Flowthru.Meta.Providers.IMetadataProvider" data-throw-if-not-resolved="false"></xref> and have <xref href="Flowthru.Meta.MetadataProviderBuilderAttribute" data-throw-if-not-resolved="false"></xref>)

`TBuilder` 

The builder type for the provider

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

Thrown when provider type lacks <xref href="Flowthru.Meta.MetadataProviderBuilderAttribute" data-throw-if-not-resolved="false"></xref> or builder type mismatch

### <a id="Flowthru_Meta_FlowthruMetadataBuilder_AddProvider_Flowthru_Meta_Providers_IMetadataProvider_"></a> AddProvider\(IMetadataProvider\)

Adds a custom metadata provider instance directly.

```csharp
public FlowthruMetadataBuilder AddProvider(IMetadataProvider provider)
```

#### Parameters

`provider` [IMetadataProvider](Flowthru.Meta.Providers.IMetadataProvider.md)

The metadata provider to register

#### Returns

 [FlowthruMetadataBuilder](Flowthru.Meta.FlowthruMetadataBuilder.md)

This builder for fluent chaining

### <a id="Flowthru_Meta_FlowthruMetadataBuilder_WithAutoExport_System_Boolean_"></a> WithAutoExport\(bool\)

Enables or disables automatic metadata export during pipeline execution.

```csharp
public FlowthruMetadataBuilder WithAutoExport(bool enabled = true)
```

#### Parameters

`enabled` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

True to auto-export (default), false to require manual export

#### Returns

 [FlowthruMetadataBuilder](Flowthru.Meta.FlowthruMetadataBuilder.md)

This builder for fluent chaining

