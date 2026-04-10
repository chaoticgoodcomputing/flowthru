# <a id="Flowthru_Extensions_MLNet_ItemFactoryMLNet"></a> Class ItemFactoryMLNet

Namespace: [Flowthru.Extensions.MLNet](Flowthru.Extensions.MLNet.md)  
Assembly: Flowthru.Extensions.MLNet.dll  

Factory methods for creating ML.NET-related catalog entries.

```csharp
public static class ItemFactoryMLNet
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ItemFactoryMLNet](Flowthru.Extensions.MLNet.ItemFactoryMLNet.md)

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
<strong>Discovery Pattern:</strong> Import this class alongside <xref href="Flowthru.Core.Data.ItemFactory" data-throw-if-not-resolved="false"></xref>
for ML.NET-specific catalog entry factory methods.
</p>
<p>
<strong>Use Cases:</strong>
</p>
<ul><li>Loading pre-trained ONNX models for inference</li><li>Working with ML.NET IDataView data structures</li></ul>
<p>
<strong>Usage:</strong>
</p>
<pre><code class="lang-csharp">using Flowthru.Core.Data;
using Flowthru.Extensions.MLNet;

// Core entries
var csvEntry = ItemFactory.Enumerable.Csv&lt;MySchema&gt;("data", "data.csv");

// MLNet entries
var modelEntry = ItemFactoryMLNet.OnnxModel("model", "model.onnx");</code></pre>

## Methods

### <a id="Flowthru_Extensions_MLNet_ItemFactoryMLNet_OnnxModel_System_String_System_String_"></a> OnnxModel\(string, string\)

Creates a catalog entry for an ONNX model file.

```csharp
public static IItem<byte[]> OnnxModel(string label, string filePath)
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Human-readable label for the catalog entry

`filePath` [string](https://learn.microsoft.com/dotnet/api/system.string)

Path to the .onnx model file

#### Returns

 IItem<[byte](https://learn.microsoft.com/dotnet/api/system.byte)\[\]\>

A catalog entry wrapping an ONNX model storage adapter

#### Examples

<pre><code class="lang-csharp">var entry = ItemFactoryMLNet.OnnxModel(
    label: "BertModel",
    filePath: "models/bert-base.onnx"
);</code></pre>

