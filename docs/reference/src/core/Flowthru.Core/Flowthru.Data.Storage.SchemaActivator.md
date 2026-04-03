# <a id="Flowthru_Data_Storage_SchemaActivator"></a> Class SchemaActivator

Namespace: [Flowthru.Data.Storage](Flowthru.Data.Storage.md)  
Assembly: Flowthru.Core.dll  

Factory for creating schema instances, supporting both traditional parameterless constructors
and modern C# features like required members and positional records.

```csharp
public static class SchemaActivator
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[SchemaActivator](Flowthru.Data.Storage.SchemaActivator.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Examples

<pre><code class="lang-csharp">// Traditional schema - uses fast path
public record OldSchema(int Id, string Name) : IFlatSchema;
var old = SchemaActivator.CreateInstance&lt;OldSchema&gt;();

// Modern schema with required members - uses slow path
public record NewSchema : IFlatSchema
{
  public required Guid Id { get; init; }
  public required string Name { get; init; }
}
var modern = SchemaActivator.CreateInstance&lt;NewSchema&gt;();</code></pre>

## Remarks

<p>
<strong>Design Philosophy:</strong>
</p>
<p>
With Flowthru's strong node contracts, schemas with required members are guaranteed to contain
valid data because:
</p>
<ul><li><strong>Layer 0 (Seeds):</strong> Validation phase checks required fields exist before execution</li><li><strong>Layers 1+ (Node outputs):</strong> C# compiler enforces required members when nodes construct output</li></ul>
<p>
This activator's role is to enable deserialization by creating instances that will be populated
via property reflection. No validation is performed here - that happens at the pipeline boundaries.
</p>
<p>
<strong>Instantiation Strategy:</strong>
</p>
<ul><li><strong>Fast Path:</strong> Parameterless constructor (uses compiled expression tree)</li><li><strong>Slow Path:</strong> No parameterless constructor (uses FormatterServices.GetUninitializedObject)</li></ul>
<p>
<strong>Performance:</strong>
</p>
<p>
The fast path (parameterless constructor) is ~10x faster than Activator.CreateInstance.
The slow path (uninitialized object) is ~2x slower than Activator.CreateInstance but enables
required members and positional records.
</p>
<p>
Both paths cache metadata to minimize reflection overhead on subsequent calls.
</p>

## Methods

### <a id="Flowthru_Data_Storage_SchemaActivator_CreateInstance__1"></a> CreateInstance<T\>\(\)

Creates an instance of the specified type, automatically selecting the optimal
instantiation strategy.

```csharp
public static T CreateInstance<T>() where T : notnull
```

#### Returns

 T

A new instance of type T with uninitialized properties

#### Type Parameters

`T` 

The type to instantiate

#### Remarks

<p>
The returned instance will have:
- Reference type properties: null
- Value type properties: default values (0, false, etc.)
- Required members: uninitialized (will be set via reflection after this call)
</p>
<p>
This is safe because:
- Layer 0: Validation ensures required fields exist in data
- Layers 1+: Data came from valid node output
</p>

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

Thrown if the type cannot be instantiated (e.g., abstract class, interface)

