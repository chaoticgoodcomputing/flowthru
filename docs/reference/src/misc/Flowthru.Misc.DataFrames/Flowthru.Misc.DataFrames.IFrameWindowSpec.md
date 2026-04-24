# <a id="Flowthru_Misc_DataFrames_IFrameWindowSpec"></a> Interface IFrameWindowSpec

Namespace: [Flowthru.Misc.DataFrames](Flowthru.Misc.DataFrames.md)  
Assembly: Flowthru.Misc.DataFrames.dll  

Non-generic contract for a window specification, used by visitors to translate
window definitions without requiring the generic source type parameter.

```csharp
public interface IFrameWindowSpec
```

## Properties

### <a id="Flowthru_Misc_DataFrames_IFrameWindowSpec_OrderByExpressions"></a> OrderByExpressions

Order-by expressions, each paired with a descending flag.

```csharp
IReadOnlyList<(LambdaExpression KeySelector, bool Descending)> OrderByExpressions { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<\([LambdaExpression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.lambdaexpression) [KeySelector](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.linq.expressions.lambdaexpression,system.boolean\-.keyselector), [bool](https://learn.microsoft.com/dotnet/api/system.boolean) [Descending](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.linq.expressions.lambdaexpression,system.boolean\-.descending)\)\>

### <a id="Flowthru_Misc_DataFrames_IFrameWindowSpec_PartitionByExpressions"></a> PartitionByExpressions

Partition-by expressions, in the order they were added.

```csharp
IReadOnlyList<LambdaExpression> PartitionByExpressions { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[LambdaExpression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.lambdaexpression)\>

