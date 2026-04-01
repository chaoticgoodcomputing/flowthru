# <a id="Flowthru_Pipelines_DryRunOption"></a> Struct DryRunOption

Namespace: [Flowthru.Pipelines](Flowthru.Pipelines.md)  
Assembly: Flowthru.Core.dll  

Represents a dry-run configuration. Can be assigned from a <xref href="System.Boolean" data-throw-if-not-resolved="false"></xref>
or a <xref href="Flowthru.Pipelines.ValidationDepth" data-throw-if-not-resolved="false"></xref> value.

```csharp
public readonly struct DryRunOption
```

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

Assigning <code>true</code> enables a full dry run (all pre-flight checks, no execution).
Assigning <code>false</code> disables dry-run mode entirely.
Assigning a <xref href="Flowthru.Pipelines.ValidationDepth" data-throw-if-not-resolved="false"></xref> enables dry-run at the specified depth.

## Properties

### <a id="Flowthru_Pipelines_DryRunOption_Depth"></a> Depth

The validation depth applied when dry-run is enabled.

```csharp
public ValidationDepth Depth { get; }
```

#### Property Value

 [ValidationDepth](Flowthru.Pipelines.ValidationDepth.md)

### <a id="Flowthru_Pipelines_DryRunOption_Enabled"></a> Enabled

Whether dry-run mode is enabled.

```csharp
public bool Enabled { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Operators

### <a id="Flowthru_Pipelines_DryRunOption_op_Implicit_System_Boolean__Flowthru_Pipelines_DryRunOption"></a> implicit operator DryRunOption\(bool\)

Implicitly converts a <xref href="System.Boolean" data-throw-if-not-resolved="false"></xref> to a <xref href="Flowthru.Pipelines.DryRunOption" data-throw-if-not-resolved="false"></xref>.
<code>true</code> enables full dry-run; <code>false</code> disables it.

```csharp
public static implicit operator DryRunOption(bool value)
```

#### Parameters

`value` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Returns

 [DryRunOption](Flowthru.Pipelines.DryRunOption.md)

### <a id="Flowthru_Pipelines_DryRunOption_op_Implicit_Flowthru_Pipelines_ValidationDepth__Flowthru_Pipelines_DryRunOption"></a> implicit operator DryRunOption\(ValidationDepth\)

Implicitly converts a <xref href="Flowthru.Pipelines.ValidationDepth" data-throw-if-not-resolved="false"></xref> to a <xref href="Flowthru.Pipelines.DryRunOption" data-throw-if-not-resolved="false"></xref>,
enabling dry-run at the specified depth.

```csharp
public static implicit operator DryRunOption(ValidationDepth depth)
```

#### Parameters

`depth` [ValidationDepth](Flowthru.Pipelines.ValidationDepth.md)

#### Returns

 [DryRunOption](Flowthru.Pipelines.DryRunOption.md)

