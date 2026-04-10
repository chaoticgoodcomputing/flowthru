# <a id="Flowthru_FUnit_Samples_SampleBuilder"></a> Class SampleBuilder

Namespace: [Flowthru.FUnit.Samples](Flowthru.FUnit.Samples.md)  
Assembly: Flowthru.FUnit.dll  

Helpers for constructing typed sample data in step and effect tests.

```csharp
public class SampleBuilder
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[SampleBuilder](Flowthru.FUnit.Samples.SampleBuilder.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Methods

### <a id="Flowthru_FUnit_Samples_SampleBuilder_FromCsv__1_System_String_"></a> FromCsv<T\>\(string\)

Loads CSV rows from an embedded resource in the calling assembly.
The resource must be a valid CSV file whose columns match
the public properties of <code class="typeparamref">T</code>.

```csharp
public IEnumerable<T> FromCsv<T>(string resourcePath) where T : new()
```

#### Parameters

`resourcePath` [string](https://learn.microsoft.com/dotnet/api/system.string)

The fully-qualified embedded resource name
(e.g. <code>"MyTests.Data.sample.csv"</code>).

#### Returns

 [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<T\>

#### Type Parameters

`T` 

Target row type. Must have a parameterless constructor.

### <a id="Flowthru_FUnit_Samples_SampleBuilder_Generate__1_System_Int32_System_Func_System_Int32___0__"></a> Generate<T\>\(int, Func<int, T\>\)

Generates <code class="paramref">count</code> rows using a factory function
that receives the zero-based row index.

```csharp
public IEnumerable<T> Generate<T>(int count, Func<int, T> factory)
```

#### Parameters

`count` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`factory` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[int](https://learn.microsoft.com/dotnet/api/system.int32), T\>

#### Returns

 [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<T\>

#### Type Parameters

`T` 

### <a id="Flowthru_FUnit_Samples_SampleBuilder_Of__1___0___"></a> Of<T\>\(params T\[\]\)

Wraps explicit instances into an <xref href="System.Collections.Generic.IEnumerable%601" data-throw-if-not-resolved="false"></xref>.

```csharp
public IEnumerable<T> Of<T>(params T[] items)
```

#### Parameters

`items` T\[\]

#### Returns

 [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<T\>

#### Type Parameters

`T` 

