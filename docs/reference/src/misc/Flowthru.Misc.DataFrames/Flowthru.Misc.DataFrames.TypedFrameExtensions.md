# <a id="Flowthru_Misc_DataFrames_TypedFrameExtensions"></a> Class TypedFrameExtensions

Namespace: [Flowthru.Misc.DataFrames](Flowthru.Misc.DataFrames.md)  
Assembly: Flowthru.Misc.DataFrames.dll  

LINQ-style extension methods for <xref href="Flowthru.Misc.DataFrames.TypedFrame%601" data-throw-if-not-resolved="false"></xref> that build expression trees.

```csharp
public static class TypedFrameExtensions
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[TypedFrameExtensions](Flowthru.Misc.DataFrames.TypedFrameExtensions.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

These methods follow the same pattern as <xref href="System.Linq.Queryable" data-throw-if-not-resolved="false"></xref>: each call
captures the lambda as an <xref href="System.Linq.Expressions.Expression" data-throw-if-not-resolved="false"></xref> tree node and delegates to
<xref href="System.Linq.IQueryProvider.CreateQuery%60%601(System.Linq.Expressions.Expression)" data-throw-if-not-resolved="false"></xref>. No native operations execute here —
translation is deferred to the provider's <xref href="Flowthru.Misc.DataFrames.IFrameQueryProvider.Compile(System.Linq.Expressions.Expression)" data-throw-if-not-resolved="false"></xref> method.

## Methods

### <a id="Flowthru_Misc_DataFrames_TypedFrameExtensions_Count__1_Flowthru_Misc_DataFrames_TypedFrame___0__"></a> Count<TSource\>\(TypedFrame<TSource\>\)

Returns the number of rows in the frame.

```csharp
public static long Count<TSource>(this TypedFrame<TSource> source)
```

#### Parameters

`source` [TypedFrame](Flowthru.Misc.DataFrames.TypedFrame\-1.md)<TSource\>

#### Returns

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

#### Type Parameters

`TSource` 

#### Remarks

This triggers compilation and execution via the provider. It is a terminal operation.

### <a id="Flowthru_Misc_DataFrames_TypedFrameExtensions_Distinct__1_Flowthru_Misc_DataFrames_TypedFrame___0__"></a> Distinct<TSource\>\(TypedFrame<TSource\>\)

Returns a frame with duplicate rows removed.

```csharp
public static TypedFrame<TSource> Distinct<TSource>(this TypedFrame<TSource> source)
```

#### Parameters

`source` [TypedFrame](Flowthru.Misc.DataFrames.TypedFrame\-1.md)<TSource\>

#### Returns

 [TypedFrame](Flowthru.Misc.DataFrames.TypedFrame\-1.md)<TSource\>

#### Type Parameters

`TSource` 

### <a id="Flowthru_Misc_DataFrames_TypedFrameExtensions_GroupBy__2_Flowthru_Misc_DataFrames_TypedFrame___0__System_Linq_Expressions_Expression_System_Func___0___1___"></a> GroupBy<TSource, TKey\>\(TypedFrame<TSource\>, Expression<Func<TSource, TKey\>\>\)

Groups rows by a key selector, producing a <xref href="Flowthru.Misc.DataFrames.GroupedFrame%602" data-throw-if-not-resolved="false"></xref>
that can be aggregated via <xref href="Flowthru.Misc.DataFrames.GroupedFrameExtensions.Aggregate%60%603(Flowthru.Misc.DataFrames.GroupedFrame%7b%60%600%2c%60%601%7d%2cSystem.Linq.Expressions.Expression%7bSystem.Func%7bFlowthru.Misc.DataFrames.AggregationContext%7b%60%600%2c%60%601%7d%2c%60%602%7d%7d)" data-throw-if-not-resolved="false"></xref>.

```csharp
public static GroupedFrame<TKey, TSource> GroupBy<TSource, TKey>(this TypedFrame<TSource> source, Expression<Func<TSource, TKey>> keySelector)
```

#### Parameters

`source` [TypedFrame](Flowthru.Misc.DataFrames.TypedFrame\-1.md)<TSource\>

`keySelector` [Expression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.expression\-1)<[Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TSource, TKey\>\>

#### Returns

 [GroupedFrame](Flowthru.Misc.DataFrames.GroupedFrame\-2.md)<TKey, TSource\>

#### Type Parameters

`TSource` 

`TKey` 

### <a id="Flowthru_Misc_DataFrames_TypedFrameExtensions_Join__4_Flowthru_Misc_DataFrames_TypedFrame___0__Flowthru_Misc_DataFrames_TypedFrame___1__System_Linq_Expressions_Expression_System_Func___0___2___System_Linq_Expressions_Expression_System_Func___1___2___System_Linq_Expressions_Expression_System_Func___0___1___3___"></a> Join<TOuter, TInner, TKey, TResult\>\(TypedFrame<TOuter\>, TypedFrame<TInner\>, Expression<Func<TOuter, TKey\>\>, Expression<Func<TInner, TKey\>\>, Expression<Func<TOuter, TInner, TResult\>\>\)

Joins two typed frames on matching keys and projects the result into a new schema.

```csharp
public static TypedFrame<TResult> Join<TOuter, TInner, TKey, TResult>(this TypedFrame<TOuter> outer, TypedFrame<TInner> inner, Expression<Func<TOuter, TKey>> outerKeySelector, Expression<Func<TInner, TKey>> innerKeySelector, Expression<Func<TOuter, TInner, TResult>> resultSelector)
```

#### Parameters

`outer` [TypedFrame](Flowthru.Misc.DataFrames.TypedFrame\-1.md)<TOuter\>

`inner` [TypedFrame](Flowthru.Misc.DataFrames.TypedFrame\-1.md)<TInner\>

`outerKeySelector` [Expression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.expression\-1)<[Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TOuter, TKey\>\>

`innerKeySelector` [Expression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.expression\-1)<[Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TInner, TKey\>\>

`resultSelector` [Expression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.expression\-1)<[Func](https://learn.microsoft.com/dotnet/api/system.func\-3)<TOuter, TInner, TResult\>\>

#### Returns

 [TypedFrame](Flowthru.Misc.DataFrames.TypedFrame\-1.md)<TResult\>

#### Type Parameters

`TOuter` 

`TInner` 

`TKey` 

`TResult` 

### <a id="Flowthru_Misc_DataFrames_TypedFrameExtensions_OrderBy__2_Flowthru_Misc_DataFrames_TypedFrame___0__System_Linq_Expressions_Expression_System_Func___0___1___"></a> OrderBy<TSource, TKey\>\(TypedFrame<TSource\>, Expression<Func<TSource, TKey\>\>\)

Sorts rows by a key in ascending order. The schema type is preserved.

```csharp
public static TypedFrame<TSource> OrderBy<TSource, TKey>(this TypedFrame<TSource> source, Expression<Func<TSource, TKey>> keySelector)
```

#### Parameters

`source` [TypedFrame](Flowthru.Misc.DataFrames.TypedFrame\-1.md)<TSource\>

`keySelector` [Expression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.expression\-1)<[Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TSource, TKey\>\>

#### Returns

 [TypedFrame](Flowthru.Misc.DataFrames.TypedFrame\-1.md)<TSource\>

#### Type Parameters

`TSource` 

`TKey` 

### <a id="Flowthru_Misc_DataFrames_TypedFrameExtensions_OrderByDescending__2_Flowthru_Misc_DataFrames_TypedFrame___0__System_Linq_Expressions_Expression_System_Func___0___1___"></a> OrderByDescending<TSource, TKey\>\(TypedFrame<TSource\>, Expression<Func<TSource, TKey\>\>\)

Sorts rows by a key in descending order. The schema type is preserved.

```csharp
public static TypedFrame<TSource> OrderByDescending<TSource, TKey>(this TypedFrame<TSource> source, Expression<Func<TSource, TKey>> keySelector)
```

#### Parameters

`source` [TypedFrame](Flowthru.Misc.DataFrames.TypedFrame\-1.md)<TSource\>

`keySelector` [Expression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.expression\-1)<[Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TSource, TKey\>\>

#### Returns

 [TypedFrame](Flowthru.Misc.DataFrames.TypedFrame\-1.md)<TSource\>

#### Type Parameters

`TSource` 

`TKey` 

### <a id="Flowthru_Misc_DataFrames_TypedFrameExtensions_Select__2_Flowthru_Misc_DataFrames_TypedFrame___0__System_Linq_Expressions_Expression_System_Func___0___1___"></a> Select<TSource, TResult\>\(TypedFrame<TSource\>, Expression<Func<TSource, TResult\>\>\)

Projects each row into a new schema type via a selector expression.

```csharp
public static TypedFrame<TResult> Select<TSource, TResult>(this TypedFrame<TSource> source, Expression<Func<TSource, TResult>> selector)
```

#### Parameters

`source` [TypedFrame](Flowthru.Misc.DataFrames.TypedFrame\-1.md)<TSource\>

`selector` [Expression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.expression\-1)<[Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TSource, TResult\>\>

#### Returns

 [TypedFrame](Flowthru.Misc.DataFrames.TypedFrame\-1.md)<TResult\>

#### Type Parameters

`TSource` 

`TResult` 

### <a id="Flowthru_Misc_DataFrames_TypedFrameExtensions_SelectOver__2_Flowthru_Misc_DataFrames_TypedFrame___0__System_Linq_Expressions_Expression_System_Func___0_Flowthru_Misc_DataFrames_WindowContext___0____1___"></a> SelectOver<TSource, TResult\>\(TypedFrame<TSource\>, Expression<Func<TSource, WindowContext<TSource\>, TResult\>\>\)

Projects each row into a new schema type, with access to windowed aggregate and
ranking functions via the <xref href="Flowthru.Misc.DataFrames.WindowContext%601" data-throw-if-not-resolved="false"></xref> parameter.

```csharp
public static TypedFrame<TResult> SelectOver<TSource, TResult>(this TypedFrame<TSource> source, Expression<Func<TSource, WindowContext<TSource>, TResult>> selector)
```

#### Parameters

`source` [TypedFrame](Flowthru.Misc.DataFrames.TypedFrame\-1.md)<TSource\>

`selector` [Expression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.expression\-1)<[Func](https://learn.microsoft.com/dotnet/api/system.func\-3)<TSource, [WindowContext](Flowthru.Misc.DataFrames.WindowContext\-1.md)<TSource\>, TResult\>\>

#### Returns

 [TypedFrame](Flowthru.Misc.DataFrames.TypedFrame\-1.md)<TResult\>

#### Type Parameters

`TSource` 

`TResult` 

#### Remarks

Each window function call in the selector must pass a
<xref href="Flowthru.Misc.DataFrames.FrameWindowSpec%601" data-throw-if-not-resolved="false"></xref> as its last argument, which defines the
partition and ordering for that specific function. Multiple specs may appear in
the same projection, enabling multi-window queries in a single call.

### <a id="Flowthru_Misc_DataFrames_TypedFrameExtensions_Take__1_Flowthru_Misc_DataFrames_TypedFrame___0__System_Int32_"></a> Take<TSource\>\(TypedFrame<TSource\>, int\)

Limits the frame to the first <code class="paramref">count</code> rows. The schema type is preserved.

```csharp
public static TypedFrame<TSource> Take<TSource>(this TypedFrame<TSource> source, int count)
```

#### Parameters

`source` [TypedFrame](Flowthru.Misc.DataFrames.TypedFrame\-1.md)<TSource\>

`count` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [TypedFrame](Flowthru.Misc.DataFrames.TypedFrame\-1.md)<TSource\>

#### Type Parameters

`TSource` 

### <a id="Flowthru_Misc_DataFrames_TypedFrameExtensions_Union__1_Flowthru_Misc_DataFrames_TypedFrame___0__Flowthru_Misc_DataFrames_TypedFrame___0__"></a> Union<TSource\>\(TypedFrame<TSource\>, TypedFrame<TSource\>\)

Concatenates two frames of the same schema, preserving all rows (including duplicates).
Equivalent to SQL <code>UNION ALL</code>; use <xref href="Flowthru.Misc.DataFrames.TypedFrameExtensions.Distinct%60%601(Flowthru.Misc.DataFrames.TypedFrame%7b%60%600%7d)" data-throw-if-not-resolved="false"></xref> after to get
distinct-row semantics.

```csharp
public static TypedFrame<TSource> Union<TSource>(this TypedFrame<TSource> source, TypedFrame<TSource> other)
```

#### Parameters

`source` [TypedFrame](Flowthru.Misc.DataFrames.TypedFrame\-1.md)<TSource\>

`other` [TypedFrame](Flowthru.Misc.DataFrames.TypedFrame\-1.md)<TSource\>

#### Returns

 [TypedFrame](Flowthru.Misc.DataFrames.TypedFrame\-1.md)<TSource\>

#### Type Parameters

`TSource` 

### <a id="Flowthru_Misc_DataFrames_TypedFrameExtensions_Where__1_Flowthru_Misc_DataFrames_TypedFrame___0__System_Linq_Expressions_Expression_System_Func___0_System_Boolean___"></a> Where<TSource\>\(TypedFrame<TSource\>, Expression<Func<TSource, bool\>\>\)

Filters rows using a predicate. The schema type is preserved.

```csharp
public static TypedFrame<TSource> Where<TSource>(this TypedFrame<TSource> source, Expression<Func<TSource, bool>> predicate)
```

#### Parameters

`source` [TypedFrame](Flowthru.Misc.DataFrames.TypedFrame\-1.md)<TSource\>

`predicate` [Expression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.expression\-1)<[Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TSource, [bool](https://learn.microsoft.com/dotnet/api/system.boolean)\>\>

#### Returns

 [TypedFrame](Flowthru.Misc.DataFrames.TypedFrame\-1.md)<TSource\>

#### Type Parameters

`TSource` 

