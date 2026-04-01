# <a id="Flowthru_Nodes_Factory_TypeActivator"></a> Class TypeActivator

Namespace: [Flowthru.Nodes.Factory](Flowthru.Nodes.Factory.md)  
Assembly: Flowthru.Core.dll  

Factory for creating instances of types using compiled expression trees for performance.

```csharp
public static class TypeActivator
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[TypeActivator](Flowthru.Nodes.Factory.TypeActivator.md)

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
<strong>Design Pattern:</strong> Factory Pattern with caching - creates instances of types
using reflection on first call, then caches compiled expression trees for subsequent calls.
</p>
<p>
<strong>Performance:</strong>
- First call uses Expression.Compile() which has overhead
- Subsequent calls use cached delegate which is nearly as fast as `new T()`
- Significantly faster than Activator.CreateInstance&lt;T&gt;() for repeated calls
</p>
<p>
<strong>Inspiration:</strong> ChainSharp uses similar pattern for node instantiation.
</p>
<p>
<strong>Thread Safety:</strong> This class is thread-safe. Multiple threads can safely
call Create&lt;T&gt;() concurrently.
</p>

## Properties

### <a id="Flowthru_Nodes_Factory_TypeActivator_CacheCount"></a> CacheCount

Gets the number of cached factory functions.

```csharp
public static int CacheCount { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="Flowthru_Nodes_Factory_TypeActivator_ClearCache"></a> ClearCache\(\)

Clears the factory cache.

```csharp
public static void ClearCache()
```

#### Remarks

Useful for testing or memory management in long-running applications
that dynamically load/unload types.

### <a id="Flowthru_Nodes_Factory_TypeActivator_Create__1"></a> Create<T\>\(\)

Creates an instance of type <code class="typeparamref">T</code> using a cached factory.

```csharp
public static T Create<T>() where T : new()
```

#### Returns

 T

A new instance of type T

#### Type Parameters

`T` 

The type to instantiate (must have parameterless constructor)

#### Remarks

<p>
<strong>Compile-Time Safety:</strong> The `new()` constraint ensures that T has a
parameterless constructor. This is enforced at compile-time by the C# compiler.
</p>
<p>
<strong>Caching Strategy:</strong>
- First call: Compiles an expression tree and caches the resulting delegate
- Subsequent calls: Reuses the cached delegate
- One cache entry per type T
</p>

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

Thrown if type T does not have a parameterless constructor

