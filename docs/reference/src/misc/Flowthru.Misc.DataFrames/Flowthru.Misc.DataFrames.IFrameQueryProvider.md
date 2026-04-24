# <a id="Flowthru_Misc_DataFrames_IFrameQueryProvider"></a> Interface IFrameQueryProvider

Namespace: [Flowthru.Misc.DataFrames](Flowthru.Misc.DataFrames.md)  
Assembly: Flowthru.Misc.DataFrames.dll  

A query provider that creates <xref href="Flowthru.Misc.DataFrames.TypedFrame%601" data-throw-if-not-resolved="false"></xref> instances and compiles
accumulated expression trees into native frame operations.

```csharp
public interface IFrameQueryProvider : IQueryProvider
```

#### Implements

[IQueryProvider](https://learn.microsoft.com/dotnet/api/system.linq.iqueryprovider)

## Remarks

This interface extends <xref href="System.Linq.IQueryProvider" data-throw-if-not-resolved="false"></xref> with a
<xref href="Flowthru.Misc.DataFrames.IFrameQueryProvider.Compile(System.Linq.Expressions.Expression)" data-throw-if-not-resolved="false"></xref> method for producing native frame objects (e.g., a Spark
<code>DataFrame</code>) from the expression tree accumulated by chained operations.
Each provider implementation handles a specific DataFrame backend.

## Methods

### <a id="Flowthru_Misc_DataFrames_IFrameQueryProvider_Compile_System_Linq_Expressions_Expression_"></a> Compile\(Expression\)

Compiles the accumulated expression tree into a native frame object.

```csharp
object Compile(Expression expression)
```

#### Parameters

`expression` [Expression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.expression)

The expression tree rooted at a <xref href="Flowthru.Misc.DataFrames.TypedFrame%601" data-throw-if-not-resolved="false"></xref> constant,
with chained method calls representing operations.

#### Returns

 [object](https://learn.microsoft.com/dotnet/api/system.object)

The native frame object (e.g., Spark <code>DataFrame</code>).

### <a id="Flowthru_Misc_DataFrames_IFrameQueryProvider_Materialize__1_System_Linq_Expressions_Expression_"></a> Materialize<T\>\(Expression\)

Materializes the accumulated expression tree into an enumerable sequence of rows.
Called by <xref href="Flowthru.Misc.DataFrames.TypedFrame%601.GetEnumerator" data-throw-if-not-resolved="false"></xref> to enable transparent
TypedFrame → IEnumerable conversion at catalog item boundaries.

```csharp
IEnumerable<T> Materialize<T>(Expression expression)
```

#### Parameters

`expression` [Expression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.expression)

The accumulated expression tree.

#### Returns

 [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<T\>

The materialized rows.

#### Type Parameters

`T` 

The row schema type.

