# <a id="Flowthru_Steps_Factory_StepFactory"></a> Class StepFactory

Namespace: [Flowthru.Steps.Factory](Flowthru.Steps.Factory.md)  
Assembly: Flowthru.Core.dll  

Factory for creating step instances using TypeActivator.

```csharp
public static class StepFactory
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[StepFactory](Flowthru.Steps.Factory.StepFactory.md)

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
<strong>Design Pattern:</strong> Factory Pattern - provides a centralized location for
step instantiation logic.
</p>
<p>
This is a thin wrapper around TypeActivator, providing a domain-specific API for
creating steps. Could be extended in the future with:
- Step validation logic
- Pre/post-creation hooks
- Step decoration/wrapping
</p>

## Methods

### <a id="Flowthru_Steps_Factory_StepFactory_Create__1"></a> Create<TStep\>\(\)

Creates a new instance of the specified step type.

```csharp
public static TStep Create<TStep>() where TStep : new()
```

#### Returns

 TStep

A new step instance

#### Type Parameters

`TStep` 

The step type to instantiate

#### Remarks

<p>
<strong>Requirements:</strong>
- TStep must inherit from StepBase&lt;TInput, TOutput&gt;
- TStep must have a parameterless constructor
</p>
<p>
These requirements are enforced at compile-time via generic constraints in
FlowBuilder.AddStep methods.
</p>

