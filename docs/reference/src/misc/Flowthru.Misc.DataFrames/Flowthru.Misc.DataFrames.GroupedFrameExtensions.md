# <a id="Flowthru_Misc_DataFrames_GroupedFrameExtensions"></a> Class GroupedFrameExtensions

Namespace: [Flowthru.Misc.DataFrames](Flowthru.Misc.DataFrames.md)  
Assembly: Flowthru.Misc.DataFrames.dll  

Extension methods for <xref href="Flowthru.Misc.DataFrames.GroupedFrame%602" data-throw-if-not-resolved="false"></xref>.

```csharp
public static class GroupedFrameExtensions
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[GroupedFrameExtensions](Flowthru.Misc.DataFrames.GroupedFrameExtensions.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Methods

### <a id="Flowthru_Misc_DataFrames_GroupedFrameExtensions_Aggregate__3_Flowthru_Misc_DataFrames_GroupedFrame___0___1__System_Linq_Expressions_Expression_System_Func_Flowthru_Misc_DataFrames_AggregationContext___0___1____2___"></a> Aggregate<TKey, TSource, TResult\>\(GroupedFrame<TKey, TSource\>, Expression<Func<AggregationContext<TKey, TSource\>, TResult\>\>\)

Aggregates a grouped frame, producing a new <xref href="Flowthru.Misc.DataFrames.TypedFrame%601" data-throw-if-not-resolved="false"></xref>.

```csharp
public static TypedFrame<TResult> Aggregate<TKey, TSource, TResult>(this GroupedFrame<TKey, TSource> source, Expression<Func<AggregationContext<TKey, TSource>, TResult>> resultSelector)
```

#### Parameters

`source` [GroupedFrame](Flowthru.Misc.DataFrames.GroupedFrame\-2.md)<TKey, TSource\>

The grouped frame.

`resultSelector` [Expression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.expression\-1)<[Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[AggregationContext](Flowthru.Misc.DataFrames.AggregationContext\-2.md)<TKey, TSource\>, TResult\>\>

A projection from a <xref href="Flowthru.Misc.DataFrames.AggregationContext%602" data-throw-if-not-resolved="false"></xref> to the result schema.
The context exposes typed aggregate functions (Avg, Sum, Count, Min, Max) and the key.

#### Returns

 [TypedFrame](Flowthru.Misc.DataFrames.TypedFrame\-1.md)<TResult\>

#### Type Parameters

`TKey` 

The grouping key type.

`TSource` 

The source row schema type.

`TResult` 

The result schema type after aggregation.

