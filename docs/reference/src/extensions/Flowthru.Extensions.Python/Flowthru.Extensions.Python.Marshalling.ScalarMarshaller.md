# <a id="Flowthru_Extensions_Python_Marshalling_ScalarMarshaller"></a> Class ScalarMarshaller

Namespace: [Flowthru.Extensions.Python.Marshalling](Flowthru.Extensions.Python.Marshalling.md)  
Assembly: Flowthru.Extensions.Python.dll  

Handles bidirectional conversion between C# scalar values and Python objects.

```csharp
public static class ScalarMarshaller
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ScalarMarshaller](Flowthru.Extensions.Python.Marshalling.ScalarMarshaller.md)

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
Supports:
<ul><li>Primitives: int, long, double, float, string, bool</li><li>Nullable primitives: int?, double?, etc.</li><li>Simple records and classes via property iteration</li></ul>
</p>
<p>
<strong>Supported since Phase 5:</strong>
<ul><li>Arrays: T[] (converted to/from Python lists)</li><li>Nested records (via recursive marshalling)</li></ul>
</p>
<p>
<strong>Not supported:</strong>
<ul><li>Generic collections: IEnumerable&lt;T&gt;, List&lt;T&gt; (use arrays or tabular I/O)</li><li>Arrow/DataFrame interchange (use tabular I/O path)</li></ul>
</p>
<p>
<strong>Thread-safety:</strong> All methods are thread-safe.
Caller is responsible for GIL acquisition.
</p>

## Methods

### <a id="Flowthru_Extensions_Python_Marshalling_ScalarMarshaller_FromPython__1_Python_Runtime_PyObject_"></a> FromPython<T\>\(PyObject\)

Converts a Python object to a C# value of type T.

```csharp
public static T FromPython<T>(PyObject pyObject)
```

#### Parameters

`pyObject` PyObject

Python object to convert.

#### Returns

 T

C# value of type T.

#### Type Parameters

`T` 

Target C# type.

#### Remarks

Must be called within a GIL-acquired context.

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

Thrown when conversion fails or the type is not supported.

### <a id="Flowthru_Extensions_Python_Marshalling_ScalarMarshaller_ToPython_System_Object_"></a> ToPython\(object?\)

Converts a C# value to a Python object.

```csharp
public static PyObject ToPython(object? value)
```

#### Parameters

`value` [object](https://learn.microsoft.com/dotnet/api/system.object)?

C# value to convert. May be null.

#### Returns

 PyObject

Python object representation of the value.

#### Remarks

Must be called within a GIL-acquired context.

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

Thrown when the value type is not supported for marshalling.

