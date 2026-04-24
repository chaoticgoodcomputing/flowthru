# <a id="Flowthru_Core_Meta_FilenameTemplateParser"></a> Class FilenameTemplateParser

Namespace: [Flowthru.Core.Meta](Flowthru.Core.Meta.md)  
Assembly: Flowthru.Core.dll  

Renders filename templates with dynamic token replacement.

```csharp
public static class FilenameTemplateParser
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[FilenameTemplateParser](Flowthru.Core.Meta.FilenameTemplateParser.md)

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
Supports tokens for pipeline metadata and slice criteria:
</p>
<ul><li><code>{FlowName}</code> - Sanitized pipeline name</li><li><code>{Timestamp}</code> - Formatted timestamp (empty if disabled)</li><li><code>{SliceType}</code> - Slice descriptor: "Flow", "Flows", "From", "To", "Only", "ComposedSlice", or empty if unsliced</li><li><code>{Flows}</code> - Comma-separated list of flow names</li><li><code>{From}</code> - Comma-separated list of from labels</li><li><code>{To}</code> - Comma-separated list of to labels</li><li><code>{Only}</code> - Comma-separated list of only labels</li></ul>
<p>
<strong>Empty Token Collapsing:</strong> Consecutive separators (hyphens, underscores)
around empty tokens are collapsed to prevent patterns like <code>file--name</code> or
<code>file-.ext</code> when slice data is absent.
</p>
<p>
<strong>Example:</strong>
</p>
<pre><code class="lang-csharp">Template: "dag-{FlowName}-{Timestamp}-{SliceType}"
Unsliced: "dag-DataProcessing-20260304-153045"
Sliced:   "dag-DataProcessing-20260304-153045-From"</code></pre>

## Methods

### <a id="Flowthru_Core_Meta_FilenameTemplateParser_Render_Flowthru_Core_Graph_Meta_Models_DagMetadata_System_String_System_String_"></a> Render\(DagMetadata, string, string?\)

Renders a filename template by replacing tokens with values from the DAG metadata.

```csharp
public static string Render(DagMetadata dag, string template, string? timestamp)
```

#### Parameters

`dag` [DagMetadata](Flowthru.Core.Graph.Meta.Models.DagMetadata.md)

DAG metadata containing pipeline and slice information

`template` [string](https://learn.microsoft.com/dotnet/api/system.string)

Template string with {Token} placeholders

`timestamp` [string](https://learn.microsoft.com/dotnet/api/system.string)?

Optional timestamp string (empty if timestamp disabled)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

Rendered filename with tokens replaced and empty segments collapsed

