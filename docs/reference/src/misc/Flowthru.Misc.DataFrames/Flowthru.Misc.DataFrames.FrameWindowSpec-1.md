# <a id="Flowthru_Misc_DataFrames_FrameWindowSpec_1"></a> Class FrameWindowSpec<TSource\>

Namespace: [Flowthru.Misc.DataFrames](Flowthru.Misc.DataFrames.md)  
Assembly: Flowthru.Misc.DataFrames.dll  

An immutable, framework-agnostic window specification that describes how rows are
partitioned and ordered for windowed computations.

```csharp
public sealed class FrameWindowSpec<TSource> : IFrameWindowSpec
```

#### Type Parameters

`TSource` 

The row schema type the spec applies to.

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[FrameWindowSpec<TSource\>](Flowthru.Misc.DataFrames.FrameWindowSpec\-1.md)

#### Implements

[IFrameWindowSpec](Flowthru.Misc.DataFrames.IFrameWindowSpec.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

<p>
<code>FrameWindowSpec&lt;TSource&gt;</code> is a pure data carrier — it holds
<xref href="System.Linq.Expressions.LambdaExpression" data-throw-if-not-resolved="false"></xref> trees for partition and order keys. No native
(Spark, SQL, etc.) objects are created until the provider's expression visitor
translates the spec at query compilation time.
</p>
<p>
Build specs with the static <xref href="Flowthru.Misc.DataFrames.FrameWindowSpec%601.PartitionBy%60%601(System.Linq.Expressions.Expression%7bSystem.Func%7b%600%2c%60%600%7d%7d)" data-throw-if-not-resolved="false"></xref> or
<xref href="Flowthru.Misc.DataFrames.FrameWindowSpec%601.Global" data-throw-if-not-resolved="false"></xref> entry points and the fluent instance methods.
Pass the finished spec as the last argument to each
<xref href="Flowthru.Misc.DataFrames.WindowContext%601" data-throw-if-not-resolved="false"></xref> function call inside a
<xref href="Flowthru.Misc.DataFrames.TypedFrameExtensions.SelectOver%60%602(Flowthru.Misc.DataFrames.TypedFrame%7b%60%600%7d%2cSystem.Linq.Expressions.Expression%7bSystem.Func%7b%60%600%2cFlowthru.Misc.DataFrames.WindowContext%7b%60%600%7d%2c%60%601%7d%7d)" data-throw-if-not-resolved="false"></xref> projection.
</p>

## Fields

### <a id="Flowthru_Misc_DataFrames_FrameWindowSpec_1_Global"></a> Global

An empty window spanning all rows with no partition or ordering.
Use as the starting point when only ordering is needed:
<code>FrameWindowSpec&lt;T&gt;.Global.OrderBy(x =&gt; x.HireDate)</code>.

```csharp
public static readonly FrameWindowSpec<TSource> Global
```

#### Field Value

 [FrameWindowSpec](Flowthru.Misc.DataFrames.FrameWindowSpec\-1.md)<TSource\>

## Properties

### <a id="Flowthru_Misc_DataFrames_FrameWindowSpec_1_OrderByExpressions"></a> OrderByExpressions

Order-by expressions, each paired with a descending flag.

```csharp
public IReadOnlyList<(LambdaExpression KeySelector, bool Descending)> OrderByExpressions { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<\([LambdaExpression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.lambdaexpression) [KeySelector](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.linq.expressions.lambdaexpression,system.boolean\-.keyselector), [bool](https://learn.microsoft.com/dotnet/api/system.boolean) [Descending](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.linq.expressions.lambdaexpression,system.boolean\-.descending)\)\>

### <a id="Flowthru_Misc_DataFrames_FrameWindowSpec_1_PartitionByExpressions"></a> PartitionByExpressions

Partition-by expressions, in the order they were added.

```csharp
public IReadOnlyList<LambdaExpression> PartitionByExpressions { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[LambdaExpression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.lambdaexpression)\>

## Methods

### <a id="Flowthru_Misc_DataFrames_FrameWindowSpec_1_OrderBy__1_System_Linq_Expressions_Expression_System_Func__0___0___"></a> OrderBy<TKey\>\(Expression<Func<TSource, TKey\>\>\)

Adds an ascending sort key to this spec.

```csharp
public FrameWindowSpec<TSource> OrderBy<TKey>(Expression<Func<TSource, TKey>> keySelector)
```

#### Parameters

`keySelector` [Expression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.expression\-1)<[Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TSource, TKey\>\>

#### Returns

 [FrameWindowSpec](Flowthru.Misc.DataFrames.FrameWindowSpec\-1.md)<TSource\>

#### Type Parameters

`TKey` 

### <a id="Flowthru_Misc_DataFrames_FrameWindowSpec_1_OrderByDescending__1_System_Linq_Expressions_Expression_System_Func__0___0___"></a> OrderByDescending<TKey\>\(Expression<Func<TSource, TKey\>\>\)

Adds a descending sort key to this spec.

```csharp
public FrameWindowSpec<TSource> OrderByDescending<TKey>(Expression<Func<TSource, TKey>> keySelector)
```

#### Parameters

`keySelector` [Expression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.expression\-1)<[Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TSource, TKey\>\>

#### Returns

 [FrameWindowSpec](Flowthru.Misc.DataFrames.FrameWindowSpec\-1.md)<TSource\>

#### Type Parameters

`TKey` 

### <a id="Flowthru_Misc_DataFrames_FrameWindowSpec_1_PartitionBy__1_System_Linq_Expressions_Expression_System_Func__0___0___"></a> PartitionBy<TKey\>\(Expression<Func<TSource, TKey\>\>\)

Creates a new spec with a single partition key.

```csharp
public static FrameWindowSpec<TSource> PartitionBy<TKey>(Expression<Func<TSource, TKey>> keySelector)
```

#### Parameters

`keySelector` [Expression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.expression\-1)<[Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TSource, TKey\>\>

#### Returns

 [FrameWindowSpec](Flowthru.Misc.DataFrames.FrameWindowSpec\-1.md)<TSource\>

#### Type Parameters

`TKey` 

### <a id="Flowthru_Misc_DataFrames_FrameWindowSpec_1_ThenPartitionBy__1_System_Linq_Expressions_Expression_System_Func__0___0___"></a> ThenPartitionBy<TKey\>\(Expression<Func<TSource, TKey\>\>\)

Adds an additional partition key to this spec.

```csharp
public FrameWindowSpec<TSource> ThenPartitionBy<TKey>(Expression<Func<TSource, TKey>> keySelector)
```

#### Parameters

`keySelector` [Expression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.expression\-1)<[Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TSource, TKey\>\>

#### Returns

 [FrameWindowSpec](Flowthru.Misc.DataFrames.FrameWindowSpec\-1.md)<TSource\>

#### Type Parameters

`TKey` 

