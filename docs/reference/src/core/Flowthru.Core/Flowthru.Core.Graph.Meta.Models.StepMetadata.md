# <a id="Flowthru_Core_Graph_Meta_Models_StepMetadata"></a> Class StepMetadata

Namespace: [Flowthru.Core.Graph.Meta.Models](Flowthru.Core.Graph.Meta.Models.md)  
Assembly: Flowthru.Core.dll  

Metadata describing a single step in the Flow DAG.

```csharp
public class StepMetadata
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[StepMetadata](Flowthru.Core.Graph.Meta.Models.StepMetadata.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

Steps are the processing units in a flow. Each step reads from one or more
catalog entries (inputs), performs a transformation, and writes to one or more
catalog entries (outputs).

## Properties

### <a id="Flowthru_Core_Graph_Meta_Models_StepMetadata_FlowName"></a> FlowName

Name of the parent Flow this step belongs to.

```csharp
[JsonPropertyName("flowName")]
public required string FlowName { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Remarks

Important for merged flows where steps from multiple flows
are combined into a single DAG.

### <a id="Flowthru_Core_Graph_Meta_Models_StepMetadata_Id"></a> Id

Unique identifier for this step within the flow.

```csharp
[JsonPropertyName("id")]
public required string Id { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Remarks

Typically the step name as defined when adding it to the flow.
Example: "PreprocessCompanies", "TrainModel"

### <a id="Flowthru_Core_Graph_Meta_Models_StepMetadata_Inputs"></a> Inputs

List of catalog entry keys this step reads from.

```csharp
[JsonPropertyName("inputs")]
public List<string> Inputs { get; init; }
```

#### Property Value

 [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

#### Remarks

For multi-input steps using CatalogMap, this contains all mapped entries.
Example: ["Companies", "Shuttles", "Reviews"]

### <a id="Flowthru_Core_Graph_Meta_Models_StepMetadata_Label"></a> Label

Human-readable display label for this step.

```csharp
[JsonPropertyName("label")]
public required string Label { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Remarks

May be formatted for better display in Flowthru.Core.Viz.
Example: "Preprocess Companies", "Train Model"

### <a id="Flowthru_Core_Graph_Meta_Models_StepMetadata_Layer"></a> Layer

Execution layer assigned by the dependency analyzer.

```csharp
[JsonPropertyName("layer")]
public int Layer { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Remarks

Layer 0 steps have no dependencies (read external data only).
Layer N steps depend only on steps in layers 0..N-1.

### <a id="Flowthru_Core_Graph_Meta_Models_StepMetadata_Outputs"></a> Outputs

List of catalog entry keys this step writes to.

```csharp
[JsonPropertyName("outputs")]
public List<string> Outputs { get; init; }
```

#### Property Value

 [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

#### Remarks

For multi-output steps using CatalogMap, this contains all mapped entries.
Example: ["XTrain", "XTest", "YTrain", "YTest"]

### <a id="Flowthru_Core_Graph_Meta_Models_StepMetadata_StepType"></a> StepType

The C# class type name implementing this step.

```csharp
[JsonPropertyName("stepType")]
public required string StepType { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Remarks

Simple type name without namespace or generic parameters.
Example: "PreprocessCompaniesStep", "TrainModelStep"

### <a id="Flowthru_Core_Graph_Meta_Models_StepMetadata_TestCount"></a> TestCount

Number of <code>[StepTest]</code> methods registered against this step's type.
Only populated when <code>Flowthru.FUnit</code> is referenced and the
<code>StepTestRegistry</code> is present in the loaded assemblies.
<code>null</code> when FUnit is absent.

```csharp
[JsonPropertyName("testCount")]
[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
public int? TestCount { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)?

