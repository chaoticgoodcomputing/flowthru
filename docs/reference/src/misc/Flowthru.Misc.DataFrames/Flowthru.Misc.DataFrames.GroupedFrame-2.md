# <a id="Flowthru_Misc_DataFrames_GroupedFrame_2"></a> Class GroupedFrame<TKey, TSource\>

Namespace: [Flowthru.Misc.DataFrames](Flowthru.Misc.DataFrames.md)  
Assembly: Flowthru.Misc.DataFrames.dll  

An intermediate representation of a grouped <xref href="Flowthru.Misc.DataFrames.TypedFrame%601" data-throw-if-not-resolved="false"></xref>, produced by
<xref href="Flowthru.Misc.DataFrames.TypedFrameExtensions.GroupBy%60%602(Flowthru.Misc.DataFrames.TypedFrame%7b%60%600%7d%2cSystem.Linq.Expressions.Expression%7bSystem.Func%7b%60%600%2c%60%601%7d%7d)" data-throw-if-not-resolved="false"></xref>.

```csharp
public sealed class GroupedFrame<TKey, TSource>
```

#### Type Parameters

`TKey` 

The type of the grouping key.

`TSource` 

The row schema type before grouping.

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[GroupedFrame<TKey, TSource\>](Flowthru.Misc.DataFrames.GroupedFrame\-2.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

#### Extension Methods

[GroupedFrameExtensions.Aggregate<TKey, TSource, TResult\>\(GroupedFrame<TKey, TSource\>, Expression<Func<AggregationContext<TKey, TSource\>, TResult\>\>\)](Flowthru.Misc.DataFrames.GroupedFrameExtensions.md\#Flowthru\_Misc\_DataFrames\_GroupedFrameExtensions\_Aggregate\_\_3\_Flowthru\_Misc\_DataFrames\_GroupedFrame\_\_\_0\_\_\_1\_\_System\_Linq\_Expressions\_Expression\_System\_Func\_Flowthru\_Misc\_DataFrames\_AggregationContext\_\_\_0\_\_\_1\_\_\_\_2\_\_\_)

## Remarks

This type exists solely as a typed anchor for the subsequent <xref href="Flowthru.Misc.DataFrames.GroupedFrameExtensions.Aggregate%60%603(Flowthru.Misc.DataFrames.GroupedFrame%7b%60%600%2c%60%601%7d%2cSystem.Linq.Expressions.Expression%7bSystem.Func%7bFlowthru.Misc.DataFrames.AggregationContext%7b%60%600%2c%60%601%7d%2c%60%602%7d%7d)" data-throw-if-not-resolved="false"></xref>
call. It carries the accumulated group expression and prevents accidental misuse
of a grouped frame as a regular frame.

## Properties

### <a id="Flowthru_Misc_DataFrames_GroupedFrame_2_Expression"></a> Expression

```csharp
public Expression Expression { get; }
```

#### Property Value

 [Expression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.expression)

