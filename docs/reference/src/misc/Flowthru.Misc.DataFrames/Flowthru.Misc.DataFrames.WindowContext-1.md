# <a id="Flowthru_Misc_DataFrames_WindowContext_1"></a> Class WindowContext<TSource\>

Namespace: [Flowthru.Misc.DataFrames](Flowthru.Misc.DataFrames.md)  
Assembly: Flowthru.Misc.DataFrames.dll  

A throw-only marker type whose methods are intercepted as expression tree nodes
inside a <xref href="Flowthru.Misc.DataFrames.TypedFrameExtensions.SelectOver%60%602(Flowthru.Misc.DataFrames.TypedFrame%7b%60%600%7d%2cSystem.Linq.Expressions.Expression%7bSystem.Func%7b%60%600%2cFlowthru.Misc.DataFrames.WindowContext%7b%60%600%7d%2c%60%601%7d%7d)" data-throw-if-not-resolved="false"></xref> projection.

```csharp
public sealed class WindowContext<TSource>
```

#### Type Parameters

`TSource` 

The row schema type of the source frame.

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[WindowContext<TSource\>](Flowthru.Misc.DataFrames.WindowContext\-1.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

<p>
Instances of this type are never constructed at runtime. The provider's expression
visitor recognises method calls on the <code>win</code> parameter and translates them to
the corresponding native window functions (e.g., Spark's
<code>Functions.Rank().Over(windowSpec)</code>).
</p>
<p>
Each method accepts a <xref href="Flowthru.Misc.DataFrames.FrameWindowSpec%601" data-throw-if-not-resolved="false"></xref> as its last argument.
This makes multi-window projections natural — different columns can reference different
specs in the same <code>SelectOver</code> call.
</p>

## Methods

### <a id="Flowthru_Misc_DataFrames_WindowContext_1_Avg_System_Linq_Expressions_Expression_System_Func__0_System_Double___Flowthru_Misc_DataFrames_FrameWindowSpec__0__"></a> Avg\(Expression<Func<TSource, double\>\>, FrameWindowSpec<TSource\>\)

Running average of <code class="paramref">selector</code> over the window frame.

```csharp
public double Avg(Expression<Func<TSource, double>> selector, FrameWindowSpec<TSource> spec)
```

#### Parameters

`selector` [Expression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.expression\-1)<[Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TSource, [double](https://learn.microsoft.com/dotnet/api/system.double)\>\>

`spec` [FrameWindowSpec](Flowthru.Misc.DataFrames.FrameWindowSpec\-1.md)<TSource\>

#### Returns

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="Flowthru_Misc_DataFrames_WindowContext_1_Avg_System_Linq_Expressions_Expression_System_Func__0_System_Int32___Flowthru_Misc_DataFrames_FrameWindowSpec__0__"></a> Avg\(Expression<Func<TSource, int\>\>, FrameWindowSpec<TSource\>\)

Running average of <code class="paramref">selector</code> over the window frame.

```csharp
public double Avg(Expression<Func<TSource, int>> selector, FrameWindowSpec<TSource> spec)
```

#### Parameters

`selector` [Expression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.expression\-1)<[Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TSource, [int](https://learn.microsoft.com/dotnet/api/system.int32)\>\>

`spec` [FrameWindowSpec](Flowthru.Misc.DataFrames.FrameWindowSpec\-1.md)<TSource\>

#### Returns

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="Flowthru_Misc_DataFrames_WindowContext_1_Count_Flowthru_Misc_DataFrames_FrameWindowSpec__0__"></a> Count\(FrameWindowSpec<TSource\>\)

Count of rows seen so far within the window frame.

```csharp
public long Count(FrameWindowSpec<TSource> spec)
```

#### Parameters

`spec` [FrameWindowSpec](Flowthru.Misc.DataFrames.FrameWindowSpec\-1.md)<TSource\>

#### Returns

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="Flowthru_Misc_DataFrames_WindowContext_1_CumeDist_Flowthru_Misc_DataFrames_FrameWindowSpec__0__"></a> CumeDist\(FrameWindowSpec<TSource\>\)

Fraction of rows within the partition that are less than or equal to the current row.

```csharp
public double CumeDist(FrameWindowSpec<TSource> spec)
```

#### Parameters

`spec` [FrameWindowSpec](Flowthru.Misc.DataFrames.FrameWindowSpec\-1.md)<TSource\>

#### Returns

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="Flowthru_Misc_DataFrames_WindowContext_1_DenseRank_Flowthru_Misc_DataFrames_FrameWindowSpec__0__"></a> DenseRank\(FrameWindowSpec<TSource\>\)

Rank without gaps (ties share a rank; next rank is always rank + 1).

```csharp
public long DenseRank(FrameWindowSpec<TSource> spec)
```

#### Parameters

`spec` [FrameWindowSpec](Flowthru.Misc.DataFrames.FrameWindowSpec\-1.md)<TSource\>

#### Returns

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="Flowthru_Misc_DataFrames_WindowContext_1_Lag__1_System_Linq_Expressions_Expression_System_Func__0___0___System_Int32_Flowthru_Misc_DataFrames_FrameWindowSpec__0__"></a> Lag<TValue\>\(Expression<Func<TSource, TValue\>\>, int, FrameWindowSpec<TSource\>\)

Value of <code class="paramref">selector</code> from the row <code class="paramref">offset</code> rows before
the current row, or <code>null</code> if no such row exists.

```csharp
public TValue? Lag<TValue>(Expression<Func<TSource, TValue>> selector, int offset, FrameWindowSpec<TSource> spec)
```

#### Parameters

`selector` [Expression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.expression\-1)<[Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TSource, TValue\>\>

`offset` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`spec` [FrameWindowSpec](Flowthru.Misc.DataFrames.FrameWindowSpec\-1.md)<TSource\>

#### Returns

 TValue?

#### Type Parameters

`TValue` 

### <a id="Flowthru_Misc_DataFrames_WindowContext_1_Lead__1_System_Linq_Expressions_Expression_System_Func__0___0___System_Int32_Flowthru_Misc_DataFrames_FrameWindowSpec__0__"></a> Lead<TValue\>\(Expression<Func<TSource, TValue\>\>, int, FrameWindowSpec<TSource\>\)

Value of <code class="paramref">selector</code> from the row <code class="paramref">offset</code> rows after
the current row, or <code>null</code> if no such row exists.

```csharp
public TValue? Lead<TValue>(Expression<Func<TSource, TValue>> selector, int offset, FrameWindowSpec<TSource> spec)
```

#### Parameters

`selector` [Expression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.expression\-1)<[Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TSource, TValue\>\>

`offset` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`spec` [FrameWindowSpec](Flowthru.Misc.DataFrames.FrameWindowSpec\-1.md)<TSource\>

#### Returns

 TValue?

#### Type Parameters

`TValue` 

### <a id="Flowthru_Misc_DataFrames_WindowContext_1_Max__1_System_Linq_Expressions_Expression_System_Func__0___0___Flowthru_Misc_DataFrames_FrameWindowSpec__0__"></a> Max<TValue\>\(Expression<Func<TSource, TValue\>\>, FrameWindowSpec<TSource\>\)

Running maximum of <code class="paramref">selector</code> over the window frame.

```csharp
public TValue Max<TValue>(Expression<Func<TSource, TValue>> selector, FrameWindowSpec<TSource> spec)
```

#### Parameters

`selector` [Expression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.expression\-1)<[Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TSource, TValue\>\>

`spec` [FrameWindowSpec](Flowthru.Misc.DataFrames.FrameWindowSpec\-1.md)<TSource\>

#### Returns

 TValue

#### Type Parameters

`TValue` 

### <a id="Flowthru_Misc_DataFrames_WindowContext_1_Min__1_System_Linq_Expressions_Expression_System_Func__0___0___Flowthru_Misc_DataFrames_FrameWindowSpec__0__"></a> Min<TValue\>\(Expression<Func<TSource, TValue\>\>, FrameWindowSpec<TSource\>\)

Running minimum of <code class="paramref">selector</code> over the window frame.

```csharp
public TValue Min<TValue>(Expression<Func<TSource, TValue>> selector, FrameWindowSpec<TSource> spec)
```

#### Parameters

`selector` [Expression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.expression\-1)<[Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TSource, TValue\>\>

`spec` [FrameWindowSpec](Flowthru.Misc.DataFrames.FrameWindowSpec\-1.md)<TSource\>

#### Returns

 TValue

#### Type Parameters

`TValue` 

### <a id="Flowthru_Misc_DataFrames_WindowContext_1_PercentRank_Flowthru_Misc_DataFrames_FrameWindowSpec__0__"></a> PercentRank\(FrameWindowSpec<TSource\>\)

Relative rank of the current row: (rank - 1) / (partition size - 1).

```csharp
public double PercentRank(FrameWindowSpec<TSource> spec)
```

#### Parameters

`spec` [FrameWindowSpec](Flowthru.Misc.DataFrames.FrameWindowSpec\-1.md)<TSource\>

#### Returns

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="Flowthru_Misc_DataFrames_WindowContext_1_Rank_Flowthru_Misc_DataFrames_FrameWindowSpec__0__"></a> Rank\(FrameWindowSpec<TSource\>\)

Rank with gaps (ties share a rank; the next rank reflects the gap).

```csharp
public long Rank(FrameWindowSpec<TSource> spec)
```

#### Parameters

`spec` [FrameWindowSpec](Flowthru.Misc.DataFrames.FrameWindowSpec\-1.md)<TSource\>

#### Returns

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="Flowthru_Misc_DataFrames_WindowContext_1_RowNumber_Flowthru_Misc_DataFrames_FrameWindowSpec__0__"></a> RowNumber\(FrameWindowSpec<TSource\>\)

Sequential row number within the partition, starting at 1.

```csharp
public long RowNumber(FrameWindowSpec<TSource> spec)
```

#### Parameters

`spec` [FrameWindowSpec](Flowthru.Misc.DataFrames.FrameWindowSpec\-1.md)<TSource\>

#### Returns

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="Flowthru_Misc_DataFrames_WindowContext_1_Sum_System_Linq_Expressions_Expression_System_Func__0_System_Double___Flowthru_Misc_DataFrames_FrameWindowSpec__0__"></a> Sum\(Expression<Func<TSource, double\>\>, FrameWindowSpec<TSource\>\)

Running sum of <code class="paramref">selector</code> over the window frame.

```csharp
public double Sum(Expression<Func<TSource, double>> selector, FrameWindowSpec<TSource> spec)
```

#### Parameters

`selector` [Expression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.expression\-1)<[Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TSource, [double](https://learn.microsoft.com/dotnet/api/system.double)\>\>

`spec` [FrameWindowSpec](Flowthru.Misc.DataFrames.FrameWindowSpec\-1.md)<TSource\>

#### Returns

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="Flowthru_Misc_DataFrames_WindowContext_1_Sum_System_Linq_Expressions_Expression_System_Func__0_System_Decimal___Flowthru_Misc_DataFrames_FrameWindowSpec__0__"></a> Sum\(Expression<Func<TSource, decimal\>\>, FrameWindowSpec<TSource\>\)

Running sum of <code class="paramref">selector</code> over the window frame.

```csharp
public decimal Sum(Expression<Func<TSource, decimal>> selector, FrameWindowSpec<TSource> spec)
```

#### Parameters

`selector` [Expression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.expression\-1)<[Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TSource, [decimal](https://learn.microsoft.com/dotnet/api/system.decimal)\>\>

`spec` [FrameWindowSpec](Flowthru.Misc.DataFrames.FrameWindowSpec\-1.md)<TSource\>

#### Returns

 [decimal](https://learn.microsoft.com/dotnet/api/system.decimal)

### <a id="Flowthru_Misc_DataFrames_WindowContext_1_Sum_System_Linq_Expressions_Expression_System_Func__0_System_Int32___Flowthru_Misc_DataFrames_FrameWindowSpec__0__"></a> Sum\(Expression<Func<TSource, int\>\>, FrameWindowSpec<TSource\>\)

Running sum of <code class="paramref">selector</code> over the window frame.

```csharp
public long Sum(Expression<Func<TSource, int>> selector, FrameWindowSpec<TSource> spec)
```

#### Parameters

`selector` [Expression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.expression\-1)<[Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TSource, [int](https://learn.microsoft.com/dotnet/api/system.int32)\>\>

`spec` [FrameWindowSpec](Flowthru.Misc.DataFrames.FrameWindowSpec\-1.md)<TSource\>

#### Returns

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

