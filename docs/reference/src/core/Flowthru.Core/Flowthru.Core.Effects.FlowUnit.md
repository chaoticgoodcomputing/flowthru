# <a id="Flowthru_Core_Effects_FlowUnit"></a> Struct FlowUnit

Namespace: [Flowthru.Core.Effects](Flowthru.Core.Effects.md)  
Assembly: Flowthru.Core.dll  

Represents a void-like value for effect operations with no meaningful return value.
Similar to <code>Unit</code> in functional programming or <code>void</code> in imperative programming,
but usable as a type parameter.

```csharp
public readonly struct FlowUnit : IEquatable<FlowUnit>, IComparable<FlowUnit>
```

#### Implements

[IEquatable<FlowUnit\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1), 
[IComparable<FlowUnit\>](https://learn.microsoft.com/dotnet/api/system.icomparable\-1)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

<p>
Use <xref href="Flowthru.Core.Effects.FlowUnit" data-throw-if-not-resolved="false"></xref> when an effect produces side effects but no meaningful result.
For example, <code>FlowIO&lt;FlowUnit&gt;</code> represents an I/O operation that doesn't return data.
</p>
<p>
<strong>Example:</strong>
</p>
<pre><code class="lang-csharp">FlowIO&lt;FlowUnit&gt; WriteLog(string message) =&gt;
    FlowIO.LiftAsync(async ct =&gt; {
        await File.WriteAllTextAsync("log.txt", message, ct);
        return FlowFlowUnit.Default;
    });</code></pre>

## Fields

### <a id="Flowthru_Core_Effects_FlowUnit_Default"></a> Default

The single instance of <xref href="Flowthru.Core.Effects.FlowUnit" data-throw-if-not-resolved="false"></xref>.

```csharp
public static readonly FlowUnit Default
```

#### Field Value

 [FlowUnit](Flowthru.Core.Effects.FlowUnit.md)

## Methods

### <a id="Flowthru_Core_Effects_FlowUnit_CompareTo_Flowthru_Core_Effects_FlowUnit_"></a> CompareTo\(FlowUnit\)

Compares the current instance with another <xref href="Flowthru.Core.Effects.FlowUnit" data-throw-if-not-resolved="false"></xref>.

```csharp
[ExcludeFromCodeCoverage]
public int CompareTo(FlowUnit other)
```

#### Parameters

`other` [FlowUnit](Flowthru.Core.Effects.FlowUnit.md)

The <xref href="Flowthru.Core.Effects.FlowUnit" data-throw-if-not-resolved="false"></xref> to compare.

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

Always 0, as all FlowUnit instances are equal.

### <a id="Flowthru_Core_Effects_FlowUnit_Equals_Flowthru_Core_Effects_FlowUnit_"></a> Equals\(FlowUnit\)

Determines whether the specified <xref href="Flowthru.Core.Effects.FlowUnit" data-throw-if-not-resolved="false"></xref> is equal to the current instance.

```csharp
[ExcludeFromCodeCoverage]
public bool Equals(FlowUnit other)
```

#### Parameters

`other` [FlowUnit](Flowthru.Core.Effects.FlowUnit.md)

The <xref href="Flowthru.Core.Effects.FlowUnit" data-throw-if-not-resolved="false"></xref> to compare.

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Always <code>true</code>, as all FlowUnit instances are equal.

### <a id="Flowthru_Core_Effects_FlowUnit_Equals_System_Object_"></a> Equals\(object?\)

Determines whether the specified object is equal to the current instance.

```csharp
[ExcludeFromCodeCoverage]
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

The object to compare.

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

<code>true</code> if <code class="paramref">obj</code> is a <xref href="Flowthru.Core.Effects.FlowUnit" data-throw-if-not-resolved="false"></xref>; otherwise, <code>false</code>.

### <a id="Flowthru_Core_Effects_FlowUnit_GetHashCode"></a> GetHashCode\(\)

Returns the hash code for this instance.

```csharp
[ExcludeFromCodeCoverage]
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

Always returns 0, as all FlowUnit instances are equal.

## Operators

### <a id="Flowthru_Core_Effects_FlowUnit_op_Equality_Flowthru_Core_Effects_FlowUnit_Flowthru_Core_Effects_FlowUnit_"></a> operator ==\(FlowUnit, FlowUnit\)

Determines whether two <xref href="Flowthru.Core.Effects.FlowUnit" data-throw-if-not-resolved="false"></xref> instances are equal.

```csharp
[ExcludeFromCodeCoverage]
public static bool operator ==(FlowUnit left, FlowUnit right)
```

#### Parameters

`left` [FlowUnit](Flowthru.Core.Effects.FlowUnit.md)

The first instance to compare.

`right` [FlowUnit](Flowthru.Core.Effects.FlowUnit.md)

The second instance to compare.

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Always <code>true</code>.

### <a id="Flowthru_Core_Effects_FlowUnit_op_GreaterThan_Flowthru_Core_Effects_FlowUnit_Flowthru_Core_Effects_FlowUnit_"></a> operator \>\(FlowUnit, FlowUnit\)

Compares two <xref href="Flowthru.Core.Effects.FlowUnit" data-throw-if-not-resolved="false"></xref> instances.

```csharp
[ExcludeFromCodeCoverage]
public static bool operator >(FlowUnit left, FlowUnit right)
```

#### Parameters

`left` [FlowUnit](Flowthru.Core.Effects.FlowUnit.md)

The first instance to compare.

`right` [FlowUnit](Flowthru.Core.Effects.FlowUnit.md)

The second instance to compare.

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Always <code>false</code>.

### <a id="Flowthru_Core_Effects_FlowUnit_op_GreaterThanOrEqual_Flowthru_Core_Effects_FlowUnit_Flowthru_Core_Effects_FlowUnit_"></a> operator \>=\(FlowUnit, FlowUnit\)

Compares two <xref href="Flowthru.Core.Effects.FlowUnit" data-throw-if-not-resolved="false"></xref> instances.

```csharp
[ExcludeFromCodeCoverage]
public static bool operator >=(FlowUnit left, FlowUnit right)
```

#### Parameters

`left` [FlowUnit](Flowthru.Core.Effects.FlowUnit.md)

The first instance to compare.

`right` [FlowUnit](Flowthru.Core.Effects.FlowUnit.md)

The second instance to compare.

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Always <code>true</code>.

### <a id="Flowthru_Core_Effects_FlowUnit_op_Inequality_Flowthru_Core_Effects_FlowUnit_Flowthru_Core_Effects_FlowUnit_"></a> operator \!=\(FlowUnit, FlowUnit\)

Determines whether two <xref href="Flowthru.Core.Effects.FlowUnit" data-throw-if-not-resolved="false"></xref> instances are not equal.

```csharp
[ExcludeFromCodeCoverage]
public static bool operator !=(FlowUnit left, FlowUnit right)
```

#### Parameters

`left` [FlowUnit](Flowthru.Core.Effects.FlowUnit.md)

The first instance to compare.

`right` [FlowUnit](Flowthru.Core.Effects.FlowUnit.md)

The second instance to compare.

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Always <code>false</code>.

### <a id="Flowthru_Core_Effects_FlowUnit_op_LessThan_Flowthru_Core_Effects_FlowUnit_Flowthru_Core_Effects_FlowUnit_"></a> operator <\(FlowUnit, FlowUnit\)

Compares two <xref href="Flowthru.Core.Effects.FlowUnit" data-throw-if-not-resolved="false"></xref> instances.

```csharp
[ExcludeFromCodeCoverage]
public static bool operator <(FlowUnit left, FlowUnit right)
```

#### Parameters

`left` [FlowUnit](Flowthru.Core.Effects.FlowUnit.md)

The first instance to compare.

`right` [FlowUnit](Flowthru.Core.Effects.FlowUnit.md)

The second instance to compare.

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Always <code>false</code>.

### <a id="Flowthru_Core_Effects_FlowUnit_op_LessThanOrEqual_Flowthru_Core_Effects_FlowUnit_Flowthru_Core_Effects_FlowUnit_"></a> operator <=\(FlowUnit, FlowUnit\)

Compares two <xref href="Flowthru.Core.Effects.FlowUnit" data-throw-if-not-resolved="false"></xref> instances.

```csharp
[ExcludeFromCodeCoverage]
public static bool operator <=(FlowUnit left, FlowUnit right)
```

#### Parameters

`left` [FlowUnit](Flowthru.Core.Effects.FlowUnit.md)

The first instance to compare.

`right` [FlowUnit](Flowthru.Core.Effects.FlowUnit.md)

The second instance to compare.

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Always <code>true</code>.

