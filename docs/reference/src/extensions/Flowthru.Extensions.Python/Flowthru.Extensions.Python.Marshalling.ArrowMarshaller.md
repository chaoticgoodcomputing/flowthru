# <a id="Flowthru_Extensions_Python_Marshalling_ArrowMarshaller"></a> Class ArrowMarshaller

Namespace: [Flowthru.Extensions.Python.Marshalling](Flowthru.Extensions.Python.Marshalling.md)  
Assembly: Flowthru.Extensions.Python.dll  

Marshals tabular data between C# IEnumerable&lt;T&gt; and Apache Arrow RecordBatch.

```csharp
public static class ArrowMarshaller
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ArrowMarshaller](Flowthru.Extensions.Python.Marshalling.ArrowMarshaller.md)

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
<strong>Purpose:</strong> Bidirectional conversion for DataFrame interchange between
C# and Python via Arrow IPC (Inter-Process Communication) format.
</p>
<p>
<strong>C# → Arrow Flow:</strong>
</p>
<pre><code class="lang-csharp">IEnumerable&lt;T&gt; → RecordBatch → IPC buffer → Python pyarrow.Table → pd.DataFrame</code></pre>
<p>
<strong>Arrow → C# Flow:</strong>
</p>
<pre><code class="lang-csharp">pd.DataFrame → pyarrow.Table → IPC buffer → RecordBatch → IEnumerable&lt;T&gt;</code></pre>
<p>
<strong>Performance:</strong> Uses columnar processing (column-by-column, not row-by-row)
for efficient Arrow array construction.
</p>

## Methods

### <a id="Flowthru_Extensions_Python_Marshalling_ArrowMarshaller_FromIpcBuffer_System_Byte___"></a> FromIpcBuffer\(byte\[\]\)

Deserializes an Arrow IPC buffer (from Python) to a RecordBatch.

```csharp
public static RecordBatch FromIpcBuffer(byte[] buffer)
```

#### Parameters

`buffer` [byte](https://learn.microsoft.com/dotnet/api/system.byte)\[\]

Byte array containing Arrow IPC stream

#### Returns

 RecordBatch

Arrow RecordBatch

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

Thrown when buffer is null

 [InvalidDataException](https://learn.microsoft.com/dotnet/api/system.io.invaliddataexception)

Thrown when buffer is not a valid Arrow IPC stream.

### <a id="Flowthru_Extensions_Python_Marshalling_ArrowMarshaller_FromRecordBatch__1_Apache_Arrow_RecordBatch_"></a> FromRecordBatch<T\>\(RecordBatch\)

Converts an Arrow RecordBatch to an IEnumerable of C# objects.

```csharp
public static IEnumerable<T> FromRecordBatch<T>(RecordBatch batch) where T : notnull
```

#### Parameters

`batch` RecordBatch

The Arrow RecordBatch to convert

#### Returns

 [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<T\>

IEnumerable of C# objects

#### Type Parameters

`T` 

The C# schema type

#### Remarks

Uses SchemaActivator for instantiation to support required members.
Data is converted row-by-row (column values → object properties).

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

Thrown when batch is null

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

Thrown when schema mismatch or type conversion fails.

### <a id="Flowthru_Extensions_Python_Marshalling_ArrowMarshaller_ToIpcBuffer_Apache_Arrow_RecordBatch_"></a> ToIpcBuffer\(RecordBatch\)

Serializes an Arrow RecordBatch to an IPC buffer for Python.NET transfer.

```csharp
public static byte[] ToIpcBuffer(RecordBatch batch)
```

#### Parameters

`batch` RecordBatch

The RecordBatch to serialize

#### Returns

 [byte](https://learn.microsoft.com/dotnet/api/system.byte)\[\]

Byte array containing Arrow IPC stream

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

Thrown when batch is null

### <a id="Flowthru_Extensions_Python_Marshalling_ArrowMarshaller_ToRecordBatch__1_System_Collections_Generic_IEnumerable___0__"></a> ToRecordBatch<T\>\(IEnumerable<T\>\)

Converts an IEnumerable of C# objects to an Arrow RecordBatch.

```csharp
public static RecordBatch ToRecordBatch<T>(IEnumerable<T> rows) where T : notnull
```

#### Parameters

`rows` [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<T\>

The rows to convert

#### Returns

 RecordBatch

Arrow RecordBatch containing the data

#### Type Parameters

`T` 

The C# schema type

#### Remarks

Data is processed column-wise for efficiency. All rows are materialized into memory
to build the RecordBatch.

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

Thrown when rows is null

 [NotSupportedException](https://learn.microsoft.com/dotnet/api/system.notsupportedexception)

Thrown when a property type cannot be marshalled to Arrow.

