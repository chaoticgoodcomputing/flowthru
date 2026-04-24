# <a id="Flowthru_Misc_DataFrames_AggregationContext_2"></a> Class AggregationContext<TKey, TSource\>

Namespace: [Flowthru.Misc.DataFrames](Flowthru.Misc.DataFrames.md)  
Assembly: Flowthru.Misc.DataFrames.dll  

Provides typed aggregate function placeholders within an
<xref href="Flowthru.Misc.DataFrames.GroupedFrameExtensions.Aggregate%60%603(Flowthru.Misc.DataFrames.GroupedFrame%7b%60%600%2c%60%601%7d%2cSystem.Linq.Expressions.Expression%7bSystem.Func%7bFlowthru.Misc.DataFrames.AggregationContext%7b%60%600%2c%60%601%7d%2c%60%602%7d%7d)" data-throw-if-not-resolved="false"></xref> expression.

```csharp
public sealed class AggregationContext<TKey, TSource>
```

#### Type Parameters

`TKey` 

The grouping key type.

`TSource` 

The source row schema type.

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[AggregationContext<TKey, TSource\>](Flowthru.Misc.DataFrames.AggregationContext\-2.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

Instances of this type are never constructed at runtime. The expression visitor
intercepts calls to its methods during expression tree translation and maps them
to the corresponding native aggregate functions (e.g., Spark's <code>avg()</code>).

## Properties

### <a id="Flowthru_Misc_DataFrames_AggregationContext_2_Key"></a> Key

The grouping key value for this group.

```csharp
public TKey Key { get; }
```

#### Property Value

 TKey

## Methods

### <a id="Flowthru_Misc_DataFrames_AggregationContext_2_Avg_System_Linq_Expressions_Expression_System_Func__1_System_Double___"></a> Avg\(Expression<Func<TSource, double\>\>\)

Computes the average of a numeric column.

```csharp
public double Avg(Expression<Func<TSource, double>> column)
```

#### Parameters

`column` [Expression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.expression\-1)<[Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TSource, [double](https://learn.microsoft.com/dotnet/api/system.double)\>\>

#### Returns

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="Flowthru_Misc_DataFrames_AggregationContext_2_Avg_System_Linq_Expressions_Expression_System_Func__1_System_Decimal___"></a> Avg\(Expression<Func<TSource, decimal\>\>\)

Computes the average of a numeric column.

```csharp
public decimal Avg(Expression<Func<TSource, decimal>> column)
```

#### Parameters

`column` [Expression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.expression\-1)<[Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TSource, [decimal](https://learn.microsoft.com/dotnet/api/system.decimal)\>\>

#### Returns

 [decimal](https://learn.microsoft.com/dotnet/api/system.decimal)

### <a id="Flowthru_Misc_DataFrames_AggregationContext_2_Avg_System_Linq_Expressions_Expression_System_Func__1_System_Int32___"></a> Avg\(Expression<Func<TSource, int\>\>\)

Computes the average of a numeric column.

```csharp
public double Avg(Expression<Func<TSource, int>> column)
```

#### Parameters

`column` [Expression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.expression\-1)<[Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TSource, [int](https://learn.microsoft.com/dotnet/api/system.int32)\>\>

#### Returns

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="Flowthru_Misc_DataFrames_AggregationContext_2_Count"></a> Count\(\)

Counts the number of rows in the group.

```csharp
public long Count()
```

#### Returns

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="Flowthru_Misc_DataFrames_AggregationContext_2_Max__1_System_Linq_Expressions_Expression_System_Func__1___0___"></a> Max<TValue\>\(Expression<Func<TSource, TValue\>\>\)

Computes the maximum value of a column.

```csharp
public TValue Max<TValue>(Expression<Func<TSource, TValue>> column)
```

#### Parameters

`column` [Expression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.expression\-1)<[Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TSource, TValue\>\>

#### Returns

 TValue

#### Type Parameters

`TValue` 

### <a id="Flowthru_Misc_DataFrames_AggregationContext_2_Min__1_System_Linq_Expressions_Expression_System_Func__1___0___"></a> Min<TValue\>\(Expression<Func<TSource, TValue\>\>\)

Computes the minimum value of a column.

```csharp
public TValue Min<TValue>(Expression<Func<TSource, TValue>> column)
```

#### Parameters

`column` [Expression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.expression\-1)<[Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TSource, TValue\>\>

#### Returns

 TValue

#### Type Parameters

`TValue` 

### <a id="Flowthru_Misc_DataFrames_AggregationContext_2_Sum_System_Linq_Expressions_Expression_System_Func__1_System_Double___"></a> Sum\(Expression<Func<TSource, double\>\>\)

Computes the sum of a numeric column.

```csharp
public double Sum(Expression<Func<TSource, double>> column)
```

#### Parameters

`column` [Expression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.expression\-1)<[Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TSource, [double](https://learn.microsoft.com/dotnet/api/system.double)\>\>

#### Returns

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="Flowthru_Misc_DataFrames_AggregationContext_2_Sum_System_Linq_Expressions_Expression_System_Func__1_System_Decimal___"></a> Sum\(Expression<Func<TSource, decimal\>\>\)

Computes the sum of a numeric column.

```csharp
public decimal Sum(Expression<Func<TSource, decimal>> column)
```

#### Parameters

`column` [Expression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.expression\-1)<[Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TSource, [decimal](https://learn.microsoft.com/dotnet/api/system.decimal)\>\>

#### Returns

 [decimal](https://learn.microsoft.com/dotnet/api/system.decimal)

### <a id="Flowthru_Misc_DataFrames_AggregationContext_2_Sum_System_Linq_Expressions_Expression_System_Func__1_System_Int32___"></a> Sum\(Expression<Func<TSource, int\>\>\)

Computes the sum of a numeric column.

```csharp
public long Sum(Expression<Func<TSource, int>> column)
```

#### Parameters

`column` [Expression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.expression\-1)<[Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TSource, [int](https://learn.microsoft.com/dotnet/api/system.int32)\>\>

#### Returns

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

